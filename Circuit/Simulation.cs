using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
        // Expression for t at the previous timestep.
        private static readonly Expression t0 = Variable.New("t0");
        private static readonly Expression t = Component.t;

        protected double _t = 0.0;
        /// <summary>
        /// Get the current time of the simulation.
        /// </summary>
        public double Time { get { return _t; } }
        
        protected double _T;
        /// <summary>
        /// Get the timestep for the simulation.
        /// </summary>
        public double TimeStep { get { return _T; } }
        
        protected int iterations;
        /// <summary>
        /// Get or set the maximum number of iterations to use when numerically solving equations.
        /// </summary>
        public int Iterations { get { return iterations; } }

        protected int oversample;
        /// <summary>
        /// Get or set the oversampling factor for the simulation.
        /// </summary>
        public int Oversample { get { return oversample; } }

        // Nodes in this simulation.
        private List<Expression> nodes;

        // Simulation timestep.
        private Expression h;

        // Expressions for trivial solutions to the system.
        private List<Arrow> trivial;
        // Expressions for the solution of the differential equations.
        private List<Arrow> differential;
        // Expressions for the solution of the linear equations.
        private List<Arrow> algebraic;
        // Implicit equations describing the non-linear system.
        private List<Tuple<Equal, Expression>> nonlinear;
        private List<Expression> unknowns;

        // Stores any global state in the simulation (previous state values, mostly).
        private Dictionary<Expression, GlobalExpr<double>> globals = new Dictionary<Expression, GlobalExpr<double>>();

        /// <summary>
        /// Enumerate the nodes in the simulation.
        /// </summary>
        public IEnumerable<Expression> Nodes { get { return nodes; } }
        
        /// <summary>
        /// Create a simulation for the given circuit.
        /// </summary>
        /// <param name="C">Circuit to simulate.</param>
        /// <param name="T">Sampling period.</param>
        public Simulation(Circuit Circuit, Quantity SampleRate, int Oversample, int Iterations)
        {
            oversample = Oversample;
            iterations = Iterations;
            _T = 1.0 / (double)SampleRate;
            nodes = Circuit.Nodes.Select(i => (Expression)Call.New(((Call)i.V).Target, t)).ToList();

            // Length of one timestep in the oversampled simulation.
            h = 1 / ((Expression)SampleRate * Oversample);

            // Analyze the circuit to get the KCL equations and the unknowns we need to solve for.
            List<Expression> x = new List<Expression>();
            List<Equal> kcl = new List<Equal>();
            Circuit.Analyze(kcl, x);

            // Create global variables for the previous state of each unknown.
            foreach (Expression i in x)
                globals[i.Evaluate(t, t0)] = new GlobalExpr<double>(0.0);

            // Find trivial solutions (e.g. ground) for x and substitute them into the system.
            trivial = kcl.Solve(x);
            trivial.RemoveAll(i => i.Right.IsFunctionOf(x));
            kcl = kcl.Evaluate(trivial).OfType<Equal>().ToList();
            x.RemoveAll(i => trivial.Any(j => j.Left.Equals(i)));

            // Get the potential differential unknowns in the system.
            List<Expression> dxdt = x.Select(i => D(i, t)).ToList();
            
            // These are the non-trivial KCL equations of the circuit, stored as differential algebraic equations (DAEs).
            List<DiffAlgEq> dae = kcl.Select(i => new DiffAlgEq(i, x, dxdt)).ToList();
            
            // First, work on the system where f(t) is constant (previous state).
            List<Equal> S = dae.Select(i => Equal.New(i.A, i.f0)).ToList();

            // Only care about dx/dt that actually exist in the system.
            dxdt.RemoveAll(i => !S.Any(j => j.IsFunctionOf(i)));

            // NDSolve can solve this system if algebraic variables are substituted with constants (previous value).
            List<Expression> xa = x.Where(i => dxdt.None(j => DOf(j).Equals(i))).ToList();
            differential = S
                .Evaluate(S.Solve(xa)).OfType<Equal>()
                .Evaluate(xa.Select(i => Arrow.New(i, i.Evaluate(t, t0)))).Cast<Equal>()
                .NDSolve(dxdt.Select(i => DOf(i)).ToList(), t, t0, h, IntegrationMethod.Trapezoid);
            x.RemoveAll(i => differential.Any(j => j.Left.Equals(i)));

            // Find as many closed form solutions in the remaining system as possible.
            algebraic = S.Solve(x.Where(i => dae.None(j => j.f.IsFunctionOf(i))));
            algebraic.RemoveAll(i => i.Right.IsFunctionOf(x.Except(algebraic.Select(j => j.Left))));
            x.RemoveAll(i => algebraic.Any(j => j.Left.Equals(i)));

            // The remaining non-linear system will be solved via Newton's method.
            nonlinear = dae.Where(i => !i.f.IsZero()).Select(i => new Tuple<Equal, Expression>(Equal.New(i.A, i.f), i.f0)).ToList();
            unknowns = x;

            // Create a global variable for the value of each f0.
            foreach (Tuple<Equal, Expression> i in nonlinear)
                globals[i.Item2] = new GlobalExpr<double>(0.0);
        }

        /// <summary>
        /// Clear all state from the simulation.
        /// </summary>
        public void Reset()
        {
            _t = 0.0;
            foreach (GlobalExpr<double> i in globals.Values)
                i.Value = 0.0;
        }
        
        /// <summary>
        /// Process some samples with this simulation.
        /// </summary>
        /// <param name="N">Number of samples to process.</param>
        /// <param name="Input">Mapping of node Expression -> double[] buffers that describe the input samples.</param>
        /// <param name="Output">Mapping of node Expression -> double[] buffers that describe requested output samples.</param>
        /// <param name="Arguments">Constant expressions describing the values of any parameters to the simulation.</param>
        public void Process(int N, IDictionary<Expression, double[]> Input, IDictionary<Expression, double[]> Output, IEnumerable<Arrow> Arguments)
        {
            Delegate processor = Compile(Input.Keys, Output.Keys, Arguments.Select(i => i.Left));

            // Build parameter list for the processor.
            List<object> parameters = new List<object>(3 + Input.Count + Output.Count + Arguments.Count());
            parameters.Add(N);
            parameters.Add((double)_t);
            parameters.Add((double)_T / Oversample);
            foreach (KeyValuePair<Expression, double[]> i in Input)
                parameters.Add(i.Value);
            foreach (KeyValuePair<Expression, double[]> i in Output)
                parameters.Add(i.Value);
            foreach (Arrow i in Arguments)
                parameters.Add((double)i.Right);

            _t = (double)processor.DynamicInvoke(parameters.ToArray());
        }

        /// <summary>
        /// Process some samples with this simulation.
        /// </summary>
        /// <param name="N">Number of samples to process.</param>
        /// <param name="Input">Mapping of node Expression -> double[] buffers that describe the input samples.</param>
        /// <param name="Output">Mapping of node Expression -> double[] buffers that describe requested output samples.</param>
        /// <param name="Arguments">Constant expressions describing the values of any parameters to the simulation.</param>
        public void Process(int N, IDictionary<Expression, double[]> Input, IDictionary<Expression, double[]> Output, params Arrow[] Arguments)
        {
            Process(N, Input, Output, Arguments.AsEnumerable());
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

        Dictionary<long, Delegate> compiled = new Dictionary<long, Delegate>();
        // Compile and cache delegates for processing various IO configurations for this simulation.
        private Delegate Compile(IEnumerable<Expression> Input, IEnumerable<Expression> Output, IEnumerable<Expression> Parameters)
        {
            long hash = Input.OrderedHashCode();
            hash = hash * 33 + Output.OrderedHashCode();
            hash = hash * 33 + Parameters.OrderedHashCode();

            Delegate d;
            if (compiled.TryGetValue(hash, out d))
                return d;

            d = DefineProcessFunction(Input, Output, Parameters).Compile();
            compiled[hash] = d;
            return d;
        }
        
        // The resulting lambda processes N samples, using buffers provided for Input and Output:
        //  double Process(int N, double t0, double T, double[] Input0 ..., double[] Output0 ..., double Parameter0 ...)
        //  { ... }
        private LinqExpressions.LambdaExpression DefineProcessFunction(IEnumerable<Expression> Input, IEnumerable<Expression> Output, IEnumerable<Expression> Parameters)
        {
            // Map expressions to identifiers in the syntax tree.
            Dictionary<Expression, LinqExpression> v = new Dictionary<Expression, LinqExpression>();
            Dictionary<Expression, LinqExpression> buffers = new Dictionary<Expression, LinqExpression>();

            // Get expressions for the state of each node. These may be replaced by input parameters.
            foreach (KeyValuePair<Expression, GlobalExpr<double>> i in globals)
                v[i.Key] = i.Value;

            // Lambda definition objects.
            List<LinqExpressions.ParameterExpression> parameters = new List<LinqExpressions.ParameterExpression>();
            List<LinqExpressions.ParameterExpression> locals = new List<LinqExpressions.ParameterExpression>();
            List<LinqExpression> body = new List<LinqExpression>();

            // Create parameters for the basic simulation info (N, t, T).
            LinqExpressions.ParameterExpression pN = Declare<int>(parameters, "N");
            LinqExpressions.ParameterExpression pt0 = Declare<double>(parameters, v, t0);
            LinqExpressions.ParameterExpression pT = Declare<double>(parameters, v, Component.T);
            // Create buffer parameters for each input, output.
            foreach (Expression i in Input.Concat(Output))
                Declare<double[]>(parameters, buffers, i);
            // Create constant parameters for simulation parameters.
            foreach (Expression i in Parameters)
                Declare<double>(parameters, v, i);
            
            // Define lambda body.

            // double t = t0
            LinqExpressions.ParameterExpression vt = Declare<double>(locals, v, t);
            body.Add(LinqExpression.Assign(vt, pt0));

            // Trivial timestep expressions that are not a function of the input can be set once here.
            foreach (Arrow i in trivial.Where(i => !i.IsFunctionOf(Input)))
            {
                LinqExpression Vi = globals[i.Left.Evaluate(t, t0)];
                body.Add(LinqExpression.Assign(Vi, i.Right.Compile(v)));
                v[i.Left] = Vi;
            }

            // for (int n = 0; n < N; ++n)
            LinqExpressions.ParameterExpression vn = Declare<int>(locals, "n");
            For(body,
                () => body.Add(LinqExpression.Assign(vn, LinqExpression.Constant(0))),
                LinqExpression.LessThan(vn, pN),
                () => body.Add(LinqExpression.PreIncrementAssign(vn)),
                () =>
                {
                    // Prepare input samples for oversampling interpolation.
                    Dictionary<Expression, LinqExpression> dinput = new Dictionary<Expression, LinqExpression>();
                    foreach (Expression i in Input)
                    {
                        // Ensure that we have a global variable to store the previous sample in.
                        globals[i] = new GlobalExpr<double>(0.0);
                        LinqExpression va = globals[i];
                        LinqExpression vb = LinqExpression.MakeIndex(
                            buffers[i],
                            buffers[i].Type.GetProperty("Item"),
                            new LinqExpression[] { vn });

                        // double vi = va
                        LinqExpressions.ParameterExpression vi = Declare<double>(locals, v, i, i.ToString() + "_i");
                        body.Add(LinqExpression.Assign(vi, va));

                        // di = (vb - vi) / Oversample.
                        LinqExpressions.ParameterExpression dinputi = Declare<double>(locals, dinput, i, "d" + i.ToString());
                        body.Add(LinqExpression.Assign(dinputi, LinqExpression.Divide(LinqExpression.Subtract(vb, vi), LinqExpression.Constant((double)Oversample))));

                        // va = vb
                        body.Add(LinqExpression.Assign(va, vb));

                        // If i isn't a node, add the 
                        if (!nodes.Contains(i))
                        {
                            v[i.Evaluate(t, t0)] = v[i];
                            v[i.Differentiate(t).Evaluate(t, t0)] = v[i.Differentiate(t)] = LinqExpression.Divide(dinputi, pT);
                        }
                    }

                    // Prepare output sample accumulators for low pass filtering.
                    Dictionary<Expression, LinqExpression> output = new Dictionary<Expression, LinqExpression>();
                    foreach (Expression i in Output)
                    {
                        // i_o = 0
                        LinqExpression s = Declare<double>(locals, output, i, i.ToString() + "_o");
                        body.Add(LinqExpression.Assign(s, LinqExpression.Constant(0.0)));
                    }

                    // for (int ov = Oversample; ov > 0; --ov)
                    LinqExpressions.ParameterExpression ov = Declare<int>(locals, "ov");
                    For(body, 
                        () => body.Add(LinqExpression.Assign(ov, LinqExpression.Constant(Oversample))),
                        LinqExpression.GreaterThan(ov, LinqExpression.Constant(0)),
                        () => body.Add(LinqExpression.PreDecrementAssign(ov)),
                        () => 
                        {
                            // t += T
                            body.Add(LinqExpression.AddAssign(vt, pT));

                            // input_i += d_i
                            foreach (Expression i in Input)
                                body.Add(LinqExpression.AddAssign(v[i], dinput[i]));

                            // Compile the trivial timestep expressions that are a function of the input.
                            foreach (Arrow i in trivial.Where(i => i.Right.IsFunctionOf(Input)))
                            {
                                LinqExpression Vi = globals[i.Left.Evaluate(t, t0)];
                                body.Add(LinqExpression.Assign(Vi, i.Right.Compile(v)));
                                v[i.Left] = Vi;
                            }

                            // Compile the differential timestep expressions.
                            foreach (Arrow i in differential)
                            {
                                LinqExpression Vt0 = globals[i.Left.Evaluate(t, t0)];
                                LinqExpression Vt = i.Right.Compile(v);
                                // Compute the value of v'(t) and store it in the map.
                                LinqExpression dV = Declare<double>(locals, "d" + i.Left.ToString());
                                body.Add(LinqExpression.Assign(dV, LinqExpression.Divide(LinqExpression.Subtract(Vt, Vt0), pT)));
                                v[D(i.Left, t)] = dV;

                                // Update Vt0.
                                body.Add(LinqExpression.Assign(Vt0, Vt));
                                v[i.Left] = Vt;
                            }

                            // And the linear timestep expressions.
                            foreach (Arrow i in algebraic)
                            {
                                LinqExpression Vt0 = globals[i.Left.Evaluate(t, t0)];
                                body.Add(LinqExpression.Assign(Vt0, i.Right.Compile(v)));
                                v[i.Left] = Vt0;
                            }
                            
                            // Solve the remaining non-linear system with Newton's method.
                            LinqExpressions.ParameterExpression it = Declare<int>(locals, "it");
                            For(body,
                                () => body.Add(LinqExpression.Assign(it, LinqExpression.Constant(Iterations))),
                                LinqExpression.GreaterThan(it, LinqExpression.Constant(0)),
                                () => body.Add(LinqExpression.PreDecrementAssign(it)),
                                () =>
                                {
                                    // Compute one iteration of newton's method.
                                    List<Arrow> iter = nonlinear.Select(i => i.Item1).NSolve(unknowns.Select(i => Arrow.New(i, i.Evaluate(t, t0))), 1);

                                    foreach (Arrow i in iter)
                                    {
                                        LinqExpression Vt0 = globals[i.Left.Evaluate(t, t0)];
                                        body.Add(LinqExpression.Assign(Vt0, i.Right.Compile(v)));
                                        v[i.Left] = Vt0;
                                    }
                                });

                            // Update f0.
                            foreach (Tuple<Equal, Expression> i in nonlinear)
                            {
                                LinqExpression f0 = globals[i.Item2];
                                body.Add(LinqExpression.Assign(f0, i.Item1.Right.Compile(v)));
                            }
                            
                            // t0 = t
                            body.Add(LinqExpression.Assign(pt0, vt));

                            // o_i += i.Evaluate()
                            foreach (Expression i in Output)
                                body.Add(LinqExpression.AddAssign(output[i], CompileOrNaN(i, v)));
                        });

                    // Output[i][n] = o_i / Oversample
                    foreach (Expression i in Output)
                        body.Add(LinqExpression.Assign(
                            LinqExpression.MakeIndex(buffers[i], buffers[i].Type.GetProperty("Item"), new LinqExpression[] { vn }),
                            LinqExpression.Divide(output[i], LinqExpression.Constant((double)Oversample))));
                });
            
            // return t
            LinqExpressions.LabelTarget returnTo = LinqExpression.Label(vt.Type);
            body.Add(LinqExpression.Return(returnTo, vt, vt.Type));
            body.Add(LinqExpression.Label(returnTo, vt));

            // Put it all together.
            return LinqExpression.Lambda(LinqExpression.Block(locals, body), parameters);
        }

        // Generate a for loop given the header and body expressions.
        private void For(
            IList<LinqExpression> Target,
            Action Init,
            LinqExpression Condition,
            Action Step,
            Action<LinqExpressions.LabelTarget, LinqExpressions.LabelTarget> Body)
        {
            string name = Target.Count.ToString();
            LinqExpressions.LabelTarget begin = LinqExpression.Label("for_" + name + "_begin");
            LinqExpressions.LabelTarget end = LinqExpression.Label("for_" + name + "_end");

            // Generate the init code.
            Init();

            // Check the condition, exit if necessary.
            Target.Add(LinqExpression.Label(begin));
            Target.Add(LinqExpression.IfThen(LinqExpression.Not(Condition), LinqExpression.Goto(end)));

            // Generate the body code.
            Body(end, begin);

            // Generate the step code.
            Step();
            Target.Add(LinqExpression.Goto(begin));

            // Exit point.
            Target.Add(LinqExpression.Label(end));
        }

        // Generate a for loop given the header and body expressions.
        private void For(
            IList<LinqExpression> Target,
            Action Init,
            LinqExpression Condition,
            Action Step,
            Action Body)
        {
            For(Target, Init, Condition, Step, (x, y) => Body());
        }

        // Generate a while loop given the condition and body expressions.
        private void While(
            IList<LinqExpression> Target,
            LinqExpression Condition,
            Action<LinqExpressions.LabelTarget, LinqExpressions.LabelTarget> Body)
        {
            string name = (Target.Count + 1).ToString();
            LinqExpressions.LabelTarget begin = LinqExpression.Label("while_" + name + "_begin");
            LinqExpressions.LabelTarget end = LinqExpression.Label("while_" + name + "_end");

            // Check the condition, exit if necessary.
            Target.Add(LinqExpression.Label(begin));
            Target.Add(LinqExpression.IfThen(LinqExpression.Not(Condition), LinqExpression.Goto(end)));

            // Generate the body code.
            Body(end, begin);

            // Loop.
            Target.Add(LinqExpression.Goto(begin));

            // Exit label.
            Target.Add(LinqExpression.Label(end));
        }

        // Generate a while loop given the condition and body expressions.
        private void While(
            IList<LinqExpression> Target,
            LinqExpression Condition,
            Action Body)
        {
            While(Target, Condition, (x, y) => Body());
        }

        // Generate a do-while loop given the condition and body expressions.
        private void DoWhile(
            IList<LinqExpression> Target,
            Action<LinqExpressions.LabelTarget, LinqExpressions.LabelTarget> Body,
            LinqExpression Condition)
        {
            string name = (Target.Count + 1).ToString();
            LinqExpressions.LabelTarget begin = LinqExpression.Label("do_" + name + "_begin");
            LinqExpressions.LabelTarget end = LinqExpression.Label("do_" + name + "_end");

            // Check the condition, exit if necessary.
            Target.Add(LinqExpression.Label(begin));

            // Generate the body code.
            Body(end, begin);

            // Loop.
            Target.Add(LinqExpression.IfThen(Condition, LinqExpression.Goto(begin)));

            // Exit label.
            Target.Add(LinqExpression.Label(end));
        }

        // Generate a do-while loop given the condition and body expressions.
        private void DoWhile(
            IList<LinqExpression> Target,
            Action Body,
            LinqExpression Condition)
        {
            DoWhile(Target, (x, y) => Body(), Condition);
        }

        private LinqExpressions.ParameterExpression Declare<T>(IList<LinqExpressions.ParameterExpression> Scope, IDictionary<Expression, LinqExpression> Map, Expression Expr, string Name)
        {
            LinqExpressions.ParameterExpression p = LinqExpression.Parameter(typeof(T), Name);
            Scope.Add(p);
            if (Map != null)
                Map.Add(Expr, p);
            return p;
        }

        private LinqExpressions.ParameterExpression Declare<T>(IList<LinqExpressions.ParameterExpression> Scope, IDictionary<Expression, LinqExpression> Map, Expression Expr)
        {
            return Declare<T>(Scope, Map, Expr, Expr.ToString());
        }

        private LinqExpressions.ParameterExpression Declare<T>(IList<LinqExpressions.ParameterExpression> Scope, string Name)
        {
            return Declare<T>(Scope, null, null, Name);
        }

        // Compile x to an expression, or NaN if compilation fails.
        private static LinqExpression CompileOrNaN(Expression x, IDictionary<Expression, LinqExpression> v)
        {
            try
            {
                return x.Compile(v);
            }
            catch (CompileException)
            {
                return LinqExpression.Constant(double.NaN);
            }
        }

        // Shorthand for df/dx.
        private static Expression D(Expression f, Expression x) { return f.Differentiate(x); }

        // Get the expression that x is a derivative of.
        private static Expression DOf(Expression x)
        {
            Call d = (Call)x;
            if (d.Target.Name == "D")
                return d.Arguments.First();
            throw new InvalidOperationException("Expression is not a derivative");
        }
    }
}
