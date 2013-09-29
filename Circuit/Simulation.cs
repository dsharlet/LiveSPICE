using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using SyMath;
using LinqExpressions = System.Linq.Expressions;
using LinqExpression = System.Linq.Expressions.Expression;

namespace Circuit
{
    /// <summary>
    /// Simulate a circuit.
    /// </summary>
    public class Simulation
    {
        class Node
        {
            public Expression step;
            public double V = 0.0f;

            private LinqExpression linqExpr;
            public LinqExpression LinqExpr { get { return linqExpr; } }

            public Node(Expression Step) 
            { 
                step = Step;
                linqExpr = LinqExpression.Field(LinqExpression.Constant(this), typeof(Node), "V");
            }
        }
        private Dictionary<Expression, Node> nodes;

        protected Expression _t = Constant.Zero;
        public Expression t { get { return _t; } }

        protected Expression _T;
        public Expression T { get { return _T; } }

        private static Expression t0 = Variable.New("t0");

        protected int iterations = 1;
        /// <summary>
        /// Get or set the maximum number of iterations to use when numerically solving equations.
        /// </summary>
        public int Iterations { get { return iterations; } set { iterations = value; } }
        protected int oversample = 1;
        /// <summary>
        /// Get or set the oversampling factor for the simulation.
        /// </summary>
        public int Oversample { get { return oversample; } set { oversample = value; } }

        // Shorthand for df/dx.
        private static Expression D(Expression f, Expression x) { return f.Differentiate(x); }
        // Solve a system of equations, possibly including linear differential equations.
        private static List<Arrow> Solve(List<Equal> f, List<Expression> y, Expression t, Expression t0, Expression h, IntegrationMethod method, int iterations)
        {
            List<Expression> dydt = y.Select(i => D(i, t)).ToList();

            List<Arrow> step = new List<Arrow>();
            // Try solving for y algebraically.
            List<Arrow> linear = f.Solve(y);
            // Only accept independent solutions.
            linear.RemoveAll(i => i.Right.IsFunctionOf(dydt.Append(i.Left)));
            step.AddRange(linear);
            // Substitute the solutions found.
            f = f.Evaluate(linear).OfType<Equal>().ToList();
            y.RemoveAll(i => step.Find(j => j.Left.Equals(i)) != null);
            
            // Solve for any non-differential functions remaining and substitute them into the system.
            List<Arrow> nondifferential = f.Where(i => !i.IsFunctionOf(dydt)).Solve(y);
            List<Equal> df = f.Evaluate(nondifferential).OfType<Equal>().ToList();
            // Solve the differential equations.
            List<Arrow> differentials = df.NDSolve(y, t, t0, h, method, iterations);
            step.AddRange(differentials);
            // Replace differentials with approximations.
            f = f.Evaluate(y.Select(i => Arrow.New(D(i, t), (i - i.Evaluate(t, t0)) / h))).Cast<Equal>().ToList();
            f = f.Evaluate(differentials).OfType<Equal>().ToList();
            y.RemoveAll(i => step.Find(j => j.Left.Equals(i)) != null);

            // Try solving for numerical solutions.
            List<Arrow> nonlinear = f.NSolve(y.Select(i => Arrow.New(i, i.Evaluate(t, t0))), iterations);
            nonlinear.RemoveAll(i => i.Right.IsFunctionOf(dydt));
            step.AddRange(nonlinear);

            return step;
        }

        /// <summary>
        /// Create a simulation for the given circuit.
        /// </summary>
        /// <param name="C">Circuit to simulate.</param>
        /// <param name="T">Sampling period.</param>
        /// <returns></returns>
        public Simulation(Circuit C, Quantity F)
        {
            _T = 1.0 / ((Expression)F * oversample);

            // Compute the KCL equations for this circuit.
            List<Equal> kcl = C.Analyze();
            
            // Find the expression for the next timestep via the trapezoid method (ala bilinear transform).
            List<Arrow> step = Solve(
                kcl.ToList(),
                C.Nodes.Select(i => i.V).Cast<Expression>().ToList(), 
                Component.t, t0, 
                _T, 
                IntegrationMethod.Trapezoid, 
                iterations);

            // Create the nodes.
            nodes = step.ToDictionary(
                i => (Expression)Call.New(((Call)i.Left).Target, Component.t), 
                i => new Node(i.Right));
        }

        public void Reset()
        {
            _t = Constant.Zero;
            foreach (Node i in nodes.Values)
                i.V = 0.0;
        }
        
        // Process some samples. Requested nodes are stored in Output.
        public void Process(int SampleCount, IDictionary<Expression, double[]> Input, IDictionary<Expression, double[]> Output)
        {
            Delegate processor = Compile(Input.Keys, Output.Keys);

            // Build parameter list for the processor.
            List<object> parameters = new List<object>();
            parameters.Add(SampleCount);
            parameters.Add((double)t);
            parameters.Add((double)T);
            foreach (KeyValuePair<Expression, double[]> i in Input)
                parameters.Add(i.Value);
            foreach (KeyValuePair<Expression, double[]> i in Output)
                parameters.Add(i.Value);

            _t = (double)processor.DynamicInvoke(parameters.ToArray());
        }

        public void Process(Expression InputNode, double[] InputSamples, IDictionary<Expression, double[]> Output)
        {
            Process(
                InputSamples.Length, 
                new Dictionary<Expression, double[]>() { { InputNode, InputSamples } }, 
                Output);
        }

        public void Process(Expression InputNode, double[] InputSamples, Expression OutputNode, double[] OutputSamples)
        {
            Process(
                InputSamples.Length, 
                new Dictionary<Expression, double[]>() { { InputNode, InputSamples } }, 
                new Dictionary<Expression, double[]>() { { OutputNode, OutputSamples } });
        }

        // Compile and cache delegates for processing various IO configurations for this simulation.
        Dictionary<long, Delegate> compiled = new Dictionary<long, Delegate>();
        private Delegate Compile(IEnumerable<Expression> Input, IEnumerable<Expression> Output)
        {
            long hash = Input.OrderedHashCode() * 33 + Output.OrderedHashCode();

            Delegate d;
            if (compiled.TryGetValue(hash, out d))
                return d;

            d = ProcessExpression(Input, Output).Compile();
            compiled[hash] = d;
            return d;
        }
        
        // The resulting lambda processes N samples, using buffers provided for Input and Output.
        // Arguments: int N, double t0, double T, double[] { Input }, double[] { Output }
        // Returns: t0 + N * T
        private LinqExpressions.LambdaExpression ProcessExpression(IEnumerable<Expression> Input, IEnumerable<Expression> Output)
        {
            // Map expressions to identifiers in the syntax tree.
            Dictionary<Expression, LinqExpression> v = new Dictionary<Expression, LinqExpression>();
            Dictionary<Expression, LinqExpression> buffers = new Dictionary<Expression, LinqExpression>();

            // Get expressions for the state of each node. These may be replaced by input parameters.
            foreach (KeyValuePair<Expression, Node> i in nodes)
                v[i.Key.Evaluate(Component.t, t0)] = i.Value.LinqExpr;

            // Lambda definition objects.
            LinqExpressions.LabelTarget returnTo = LinqExpression.Label(typeof(double));
            List<LinqExpressions.ParameterExpression> parameters = new List<LinqExpressions.ParameterExpression>();
            List<LinqExpression> body = new List<LinqExpression>();
            List<LinqExpressions.ParameterExpression> locals = new List<LinqExpressions.ParameterExpression>();

            LinqExpressions.ParameterExpression pN = LinqExpression.Parameter(typeof(int), "N");
            parameters.Add(pN);

            LinqExpressions.ParameterExpression pt0 = LinqExpression.Parameter(typeof(double), "t0");
            parameters.Add(pt0);
            v[t0] = pt0;

            LinqExpressions.ParameterExpression pT = LinqExpression.Parameter(typeof(double), "T");
            parameters.Add(pT);
            v[Component.T] = pT;

            foreach (Expression i in Input)
            {
                LinqExpressions.ParameterExpression arg = LinqExpression.Parameter(typeof(double[]), i.ToString());
                parameters.Add(arg);
                buffers[i] = arg;
            }

            foreach (Expression i in Output)
            {
                LinqExpressions.ParameterExpression arg = LinqExpression.Parameter(typeof(double[]), i.ToString());
                parameters.Add(arg);
                buffers[i] = arg;
            }

            // Set t = t0.
            LinqExpressions.ParameterExpression vt = LinqExpression.Variable(typeof(double), "t");
            body.Add(LinqExpression.Assign(vt, pt0));
            locals.Add(vt);
            v[Component.t] = vt;

            // Set n = 0.
            LinqExpressions.ParameterExpression vn = LinqExpression.Variable(typeof(int), "n");
            locals.Add(vn);
            body.Add(LinqExpression.Assign(vn, LinqExpression.Constant(0)));

            LinqExpressions.LabelTarget loop = LinqExpression.Label("loop");

            // Loop header.
            body.Add(LinqExpression.Label(loop));
            body.Add(LinqExpression.IfThen(
                LinqExpression.GreaterThanOrEqual(vn, pN),
                LinqExpression.Return(returnTo, vt, typeof(double))));

            // Loop body.
            // Get input samples.
            foreach (Expression i in Input)
            {
                v[i] = LinqExpression.MakeIndex(
                    buffers[i], 
                    typeof(double[]).GetProperty("Item"), 
                    new LinqExpression[] { vn });

                // If i isn't a node, just make a dummy expression for the previous timestep. 
                // This might be able to be removed with an improved system solver that doesn't create references to i[t0] when i is not a node.
                if (!nodes.ContainsKey(i))
                    v[i.Evaluate(Component.t, t0)] = v[i];
            }

            // Set t = t0 + T.
            body.Add(LinqExpression.Assign(vt, LinqExpression.Add(pt0, pT)));
            
            // Compile step expressions and assign to the node state.
            foreach (KeyValuePair<Expression, Node> i in nodes)
                body.Add(LinqExpression.Assign(i.Value.LinqExpr, i.Value.step.Compile(v)));

            // Store output samples.
            foreach (Expression i in Output)
            {
                Node n;
                if (nodes.TryGetValue(i, out n))
                {
                    body.Add(LinqExpression.Assign(
                        LinqExpression.MakeIndex(
                            buffers[i],
                            typeof(double[]).GetProperty("Item"),
                            new LinqExpression[] { vn }),
                        n.LinqExpr));
                }
                else
                {
                    body.Add(LinqExpression.Assign(
                        LinqExpression.MakeIndex(
                            buffers[i],
                            typeof(double[]).GetProperty("Item"),
                            new LinqExpression[] { vn }),
                        LinqExpression.Constant(double.NaN)));
                }
            }

            // Update t0 = t.
            body.Add(LinqExpression.Assign(pt0, vt));

            // ++n.
            body.Add(LinqExpression.PreIncrementAssign(vn));

            // Go to the beginning of the loop.
            body.Add(LinqExpression.Goto(loop));
            
            body.Add(LinqExpression.Label(returnTo, vt));
            return LinqExpression.Lambda(LinqExpression.Block(locals, body), parameters);
        }
    }
}
