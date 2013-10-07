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
        private List<Arrow> linear;
        // Implicit equations describing the non-linear system.
        private List<Equal> nonlinear;
        private List<Arrow> f0;
        private List<Expression> unknowns;

        // Stores any global state in the simulation (previous state values, mostly).
        private Dictionary<Expression, GlobalExpr<double>> globals = new Dictionary<Expression, GlobalExpr<double>>();

        /// <summary>
        /// Enumerate the nodes in the simulation.
        /// </summary>
        public IEnumerable<Expression> Nodes { get { return nodes; } }

        // Given a system of potentially non-linear equations f, extract the non-linear expressions and replace them with a variable f0.
        // f0 is constructed such that ExtractNonLinear(f, x, f0).Evaluate(f0) == f.
        private static List<Equal> ExtractNonLinear(List<Equal> f, List<Expression> x, List<Arrow> f0)
        {
            List<Equal> ex = new List<Equal>();
            foreach (Equal i in f)
            {
                // Gather list of linear and non-linear terms.
                List<Expression> linear = new List<Expression>();
                List<Expression> nonlinear = new List<Expression>();
                foreach (Expression t in Add.TermsOf((i.Left - i.Right).Expand()))
                {
                    if (t.IsFunctionOf(x) && !IsLinearFunctionOf(t, x))
                        nonlinear.Add(t);
                    else
                        linear.Add(t);
                }

                // If there are any non-linear terms, create a token variable for them and add them to the linear system.
                if (nonlinear.Any())
                {
                    Variable f0i = Variable.New("f0_" + f0.Count.ToString());
                    linear.Add(f0i);
                    f0.Add(Arrow.New(f0i, Add.New(nonlinear)));

                    ex.Add(Equal.New(Add.New(linear), Constant.Zero));
                }
                else
                {
                    ex.Add(i);
                }
            }

            return ex;
        }

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

            // Separate the non-linear expressions of the KCL equations to a separate system.
            f0 = new List<Arrow>();
            kcl = ExtractNonLinear(kcl, x, f0);

            // Separate x into differential and algebraic unknowns.
            List<Expression> dx_dt = x.Where(i => IsD(i, t)).ToList();
            x.RemoveAll(i => IsD(i, t));

            // NDSolve can solve this system if algebraic variables are substituted with constants (previous value).
            List<Expression> xa = x.Where(i => dx_dt.None(j => DOf(j).Equals(i))).ToList();
            differential = kcl
                // Solve for the algebraic variables and substitute them.
                .Evaluate(kcl.Solve(xa)).OfType<Equal>()
                // Solve the resulting system of differential equations.
                .NDSolve(dx_dt.Select(i => DOf(i)).ToList(), t, t0, h, IntegrationMethod.Trapezoid);
            x.RemoveAll(i => differential.Any(j => j.Left.Equals(i)));

            // Find as many closed form solutions in the remaining system as possible.
            linear = kcl.Solve(x.Where(i => f0.None(j => j.Right.IsFunctionOf(i))));
            x.RemoveAll(i => linear.Any(j => j.Left.Equals(i)));
            linear.RemoveAll(i => i.Right.IsFunctionOf(x));

            // The remaining non-linear system will be solved via Newton's method.
            nonlinear = kcl
                .Where(i => i.IsFunctionOf(f0.Select(j => j.Left)))
                .Evaluate(f0).OfType<Equal>().ToList();
            unknowns = x;

            // Create a global variable for the value of each f0.
            foreach (Arrow i in f0)
                globals[i.Left] = new GlobalExpr<double>(0.0);
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
            parameters.Add((double)_T);
            parameters.Add(Oversample);
            parameters.Add(Iterations);
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

            LinqExpressions.LambdaExpression lambda = DefineProcessFunction(Input, Output, Parameters);
            return compiled[hash] = lambda.Compile();
        }

        private static readonly Arrow t_t0 = Arrow.New(t, t0);

        // The resulting lambda processes N samples, using buffers provided for Input and Output:
        //  double Process(int N, double t0, double T, double[] Input0 ..., double[] Output0 ..., double Parameter0 ...)
        //  { ... }
        private LinqExpressions.LambdaExpression DefineProcessFunction(IEnumerable<Expression> Input, IEnumerable<Expression> Output, IEnumerable<Expression> Parameters)
        {
            // Map expressions to identifiers in the syntax tree.
            Dictionary<Expression, LinqExpression> map = new Dictionary<Expression, LinqExpression>();
            Dictionary<Expression, LinqExpression> buffers = new Dictionary<Expression, LinqExpression>();

            // Get expressions for the state of each node. These may be replaced by input parameters.
            foreach (KeyValuePair<Expression, GlobalExpr<double>> i in globals)
                map[i.Key] = i.Value;

            // Lambda definition objects.
            List<LinqExpressions.ParameterExpression> parameters = new List<LinqExpressions.ParameterExpression>();
            List<LinqExpressions.ParameterExpression> locals = new List<LinqExpressions.ParameterExpression>();
            List<LinqExpression> body = new List<LinqExpression>();

            // Create parameters for the basic simulation info (N, t, T, Oversample, Iterations).
            LinqExpressions.ParameterExpression SampleCount = Declare<int>(parameters, "SampleCount");
            LinqExpressions.ParameterExpression t0 = Declare<double>(parameters, map, Simulation.t0);
            LinqExpressions.ParameterExpression T = Declare<double>(parameters, map, Component.T);
            LinqExpressions.ParameterExpression Oversample = Declare<int>(parameters, "Oversample");
            LinqExpressions.ParameterExpression Iterations = Declare<int>(parameters, "Iterations");
            // Create buffer parameters for each input, output.
            foreach (Expression i in Input.Concat(Output))
                Declare<double[]>(parameters, buffers, i);
            // Create constant parameters for simulation parameters.
            foreach (Expression i in Parameters)
                Declare<double>(parameters, map, i);

            // Define lambda body.

            // double t = t0
            LinqExpressions.ParameterExpression t = Declare<double>(locals, map, Simulation.t);
            body.Add(LinqExpression.Assign(t, t0));

            // double h = T / Oversample
            LinqExpressions.ParameterExpression h = Declare<double>(locals, "h");
            body.Add(LinqExpression.Assign(h, LinqExpression.Divide(T, LinqExpression.Convert(Oversample, typeof(double)))));

            // double invOversample = 1 / (double)Oversample
            LinqExpressions.ParameterExpression invOversample = Declare<double>(locals, "invOversample");
            body.Add(LinqExpression.Assign(invOversample, LinqExpression.Divide(LinqExpression.Constant(1.0), LinqExpression.Convert(Oversample, typeof(double)))));

            // Trivial timestep expressions that are not a function of the input can be set once here (outside the sample loop).
            // This might not be necessary if you trust the .Net expression compiler to lift this invariant code out of the loop.
            foreach (Arrow i in trivial.Where(i => !i.IsFunctionOf(Input)))
            {
                LinqExpression Vi = globals[i.Left.Evaluate(t_t0)];
                body.Add(LinqExpression.Assign(Vi, i.Right.Compile(map)));
                map[i.Left] = Vi;
            }

            // for (int n = 0; n < SampleCount; ++n)
            LinqExpressions.ParameterExpression n = Declare<int>(locals, "n");
            For(body,
                () => body.Add(LinqExpression.Assign(n, LinqExpression.Constant(0))),
                LinqExpression.LessThan(n, SampleCount),
                () => body.Add(LinqExpression.PreIncrementAssign(n)),
                () =>
                {
                    // Prepare input samples for oversampling interpolation.
                    Dictionary<Expression, LinqExpression> dVi = new Dictionary<Expression, LinqExpression>();
                    foreach (Expression i in Input)
                    {
                        // Ensure that we have a global variable to store the previous sample in.
                        globals[i] = new GlobalExpr<double>(0.0);
                        LinqExpression Va = globals[i];
                        LinqExpression Vb = LinqExpression.MakeIndex(
                            buffers[i],
                            buffers[i].Type.GetProperty("Item"),
                            new LinqExpression[] { n });

                        // double Vi = Va
                        body.Add(LinqExpression.Assign(
                            Declare<double>(locals, map, i, i.ToString() + "i"),
                            Va));

                        // dVi = (Vb - Vi) / Oversample.
                        body.Add(LinqExpression.Assign(
                            Declare<double>(locals, dVi, i, "d" + i.ToString()),
                            LinqExpression.Multiply(LinqExpression.Subtract(Vb, Va), invOversample)));

                        if (!nodes.Contains(i))
                        {
                            map[i.Evaluate(t_t0)] = map[i];
                            // dVi/dt = (Vb - Va) / h.
                            map[D(i, Simulation.t)] = LinqExpression.Divide(LinqExpression.Subtract(Vb, Va), h);
                        }

                        // Va = Vb
                        body.Add(LinqExpression.Assign(Va, Vb));
                    }

                    // Prepare output sample accumulators for low pass filtering.
                    Dictionary<Expression, LinqExpression> Vo = new Dictionary<Expression, LinqExpression>();
                    foreach (Expression i in Output)
                    {
                        // Vo = 0
                        body.Add(LinqExpression.Assign(
                            Declare<double>(locals, Vo, i, i.ToString()),
                            LinqExpression.Constant(0.0)));
                    }

                    // int ov = Oversample; 
                    // do { -- ov; } while(ov > 0)
                    LinqExpressions.ParameterExpression ov = Declare<int>(locals, "ov");
                    body.Add(LinqExpression.Assign(ov, Oversample));
                    DoWhile(body,
                        () =>
                        {
                            // t += h
                            body.Add(LinqExpression.AddAssign(t, h));

                            // Interpolate the input samples.
                            // Vi += dVi
                            foreach (Expression i in Input)
                                body.Add(LinqExpression.AddAssign(map[i], dVi[i]));

                            // Compile the trivial timestep expressions that are a function of the input.
                            foreach (Arrow i in trivial.Where(i => i.Right.IsFunctionOf(Input)))
                            {
                                LinqExpression Vi = globals[i.Left.Evaluate(t_t0)];
                                body.Add(LinqExpression.Assign(Vi, i.Right.Compile(map)));
                                map[i.Left] = Vi;
                            }

                            // Compile the differential timestep expressions.
                            foreach (Arrow i in differential)
                            {
                                LinqExpression Vt0 = globals[i.Left.Evaluate(t_t0)];
                                LinqExpression Vt = Declare(locals, body, i.Left.ToString(), i.Right.Compile(map));
                                // Compute the value of v'(t) and store it in the map.
                                LinqExpression dV = Declare<double>(locals, "d" + i.Left.ToString());
                                body.Add(LinqExpression.Assign(dV, LinqExpression.Divide(LinqExpression.Subtract(Vt, Vt0), h)));
                                map[D(i.Left, Simulation.t)] = dV;

                                // Update Vt0.
                                body.Add(LinqExpression.Assign(Vt0, Vt));
                                map[i.Left] = Vt0;
                            }

                            // And the linear timestep expressions.
                            foreach (Arrow i in linear)
                            {
                                LinqExpression Vt0 = globals[i.Left.Evaluate(t_t0)];
                                body.Add(LinqExpression.Assign(Vt0, i.Right.Compile(map)));
                                map[i.Left] = Vt0;
                            }

                            if (unknowns.Any())
                            {
                                // Solve the remaining non-linear system with Newton's method.

                                // int it = Oversample
                                // do { ... --it } while(it > 0)
                                LinqExpressions.ParameterExpression it = Declare<int>(locals, "it");
                                body.Add(LinqExpression.Assign(it, Iterations));
                                DoWhile(body,
                                    () =>
                                    {
                                        // Compute one iteration of Newton's method.
                                        List<Arrow> iter = nonlinear.NSolve(unknowns.Select(i => Arrow.New(i, i.Evaluate(t_t0))), 1);

                                        foreach (Arrow i in iter)
                                        {
                                            LinqExpression Vt0 = globals[i.Left.Evaluate(t_t0)];
                                            body.Add(LinqExpression.Assign(Vt0, i.Right.Compile(map)));
                                            map[i.Left] = Vt0;
                                        }

                                        // --it;
                                        body.Add(LinqExpression.PreDecrementAssign(it));
                                    },
                                    LinqExpression.GreaterThan(it, LinqExpression.Constant(0)));

                                // Update f0.
                                foreach (Arrow i in f0)
                                {
                                    LinqExpression f0i = globals[i.Left];
                                    body.Add(LinqExpression.Assign(f0i, i.Right.Compile(map)));
                                }
                            }

                            // t0 = t
                            body.Add(LinqExpression.Assign(t0, t));

                            // o_i += i.Evaluate()
                            foreach (Expression i in Output)
                                body.Add(LinqExpression.AddAssign(Vo[i], CompileOrNaN(i, map)));

                            // --ov;
                            body.Add(LinqExpression.PreDecrementAssign(ov));
                        },
                        LinqExpression.GreaterThan(ov, LinqExpression.Constant(0)));

                    // Output[i][n] = o_i / Oversample
                    foreach (Expression i in Output)
                        body.Add(LinqExpression.Assign(
                            LinqExpression.MakeIndex(buffers[i], buffers[i].Type.GetProperty("Item"), new LinqExpression[] { n }),
                            LinqExpression.Multiply(Vo[i], invOversample)));
                });

            // return t
            LinqExpressions.LabelTarget returnTo = LinqExpression.Label(t.Type);
            body.Add(LinqExpression.Return(returnTo, t, t.Type));
            body.Add(LinqExpression.Label(returnTo, t));

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

        private static LinqExpressions.ParameterExpression Declare<T>(IList<LinqExpressions.ParameterExpression> Scope, IDictionary<Expression, LinqExpression> Map, Expression Expr, string Name)
        {
            LinqExpressions.ParameterExpression p = LinqExpression.Parameter(typeof(T), Name);
            Scope.Add(p);
            if (Map != null)
                Map.Add(Expr, p);
            return p;
        }

        private static LinqExpressions.ParameterExpression Declare<T>(IList<LinqExpressions.ParameterExpression> Scope, IDictionary<Expression, LinqExpression> Map, Expression Expr)
        {
            return Declare<T>(Scope, Map, Expr, Expr.ToString());
        }

        private static LinqExpressions.ParameterExpression Declare<T>(IList<LinqExpressions.ParameterExpression> Scope, string Name)
        {
            return Declare<T>(Scope, null, null, Name);
        }

        private static LinqExpressions.ParameterExpression Declare(IList<LinqExpressions.ParameterExpression> Scope, IList<LinqExpression> Target, string Name, LinqExpression Init)
        {
            LinqExpressions.ParameterExpression p = LinqExpression.Parameter(Init.Type, Name);
            Scope.Add(p);
            Target.Add(LinqExpression.Assign(p, Init));
            return p;
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
        private static Expression D(Expression f, Expression x) { return Call.D(f, x); }

        // Check if x is a derivative
        private static bool IsD(Expression f, Expression x)
        {
            Call C = f as Call;
            if (!ReferenceEquals(C, null))
                return C.Target.Name == "D" && C.Arguments[1].Equals(x);
            return false;
        }

        // Get the expression that x is a derivative of.
        private static Expression DOf(Expression x)
        {
            Call d = (Call)x;
            if (d.Target.Name == "D")
                return d.Arguments.First();
            throw new InvalidOperationException("Expression is not a derivative");
        }

        // Test if f is a linear function of x.
        private static bool IsLinearFunctionOf(Expression f, IEnumerable<Expression> x)
        {
            foreach (Expression i in x)
            {
                // TODO: There must be a more efficient way to do this...
                Expression fi = f / i;
                if (!fi.IsFunctionOf(i))
                    return true;

                //if (Add.TermsOf(f).Count(j => Multiply.TermsOf(j).Sum(k => k.Equals(i) ? 1 : k.IsFunctionOf(i) ? 2 : 0) == 1) == 1)
                //    return true;
            }
            return false;
        }
    }
}
