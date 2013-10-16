using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Reflection.Emit;
using SyMath;

namespace Circuit
{
    // Block of equations to solve, with some solutions that are linear combinations of the non-linear solutions.
    public class AlgebraicSystem
    {
        private List<Arrow> linear;
        private List<Equal> nonlinear;
        private List<Expression> unknowns;
        private List<Arrow> dependent;

        public IEnumerable<Arrow> Linear { get { return linear; } }
        public IEnumerable<Equal> Nonlinear { get { return nonlinear; } }
        public IEnumerable<Expression> Unknowns { get { return unknowns; } }
        public IEnumerable<Arrow> Dependent { get { return dependent; } }

        public AlgebraicSystem(List<Arrow> Linear, List<Equal> System, List<Expression> Unknowns, List<Arrow> DependentSolutions)
        {
            linear = Linear;
            nonlinear = System;
            unknowns = Unknowns;
            dependent = DependentSolutions;
        }

        public bool DependsOn(Expression x) { return nonlinear.Any(i => i.DependsOn(x)); }
    }

    /// <summary>
    /// The numerical solution for a transient simulation of a circuit.
    /// </summary>
    public class TransientSolution
    {
        // Expression for t at the previous timestep.
        public static readonly Variable t0 = Variable.New("t0");
        // And the current timestep.
        public static readonly Variable t = Component.t;

        private Quantity sampleRate;
        /// <summary>
        /// The sample rate of this solution.
        /// </summary>
        public Quantity SampleRate { get { return sampleRate; } }
        
        private List<Expression> nodes;
        /// <summary>
        /// The nodes this contains a solution for.
        /// </summary>
        public IEnumerable<Expression> Nodes { get { return nodes; } }
                                
        protected List<Arrow> trivial;
        /// <summary>
        /// Trivial solutions.
        /// </summary>
        public IEnumerable<Arrow> Trivial { get { return trivial; } }

        protected List<Arrow> differential;
        /// <summary>
        /// Solutions to the differential varibales in the system.
        /// </summary>
        public IEnumerable<Arrow> Differential { get { return differential; } }

        protected List<AlgebraicSystem> algebraic;
        /// <summary>
        /// Chunks of equations and solutions.
        /// </summary>
        public IEnumerable<AlgebraicSystem> Algebraic { get { return algebraic; } }

        protected List<Arrow> components;
        /// <summary>
        /// Voltage across the two terminal components in the system.
        /// </summary>
        public IEnumerable<Arrow> Components { get { return components; } }

        protected List<Arrow> linearization;
        /// <summary>
        /// Terms used to linearize the differential system.
        /// </summary>
        public IEnumerable<Arrow> Linearization { get { return linearization; } }

        private TransientSolution(
            Quantity SampleRate,
            List<Expression> Nodes,
            List<Arrow> Trivial,
            List<Arrow> Differential,
            List<AlgebraicSystem> Algebraic,
            List<Arrow> Components,
            List<Arrow> Linearization)
        {
            sampleRate = SampleRate;
            nodes = Nodes;
            trivial = Trivial;
            differential = Differential;
            algebraic = Algebraic;
            components = Components;
            linearization = Linearization;
        }

        /// <summary>
        /// Solve the given circuit for transient simulation.
        /// </summary>
        /// <param name="Circuit">Circuit to simulate.</param>
        /// <param name="SampleRate">Sampling period.</param>
        public static TransientSolution SolveCircuit(Circuit Circuit, Quantity SampleRate, ILog Log)
        {
            Stopwatch time = new Stopwatch();
            time.Start();

            // Length of one timestep in the oversampled simulation.
            Expression h = 1 / ((Expression)SampleRate);

            Log.WriteLine(MessageType.Info, "Building simulation for circuit '{0}', f={1}", Circuit.Name, SampleRate.ToString());

            Log.WriteLine(MessageType.Info, "[{0} ms] Performing MNA on circuit...", time.ElapsedMilliseconds);

            // Analyze the circuit to get the MNA system and unknowns.
            List<Expression> y = new List<Expression>();
            List<Equal> mna = new List<Equal>();
            Circuit.Analyze(mna, y);
            LogExpressions(Log, "MNA system of " + mna.Count + " equations and " + y.Count + " unknowns = {{ " + y.UnSplit(", ") + " }}", mna);

            Log.WriteLine(MessageType.Info, "[{0} ms] Solving MNA system...", time.ElapsedMilliseconds);

            // Separate y into differential and algebraic unknowns.
            List<Expression> dy_dt = y.Where(i => mna.Any(j => j.DependsOn(D(i, t)))).Select(i => D(i, t)).ToList();
            y.AddRange(dy_dt);

            // Find trivial solutions for y and substitute them into the system.
            List<Arrow> trivial = mna.Solve(y);
            trivial.RemoveAll(i => i.Right.DependsOn(y));
            mna = mna.Evaluate(trivial).OfType<Equal>().ToList();
            y.RemoveAll(i => trivial.Any(j => j.Left.Equals(i)));
            LogExpressions(Log, "Trivial solutions:", trivial);

            // Linearize the system for solving the differential equations by replacing 
            // non-linear terms with constants (from the previous timestep).
            List<Arrow> linearization = new List<Arrow>();
            List<Equal> linearized = ExtractNonLinear(mna, y, linearization);
            LogExpressions(Log, "Linearized system:", linearized);
            LogExpressions(Log, "Non-linear terms:", linearization);

            // Solve for differential solutions to the linearized system.
            y.RemoveAll(i => IsD(i, t));
            List<Arrow> differential = linearized
                // Solve for the algebraic unknowns in terms of the rest and substitute them.
                .Evaluate(linearized.Solve(y.Where(i => dy_dt.None(j => DOf(j).Equals(i))))).OfType<Equal>()
                // Solve the resulting system of differential equations.
                .NDPartialSolve(dy_dt.Select(i => DOf(i)), t, t0, h, IntegrationMethod.Trapezoid);
            y.RemoveAll(i => differential.Any(j => j.Left.Equals(i)));
            LogExpressions(Log, "Differential solutions:", differential);

            // After solving for the differential unknowns, divide them by h so we don't have 
            // to do it during simulation. It's faster to simulate, and we get the benefits of 
            // arbitrary precision calculations here.
            mna = mna.Evaluate(dy_dt.Select(i => Arrow.New(i, i / h))).Cast<Equal>().ToList();
            linearization = linearization.Evaluate(dy_dt.Select(i => Arrow.New(i, i / h))).Cast<Arrow>().ToList();


            // Solve the algebraic system.
            List<AlgebraicSystem> algebraic = new List<AlgebraicSystem>();

            // Find the minimum set of unknowns requiring non-linear methods.
            List<Expression> unknowns = y.Where(i => linearization.Any(j => j.DependsOn(i))).ToList();
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
                List<Arrow> dependent = linear.Concat(IndependentSolutions(mna.PartialSolve(y), y)).ToList();
                y.RemoveAll(i => dependent.Any(j => j.Left == i));

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

                LogExpressions(Log, "Nonlinear system of " + block.Count + " equations and " + by.Count + " unknowns {{ " + by.UnSplit(", ") + " }}:", block.Select(i => Equal.New(i, 0)));
                LogExpressions(Log, "Dependent solutions:", dependent);
                
                algebraic.Add(new AlgebraicSystem(
                    new List<Arrow>(),
                    NewtonIteration(block, by.Select(i => Arrow.New(i, i.Evaluate(t, t0)))),
                    by,
                    dependent));
            }
            
            // Add solutions for the voltage across all the components.
            List<Arrow> components = Circuit.Components.OfType<TwoTerminal>().Select(
                i => Arrow.New((Expression)DependentVariable(i.Name, t), i.V.Evaluate(trivial))).ToList();

            Log.WriteLine(MessageType.Info, "[{0} ms] System solved!", time.ElapsedMilliseconds);
            
            return new TransientSolution(
                SampleRate,
                Circuit.Nodes.Select(i => (Expression)i.V).ToList(),
                trivial, 
                differential,
                algebraic, 
                components, 
                linearization);
        }


        private static List<Equal> NewtonIteration(IEnumerable<Expression> f, IEnumerable<Arrow> y)
        {
            return f.NewtonRhapson(y);
        }

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
                
        // Logging helpers.
        private static void LogExpressions(ILog Log, string Title, IEnumerable<Expression> Expressions)
        {
            if (Expressions.Any())
            {
                Log.WriteLine(MessageType.Info, Title);
                foreach (Expression i in Expressions)
                    Log.WriteLine(MessageType.Info, "  " + i.ToString());
                Log.WriteLine(MessageType.Info, "");
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
