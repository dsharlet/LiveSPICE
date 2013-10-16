using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Reflection.Emit;
using SyMath;

namespace Circuit
{
    /// <summary>
    /// Simulate a circuit.
    /// </summary>
    public abstract class Simulation
    {
        // Expression for t at the previous timestep.
        protected static readonly Variable t0 = Variable.New("t0");
        protected static readonly Variable t = Component.t;
        
        // This is used often enough to shorten it a few characters.
        protected static readonly Arrow t_t0 = Arrow.New(t, t0);

        private long n = 0;
        /// <summary>
        /// Get the current time of the simulation.
        /// </summary>
        public long Sample { get { return n; } }
        public double Time { get { return (double)n * T; } }

        private double T;
        /// <summary>
        /// Get the timestep for the simulation.
        /// </summary>
        public double TimeStep { get { return T; } }

        private int oversample;
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
        protected class AlgebraicSystem
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
        protected List<Arrow> trivial;
        // Expressions for the solution of the differential equations.
        protected List<Arrow> differential;
        // Expressions for algebraic solutions.
        protected List<AlgebraicSystem> algebraic;
        // Expressions for the voltage of each two terminal component.
        protected List<Arrow> components;

        protected List<Arrow> f0;
        
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

            LogTime(MessageType.Info, "Building simulation for circuit '" + Circuit.Name + "', f=" + SampleRate.ToString() + " x " + Oversample, true);

            LogTime(MessageType.Info, "Performing MNA on circuit...");

            // Analyze the circuit to get the MNA system and unknowns.
            List<Expression> y = new List<Expression>();
            List<Equal> mna = new List<Equal>();
            Circuit.Analyze(mna, y);
            LogTime(MessageType.Info, "Done.");
            LogExpressions("MNA system of " + mna.Count + " equations and " + y.Count + " unknowns {{" + y.UnSplit(", ") + "}}:", mna);

            LogTime(MessageType.Info, "Solving MNA system...");

            // Separate y into differential and algebraic unknowns.
            List<Expression> dy_dt = y.Where(i => mna.Any(j => j.DependsOn(D(i, t)))).Select(i => D(i, t)).ToList();
            y.AddRange(dy_dt);

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

            // Solve for differential solutions to the linearized system.
            y.RemoveAll(i => IsD(i, t));
            differential = linearized
                // Solve for the algebraic unknowns in terms of the rest and substitute them.
                .Evaluate(linearized.Solve(y.Where(i => dy_dt.None(j => DOf(j).Equals(i))))).OfType<Equal>()
                // Solve the resulting system of differential equations.
                .NDPartialSolve(dy_dt.Select(i => DOf(i)), t, t0, h, IntegrationMethod.Trapezoid);
            y.RemoveAll(i => differential.Any(j => j.Left.Equals(i)));
            LogExpressions("Differential solutions:", differential);

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
                        if (!candidates.Any())
                            throw new AlgebraException("Underdeterined MNA system");
                        Expression add = candidates.ArgMin(i => ry.Count(j => j.DependsOn(i)));
                        system.Add(add);
                        fy.AddRange(ry.Where(i => add.DependsOn(i)));
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

                //for (int r = 1; r <= y.Count; ++r)
                //{
                //    foreach (IEnumerable<Expression> yi in y.Combinations(r))
                //    {
                //        List<Arrow> s = IndependentSolutions(mna.PartialSolve(yi), y).ToList();
                //        if (!s.Empty())
                //        {
                //            linear.AddRange(s);
                //            y.RemoveAll(i => s.Any(j => j.Left == i));
                //            r = 0;
                //            break;
                //        }
                //    }
                //}

                LogExpressions("Nonlinear system of " + block.Count + " equations and " + by.Count + " unknowns {{" + by.UnSplit(", ") + "}}:", block.Select(i => Equal.New(i, 0)));
                LogExpressions("Linear solutions (including MNA system):", linear);
                
                algebraic.Add(new AlgebraicSystem(
                    NewtonIteration(block, by.Select(i => Arrow.New(i, i.Evaluate(t, t0)))),
                    by,
                    linear));
            }
            
            // Add solutions for the voltage across all the components.
            components = Circuit.Components.OfType<TwoTerminal>()
                .Select(i => Arrow.New(DependentVariable(i.Name, t), i.V.Evaluate(trivial)))
                .ToList();
            
            LogTime(MessageType.Info, "System solved.");
        }

        private static List<Equal> NewtonIteration(IEnumerable<Expression> f, IEnumerable<Arrow> y)
        {
            return f.NewtonRhapson(y);
            List<Expression> F = f.ToList();

        }

        /// <summary>
        /// Clear all state from the simulation.
        /// </summary>
        public virtual void Reset()
        {
            n = 0;
        }

        // Implementation of process.
        protected abstract void Process(
            long n, double T, int N,
            IEnumerable<KeyValuePair<Expression, double[]>> Input,
            IEnumerable<KeyValuePair<Expression, double[]>> Output,
            IEnumerable<Arrow> Arguments,
            int Iterations);

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
            // Call the implementation of process.
            Process(n, T, N, Input, Output, Arguments, Iterations);

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

        // Logging helpers.
        protected void LogExpressions(string Title, IEnumerable<Expression> Expressions)
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
        protected void LogTime(MessageType Type, string Message, bool Reset)
        {
            if (Reset)
            {
                watch.Reset();
                watch.Start();
            }
            log.WriteLine(Type, "[" + watch.ElapsedMilliseconds + " ms] " + Message);
        }
        protected void LogTime(MessageType Type, string Message) { LogTime(Type, Message, false); }

        // Shorthand for df/dx.
        protected static Expression D(Expression f, Expression x) { return Call.D(f, x); }

        // Check if x is a derivative
        protected static bool IsD(Expression f, Expression x)
        {
            Call C = f as Call;
            if (!ReferenceEquals(C, null))
                return C.Target.Name == "D" && C.Arguments[1].Equals(x);
            return false;
        }

        // Get the expression that x is a derivative of.
        protected static Expression DOf(Expression x)
        {
            Call d = (Call)x;
            if (d.Target.Name == "D")
                return d.Arguments.First();
            throw new InvalidOperationException("Expression is not a derivative");
        }

        // Make a variable Name dependent on On.
        protected static Call DependentVariable(string Name, params Variable[] On) 
        { 
            return Call.New(ExprFunction.New(Name, On), On); 
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
        
        private static T Pop<T>(List<T> From)
        {
            T p = From.Last();
            From.RemoveAt(From.Count - 1);
            return p;
        }

        private static IEqualityComparer<Expression> ExprRefEquality = new ReferenceEqualityComparer<Expression>();
    }
}
