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
using ParameterExpression = System.Linq.Expressions.ParameterExpression;

namespace Circuit
{
    /// <summary>
    /// Simulate a circuit.
    /// </summary>
    public class Simulation
    {
        // Expression for t at the previous timestep.
        private static readonly Variable t0 = Variable.New("t0");
        private static readonly Variable t = Component.t;
        
        // This is used often enough to shorten it a few characters.
        private static readonly Arrow t_t0 = Arrow.New(t, t0);

        protected long n = 0;
        /// <summary>
        /// Get the current time of the simulation.
        /// </summary>
        public long Sample { get { return n; } }
        public double Time { get { return (double)n * T; } }

        protected double T;
        /// <summary>
        /// Get the timestep for the simulation.
        /// </summary>
        public double TimeStep { get { return T; } }
        
        protected int oversample;
        /// <summary>
        /// Get the oversampling factor for the simulation.
        /// </summary>
        public int Oversample { get { return oversample; } }
        
        private List<Expression> nodes;
        /// <summary>
        /// Enumerate the nodes in the simulation.
        /// </summary>
        public IEnumerable<Expression> Nodes { get { return nodes; } }

        private ILog log = new ConsoleLog();
        /// <summary>
        /// Get or set the log associated with this simulation.
        /// </summary>
        public ILog Log { get { return log; } set { log = value; } }

        // Simulation timestep.
        private Expression h;

        // Block of equations to solve, with some solutions that are linear combinations of the non-linear solutions.
        private class AlgebraicSystem
        {
            private List<Equal> nonlinear;
            private List<Expression> unknowns;
            private List<Arrow> linear;

            public IEnumerable<Equal> Nonlinear { get { return nonlinear; } }
            public IEnumerable<Expression> Unknowns { get { return unknowns; } }
            public IEnumerable<Arrow> Linear { get { return linear; } }

            public AlgebraicSystem(List<Equal> System, List<Expression> Unknowns, List<Arrow> Linear) 
            {
                nonlinear = System; 
                unknowns = Unknowns;
                linear = Linear;
            }

            public bool DependsOn(Expression x) { return nonlinear.Any(i => i.DependsOn(x)); }
        }

        // Expressions for trivial solutions to the system.
        private List<Arrow> trivial;
        // Expressions for the solution of the differential equations.
        private List<Arrow> differential;
        // Expressions for algebraic solutions.
        private List<AlgebraicSystem> algebraic;
        // Expressions for the voltage of each two terminal component.
        private List<Arrow> components;

        private List<Arrow> f0;
        
        // Stores any global state in the simulation (previous state values, mostly).
        private Dictionary<Expression, GlobalExpr<double>> globals = new Dictionary<Expression, GlobalExpr<double>>();

        // Given a system of potentially non-linear equations f, extract the non-linear expressions and replace them with a variable f0.
        // f0 is constructed such that ExtractNonLinear(f, x, f0).Evaluate(f0) == f.
        private static List<Equal> ExtractNonLinear(List<Equal> S, List<Expression> y, List<Arrow> f)
        {
            List<Equal> ret = new List<Equal>();
            foreach (Equal i in S)
            {
                // Gather list of linear and non-linear terms.
                List<Expression> linear = new List<Expression>();
                List<Expression> nonlinear = new List<Expression>();
                foreach (Expression t in Add.TermsOf((i.Left - i.Right).Expand()))
                {
                    if (t.DependsOn(y) && !IsLinearFunctionOf(t, y))
                        nonlinear.Add(t);
                    else
                        linear.Add(t);
                }

                // If there are any non-linear terms, create a token variable for them and add them to the linear system.
                if (nonlinear.Any())
                {
                    Expression fi = "f" + f.Count;
                    linear.Add(fi);
                    f.Add(Arrow.New(fi, Add.New(nonlinear)));

                    ret.Add(Equal.New(Add.New(linear), Constant.Zero));
                }
                else
                {
                    ret.Add(i);
                }
            }

            return ret;
        }
        
        /// <summary>
        /// Create a simulation for the given circuit.
        /// </summary>
        /// <param name="C">Circuit to simulate.</param>
        /// <param name="T">Sampling period.</param>
        public Simulation(Circuit Circuit, Quantity SampleRate, int Oversample, ILog Log)
        {
            log = Log;
            oversample = Oversample;
            T = 1.0 / (double)SampleRate;
            nodes = Circuit.Nodes.Select(i => (Expression)Call.New(((Call)i.V).Target, t)).ToList();

            // Length of one timestep in the oversampled simulation.
            h = 1 / ((Expression)SampleRate * Oversample);

            log.WriteLine(MessageType.Info, "--------");
            LogTime(MessageType.Info, "Building simulation for circuit '" + Circuit.Name + "', f=" + SampleRate.ToString() + " x " + Oversample, true);

            LogTime(MessageType.Info, "Performing MNA on circuit...");

            // Analyze the circuit to get the MNA system and unknowns.
            List<Expression> y = new List<Expression>();
            List<Equal> mna = new List<Equal>();
            Circuit.Analyze(mna, y);
            LogTime(MessageType.Info, "Done.");
            LogExpressions("MNA system of " + mna.Count + " equations and " + y.Count + " unknowns {{" + y.UnSplit(", ") + "}}:", mna);

            LogTime(MessageType.Info, "Solving MNA system...");

            // Find trivial solutions for y and substitute them into the system.
            trivial = mna.Solve(y);
            trivial.RemoveAll(i => i.Right.DependsOn(y));
            mna = mna.Evaluate(trivial).OfType<Equal>().ToList();
            y.RemoveAll(i => trivial.Any(j => j.Left.Equals(i)));
            LogExpressions("Trivial solutions:", trivial);

            // Linearize the system for solving the differential equations by replacing 
            // non-linear terms with constants (from the previous timestep).
            f0 = new List<Arrow>();
            List<Equal> linearized = ExtractNonLinear(mna, y, f0);
            LogExpressions("Linearized system:", linearized);
            LogExpressions("Non-linear terms:", f0);
            
            // Separate y into differential and algebraic unknowns.
            List<Expression> dy_dt = y.Where(i => IsD(i, t)).ToList();
            y.RemoveAll(i => IsD(i, t));
            differential = linearized
                // Solve for the algebraic unknowns in terms of the rest and substitute them.
                .Evaluate(linearized.Solve(y.Where(i => dy_dt.None(j => DOf(j).Equals(i))))).OfType<Equal>()
                // Solve the resulting system of differential equations.
                .NDPartialSolve(dy_dt.Select(i => DOf(i)), t, t0, h, IntegrationMethod.Trapezoid);
            y.RemoveAll(i => differential.Any(j => j.Left.Equals(i)));
            LogExpressions("Differential solutions:", differential);
            // Create global variables for the previous value of each differential solution.
            foreach (Arrow i in differential)
                globals[i.Left.Evaluate(t, t0)] = new GlobalExpr<double>(0.0);

            // After solving for the differential unknowns, divide them by h so we don't have 
            // to do it during simulation. It's faster to simulate, and we get the benefits of 
            // arbitrary precision calculations here.
            mna = mna.Evaluate(dy_dt.Select(i => Arrow.New(i, i / h))).Cast<Equal>().ToList();
            f0 = f0.Evaluate(dy_dt.Select(i => Arrow.New(i, i / h))).Cast<Arrow>().ToList();


            // Solve the algebraic system.
            algebraic = new List<AlgebraicSystem>();

            // Find the minimum set of unknowns requiring non-linear methods.
            List<Expression> unknowns = y.Where(i => f0.Any(j => j.DependsOn(i))).ToList();
            List<Expression> nonlinear = mna.Evaluate(mna.Solve(y.Except(unknowns))).OfType<Equal>().Select(i => i.Left - i.Right).ToList();
            while (y.Any())
            {
                // Find the smallest independent system we can to numerically solve.
                Tuple<List<Expression>, List<Expression>> next = nonlinear.Select(f =>
                {
                    // Starting with f, find the minimal set of equations necessary to solve the system.
                    // Find the unknowns in this equation.
                    List<Expression> fy = y.Where(i => f.DependsOn(i)).ToList();
                    List<Expression> system = new List<Expression>() { f };

                    // While we have fewer equations than variables...
                    while (system.Count < fy.Count)
                    {
                        // Find the equation that will introduce the fewest variables to the system.
                        IEnumerable<Expression> ry = y.Except(fy, ExprRefEquality);
                        List<Expression> candidates = nonlinear.Except(system, ExprRefEquality).ToList();
                        if (candidates.Any())
                        {
                            Expression add = candidates.ArgMin(i => ry.Count(j => j.DependsOn(i)));
                            system.Add(add);
                            fy.AddRange(ry.Where(i => add.DependsOn(i)));
                        }
                    }

                    return new Tuple<List<Expression>, List<Expression>>(system, fy);
                }).ArgMin(i => i.Item1.Count());

                // block is a subset of the system that we can solve for by with.
                List<Expression> block = next.Item1;
                List<Expression> by = next.Item2;

                // Remove these from the system.
                nonlinear.RemoveAll(i => block.Contains(i));
                y.RemoveAll(i => by.Contains(i));

                // Try solving+substituting linear solutions first.
                List<Arrow> linear = block.Select(i => Equal.New(i, 0)).Solve(by).ToList();
                linear.RemoveAll(i => i.Right.DependsOn(by));
                by.RemoveAll(i => linear.Any(j => j.Left == i));
                block = block.Evaluate(linear).Where(i => i.DependsOn(by)).ToList();
                
                // Now that we know solutions to by, we might be able to find other solutions too.
                linear.AddRange(IndependentSolutions(mna.PartialSolve(y), y));
                y.RemoveAll(i => linear.Any(j => j.Left == i));

                LogExpressions("Nonlinear system of " + block.Count + " equations and " + by.Count + " unknowns {{" + by.UnSplit(", ") + "}}:", block.Select(i => Equal.New(i, 0)));
                LogExpressions("Linear solutions (including MNA system):", linear);
                
                algebraic.Add(new AlgebraicSystem(
                    block.NewtonRhapson(by.Select(i => Arrow.New(i, i.Evaluate(t, t0)))),
                    by,
                    linear));

                // Create global variables for iterative unknowns
                foreach (Expression i in by)
                    globals[i.Evaluate(t, t0)] = new GlobalExpr<double>(0.0);
            }
            
            // Create a global variable for the value of each f0.
            foreach (Arrow i in f0)
                globals[i.Left] = new GlobalExpr<double>(0.0);
            
            LogTime(MessageType.Info, "System solved.");
            
            // Add solutions for the voltage across all the components.
            components = Circuit.Components.OfType<TwoTerminal>()
                .Select(i => Arrow.New(DependentVariable(i.Name, t), i.V.Evaluate(trivial)))
                .ToList();
            LogExpressions("Component voltages:", components);
        }

        /// <summary>
        /// Clear all state from the simulation.
        /// </summary>
        public void Reset()
        {
            n = 0;
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
        public void Process(
            int N,
            IEnumerable<KeyValuePair<Expression, double[]>> Input, 
            IEnumerable<KeyValuePair<Expression, double[]>> Output, 
            IEnumerable<Arrow> Arguments, 
            int Iterations)
        {
            Delegate processor = Compile(Input.Select(i => i.Key), Output.Select(i => i.Key), Arguments.Select(i => i.Left));

            // Build parameter list for the processor.
            List<object> parameters = new List<object>(3 + Input.Count() + Output.Count() + Arguments.Count());
            parameters.Add(N);
            parameters.Add((double)n * T);
            parameters.Add(T);
            parameters.Add(Oversample);
            parameters.Add(Iterations);
            foreach (KeyValuePair<Expression, double[]> i in Input)
                parameters.Add(i.Value);
            foreach (KeyValuePair<Expression, double[]> i in Output)
                parameters.Add(i.Value);
            if (Arguments != null)
                foreach (Arrow i in Arguments)
                    parameters.Add((double)i.Right);

            processor.DynamicInvoke(parameters.ToArray());

            // Check the last samples for infinity/NaN.
            foreach (KeyValuePair<Expression, double[]> i in Output)
            {
                double v = i.Value[i.Value.Length - 1];
                if (double.IsInfinity(v) || double.IsNaN(v))
                    throw new OverflowException("Simulation diverged after t=" + Quantity.ToString(n * T, Units.s));
            }

            n += N;
        }

        private static Arrow[] NoArguments = new Arrow[] { };
        public void Process(
            int N,
            IEnumerable<KeyValuePair<Expression, double[]>> Input,
            IEnumerable<KeyValuePair<Expression, double[]>> Output, 
            int Iterations)
        {
            Process(N, Input, Output, NoArguments, Iterations);
        }

        public void Process(
            Expression InputNode, double[] InputSamples,
            IEnumerable<KeyValuePair<Expression, double[]>> Output, 
            int Iterations)
        {
            Process(
                InputSamples.Length,
                new KeyValuePair<Expression, double[]>[] { new KeyValuePair<Expression, double[]> (InputNode, InputSamples) },
                Output,
                Iterations);
        }

        public void Process(
            Expression InputNode, double[] InputSamples, 
            Expression OutputNode, double[] OutputSamples,
            int Iterations)
        {
            Process(
                InputSamples.Length,
                new KeyValuePair<Expression, double[]>[] { new KeyValuePair<Expression, double[]>(InputNode, InputSamples) },
                new KeyValuePair<Expression, double[]>[] { new KeyValuePair<Expression, double[]>(OutputNode, OutputSamples) },
                Iterations);
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

            LogTime(MessageType.Info, "Compiling simulation...", true);
            LogExpressions("Input:", Input);
            LogExpressions("Output:", Output);
            LogExpressions("Parameters:", Parameters);

            LogTime(MessageType.Info, "Defining sample processing function...");
            LinqExpressions.LambdaExpression lambda = DefineProcessFunction(Input, Output, Parameters);
            LogTime(MessageType.Info, "Compiling sample processing function...");
            d = lambda.Compile();
            LogTime(MessageType.Info, "Done.");

            return compiled[hash] = d;
        }
        
        // The resulting lambda processes N samples, using buffers provided for Input and Output:
        //  void Process(int N, double t0, double T, double[] Input0 ..., double[] Output0 ..., double Parameter0 ...)
        //  { ... }
        private LinqExpressions.LambdaExpression DefineProcessFunction(IEnumerable<Expression> Input, IEnumerable<Expression> Output, IEnumerable<Expression> Parameters)
        {
            // Map expressions to identifiers in the syntax tree.
            Dictionary<Expression, LinqExpression> map = new Dictionary<Expression, LinqExpression>();
            Dictionary<Expression, LinqExpression> buffers = new Dictionary<Expression, LinqExpression>();

            
            // Lambda definition objects.
            List<ParameterExpression> parameters = new List<ParameterExpression>();
            List<ParameterExpression> locals = new List<ParameterExpression>();
            List<LinqExpression> body = new List<LinqExpression>();

            // Create parameters for the basic simulation info (N, t, T, Oversample, Iterations).
            ParameterExpression SampleCount = Declare<int>(parameters, "SampleCount");
            ParameterExpression t0 = Declare<double>(parameters, map, Simulation.t0);
            ParameterExpression T = Declare<double>(parameters, map, Component.T);
            ParameterExpression Oversample = Declare<int>(parameters, "Oversample");
            ParameterExpression Iterations = Declare<int>(parameters, "Iterations");
            // Create buffer parameters for each input, output.
            foreach (Expression i in Input.Concat(Output))
                Declare<double[]>(parameters, buffers, i);
            // Create constant parameters for simulation parameters.
            foreach (Expression i in Parameters)
                Declare<double>(parameters, map, i);

            // Create globals to store previous values of input.
            foreach (Expression i in Input)
                globals[i.Evaluate(t_t0)] = new GlobalExpr<double>(0.0);

            // Define lambda body.

            // double t = t0
            ParameterExpression t = Declare<double>(locals, map, Simulation.t);
            body.Add(LinqExpression.Assign(t, t0));

            // double h = T / Oversample
            ParameterExpression h = Declare<double>(locals, "h");
            body.Add(LinqExpression.Assign(h, LinqExpression.Divide(T, LinqExpression.Convert(Oversample, typeof(double)))));

            // double invOversample = 1 / (double)Oversample
            ParameterExpression invOversample = Declare<double>(locals, "invOversample");
            body.Add(LinqExpression.Assign(invOversample, Reciprocal(LinqExpression.Convert(Oversample, typeof(double)))));

            // Load the globals to local variables and add them to the map.
            foreach (KeyValuePair<Expression, GlobalExpr<double>> i in globals)
                body.Add(LinqExpression.Assign(Declare<double>(locals, map, i.Key), i.Value));

            // Trivial timestep expressions that are not a function of the input or t can be set once here (outside the sample loop).
            // This might not be necessary if you trust the .Net expression compiler to lift this invariant code out of the loop.
            foreach (Arrow i in trivial.Where(i => !i.Right.DependsOn(Input) && !i.Right.DependsOn(Component.t)))
                body.Add(LinqExpression.Assign(Declare<double>(locals, map, i.Left), i.Right.Compile(map)));

            // for (int n = 0; n < SampleCount; ++n)
            ParameterExpression n = Declare<int>(locals, "n");
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
                        LinqExpression Va = map[i.Evaluate(t_t0)];
                        LinqExpression Vb = LinqExpression.MakeIndex(buffers[i], buffers[i].Type.GetProperty("Item"), new LinqExpression[] { n });

                        // double Vi = Va
                        body.Add(LinqExpression.Assign(Declare<double>(locals, map, i, i.ToString()), Va));

                        // dVi = (Vb - Vi) / Oversample.
                        body.Add(LinqExpression.Assign(
                            Declare<double>(locals, dVi, i, "d" + i.ToString().Replace("[t]", "")),
                            LinqExpression.Multiply(LinqExpression.Subtract(Vb, Va), invOversample)));

                        // Va = Vb
                        body.Add(LinqExpression.Assign(Va, Vb));
                    }

                    // Prepare output sample accumulators for low pass filtering.
                    Dictionary<Expression, LinqExpression> Vo = new Dictionary<Expression, LinqExpression>();
                    foreach (Expression i in Output)
                        body.Add(LinqExpression.Assign(
                            Declare<double>(locals, Vo, i, i.ToString().Replace("[t]", "")),
                            LinqExpression.Constant(0.0)));

                    // int ov = Oversample; 
                    // do { -- ov; } while(ov > 0)
                    ParameterExpression ov = Declare<int>(locals, "ov");
                    body.Add(LinqExpression.Assign(ov, Oversample));
                    DoWhile(body,
                        () =>
                        {
                            // t += h
                            body.Add(LinqExpression.AddAssign(t, h));

                            // Interpolate the input samples.
                            foreach (Expression i in Input)
                                body.Add(LinqExpression.AddAssign(map[i], dVi[i]));

                            // Compile the trivial timestep expressions that are a function of the input.
                            foreach (Arrow i in trivial.Where(i => !map.ContainsKey(i.Left)))
                                body.Add(LinqExpression.Assign(Declare<double>(locals, map, i.Left), i.Right.Compile(map)));

                            // Compile the differential timestep expressions.
                            foreach (Arrow i in differential)
                            {
                                body.Add(LinqExpression.Assign(
                                    Declare<double>(locals, map, i.Left),
                                    i.Right.Compile(map)));

                                Expression di_dt = D(i.Left, Simulation.t);
                                LinqExpression Vt0 = map[i.Left.Evaluate(t_t0)];

                                // double dV = (Vt - Vt0) / h, but we already divided by h when solving the system.
                                body.Add(LinqExpression.Assign(
                                    Declare<double>(locals, map, di_dt, "d" + i.Left.ToString().Replace("[t]", "")), 
                                    LinqExpression.Subtract(map[i.Left], Vt0)));
                            }
                            // Vt0 = Vt
                            foreach (Arrow i in differential)
                                body.Add(LinqExpression.Assign(map[i.Left.Evaluate(t_t0)], map[i.Left]));

                            // int it
                            ParameterExpression it = Declare<int>(locals, "it");

                            // Compile the algebraic systems' solutions.
                            foreach (AlgebraicSystem i in algebraic)
                            {
                                // it = Oversample
                                // do { ... --it } while(it > 0)
                                body.Add(LinqExpression.Assign(it, Iterations));
                                DoWhile(body,
                                    () =>
                                    {
                                        // Compile the numerical scheme to solve this system.
                                        List<Arrow> iteration = i.Nonlinear.Solve(i.Unknowns);
                                        foreach (Arrow j in iteration)
                                        {
                                            LinqExpression Vt0 = map[j.Left.Evaluate(t_t0)];
                                            body.Add(LinqExpression.Assign(Vt0, j.Right.Compile(map)));
                                            map[j.Left] = Vt0;
                                        }

                                        // --it;
                                        body.Add(LinqExpression.PreDecrementAssign(it));
                                    },
                                    LinqExpression.GreaterThan(it, LinqExpression.Constant(0)));

                                // Compile the linear solutions.
                                foreach (Arrow j in i.Linear)
                                    body.Add(LinqExpression.Assign(Declare<double>(locals, map, j.Left), j.Right.Compile(map)));
                            }

                            // Update f0.
                            foreach (Arrow i in f0)
                                body.Add(LinqExpression.Assign(map[i.Left], i.Right.Compile(map)));

                            // Compile the component voltage expressions.
                            foreach (Arrow i in components.Where(i => !map.ContainsKey(i.Left)))
                                body.Add(LinqExpression.Assign(Declare<double>(locals, map, i.Left), i.Right.Compile(map)));

                            // t0 = t
                            body.Add(LinqExpression.Assign(t0, t));

                            // Vo += i
                            foreach (Expression i in Output)
                                body.Add(LinqExpression.AddAssign(Vo[i], CompileOrWarn(i, map)));

                            // Vi_t0 = Vi
                            foreach (Expression i in Input)
                                body.Add(LinqExpression.Assign(map[i.Evaluate(t_t0)], map[i]));

                            // --ov;
                            body.Add(LinqExpression.PreDecrementAssign(ov));
                        },
                        LinqExpression.GreaterThan(ov, LinqExpression.Constant(0)));

                    // Output[i][n] = Vo / Oversample
                    foreach (Expression i in Output)
                    {
                        body.Add(LinqExpression.Assign(
                            LinqExpression.MakeIndex(buffers[i], buffers[i].Type.GetProperty("Item"), new LinqExpression[] { n }),
                            LinqExpression.Multiply(Vo[i], invOversample)));
                    }
                });

            // Copy the global state variables back to the globals.
            foreach (KeyValuePair<Expression, GlobalExpr<double>> i in globals)
                body.Add(LinqExpression.Assign(i.Value, map[i.Key]));
            
            // Put it all together.
            return LinqExpression.Lambda(LinqExpression.Block(locals, body), parameters);
        }
        
        // If x fails to compile, return 0. 
        private LinqExpression CompileOrWarn(Expression x, IDictionary<Expression, LinqExpression> map)
        {
            try
            {
                return x.Compile(map);
            }
            catch (System.Exception ex)
            {
                Log.WriteLine(MessageType.Warning, "Error compiling output expression '{0}': {1}", x.ToString(), ex.Message);
                return LinqExpression.Constant(0.0);
            }
        }

        // Generate a for loop given the header and body expressions.
        private static void For(
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
        private static void For(
            IList<LinqExpression> Target,
            Action Init,
            LinqExpression Condition,
            Action Step,
            Action Body)
        {
            For(Target, Init, Condition, Step, (x, y) => Body());
        }

        // Generate a while loop given the condition and body expressions.
        private static void While(
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
        private static void While(
            IList<LinqExpression> Target,
            LinqExpression Condition,
            Action Body)
        {
            While(Target, Condition, (x, y) => Body());
        }

        // Generate a do-while loop given the condition and body expressions.
        private static void DoWhile(
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
        private static void DoWhile(
            IList<LinqExpression> Target,
            Action Body,
            LinqExpression Condition)
        {
            DoWhile(Target, (x, y) => Body(), Condition);
        }

        private static ParameterExpression Declare<T>(IList<ParameterExpression> Scope, IDictionary<Expression, LinqExpression> Map, Expression Expr, string Name)
        {
            ParameterExpression p = LinqExpression.Parameter(typeof(T), Name);
            Scope.Add(p);
            if (Map != null)
                Map.Add(Expr, p);
            return p;
        }

        private static ParameterExpression Declare<T>(IList<ParameterExpression> Scope, IDictionary<Expression, LinqExpression> Map, Expression Expr)
        {
            return Declare<T>(Scope, Map, Expr, Expr.ToString());
        }

        private static ParameterExpression Declare<T>(IList<ParameterExpression> Scope, string Name)
        {
            return Declare<T>(Scope, null, null, Name);
        }

        private static ParameterExpression Declare(IList<ParameterExpression> Scope, IList<LinqExpression> Target, string Name, LinqExpression Init)
        {
            ParameterExpression p = LinqExpression.Parameter(Init.Type, Name);
            Scope.Add(p);
            Target.Add(LinqExpression.Assign(p, Init));
            return p;
        }
        
        // Logging helpers.
        private void LogExpressions(string Title, IEnumerable<Expression> Expressions)
        {
            if (Expressions.Any())
            {
                log.WriteLine(MessageType.Info, Title);
                foreach (Expression i in Expressions)
                    log.WriteLine(MessageType.Info, "  " + i.ToString());
                log.WriteLine(MessageType.Info, "");
            }
        }

        private System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
        private void LogTime(MessageType Type, string Message, bool Reset)
        {
            if (Reset)
            {
                watch.Reset();
                watch.Start();
            }
            log.WriteLine(Type, "[" + watch.ElapsedMilliseconds + " ms] " + Message);
        }
        private void LogTime(MessageType Type, string Message) { LogTime(Type, Message, false); }

        //private void Log(string Text, params object[] Format) { log.WriteLine(MessageType.Info, Text, Format); }

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

        // Make a variable Name dependent on On.
        private static Call DependentVariable(string Name, params Variable[] On) 
        { 
            return Call.New(ExprFunction.New(Name, On), On); 
        }

        // Returns 1 / x.
        private static LinqExpression Reciprocal(LinqExpression x)
        {
            LinqExpression one = null;
            if (x.Type == typeof(double))
                one = LinqExpression.Constant(1.0);
            else if (x.Type == typeof(float))
                one = LinqExpression.Constant(1.0f);
            return LinqExpression.Divide(one, x);
        }

        // Test if f is a linear function of x.
        private static bool IsLinearFunctionOf(Expression f, IEnumerable<Expression> x)
        {
            foreach (Expression i in x)
            {
                // TODO: There must be a more efficient way to do this...
                Expression fi = f / i;
                if (!fi.DependsOn(i))
                    return true;

                //if (Add.TermsOf(f).Count(j => Multiply.TermsOf(j).Sum(k => k.Equals(i) ? 1 : k.IsFunctionOf(i) ? 2 : 0) == 1) == 1)
                //    return true;
            }
            return false;
        }

        // Filters the solutions in S that are dependent on x if evaluated in order.
        private static IEnumerable<Arrow> IndependentSolutions(IEnumerable<Arrow> S, IEnumerable<Expression> x)
        {
            foreach (Arrow i in S)
            {
                if (!i.Right.DependsOn(x))
                {
                    yield return i;
                    x = x.Except(i.Left);
                }
            }
        }

        private static IEnumerable<T> Concat<T>(IEnumerable<T> x0, params IEnumerable<T>[] x)
        {
            IEnumerable<T> concat = x0;
            foreach (IEnumerable<T> i in x)
                concat = concat.Concat(i);
            return concat;
        }

        private static T Pop<T>(List<T> From)
        {
            T p = From.Last();
            From.RemoveAt(From.Count - 1);
            return p;
        }

        private static IEqualityComparer<Expression> ExprRefEquality = new ReferenceEqualityComparer<Expression>();
    }
}
