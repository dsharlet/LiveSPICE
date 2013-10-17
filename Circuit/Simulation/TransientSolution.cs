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
    /// <summary>
    /// The numerical solution for a transient simulation of a circuit.
    /// </summary>
    public class TransientSolution
    {
        // Expression for t at the previous timestep.
        public static readonly Variable t0 = Variable.New("t0");
        // And the current timestep.
        public static readonly Variable t = Component.t;

        private Quantity h;
        /// <summary>
        /// The length of a timestep given by this solution.
        /// </summary>
        public Quantity TimeStep { get { return h; } }
        
        private List<Expression> nodes;
        /// <summary>
        /// The nodes this contains a solution for.
        /// </summary>
        public IEnumerable<Expression> Nodes { get { return nodes; } }

        private List<SolutionSet> solutions;
        /// <summary>
        /// Ordered list of systems that comprise this solution.
        /// </summary>
        public IEnumerable<SolutionSet> Solutions { get { return solutions; } }
        
        private List<Arrow> linearization;
        /// <summary>
        /// Defines any linearization expressions used in the solution.
        /// </summary>
        public IEnumerable<Arrow> Linearization { get { return linearization; } }

        public TransientSolution(
            Quantity TimeStep,
            List<Expression> Nodes,
            List<SolutionSet> Solutions,
            List<Arrow> Linearization)
        {
            h = TimeStep;
            nodes = Nodes;
            solutions = Solutions;
            linearization = Linearization;
        }

        public TransientSolution(
            Quantity TimeStep,
            List<Expression> Nodes,
            List<SolutionSet> Solutions)
        {
            h = TimeStep;
            nodes = Nodes;
            solutions = Solutions;
        }

        /// <summary>
        /// Solve the given circuit for transient simulation.
        /// </summary>
        /// <param name="Circuit">Circuit to simulate.</param>
        /// <param name="SampleRate">Sampling period.</param>
        public static TransientSolution SolveCircuit(Circuit Circuit, Quantity TimeStep, ILog Log)
        {
            Stopwatch time = new Stopwatch();
            time.Start();

            Expression h = TimeStep;

            Log.WriteLine(MessageType.Info, "Building TransientSolution for circuit '{0}', h={1}", Circuit.Name, TimeStep.ToString());

            Log.WriteLine(MessageType.Info, "[{0} ms] Performing MNA on circuit...", time.ElapsedMilliseconds);

            // Analyze the circuit to get the MNA system and unknowns.
            List<Expression> y = new List<Expression>();
            List<Equal> mna = new List<Equal>();
            Circuit.Analyze(mna, y);
            LogExpressions(Log, "MNA system of " + mna.Count + " equations and " + y.Count + " unknowns = {{ " + y.UnSplit(", ") + " }}", mna);

            Log.WriteLine(MessageType.Info, "[{0} ms] Solving MNA system...", time.ElapsedMilliseconds);

            // Separate y into differential and algebraic unknowns.
            List<Expression> dy_dt = y.Where(i => mna.Any(j => j.DependsOn(D(i, t)))).Select(i => D(i, t)).ToList();

            // Separate mna into differential and algebraic equations.
            List<Equal> diffeq = mna.Where(i => i.DependsOn(dy_dt)).ToList();
            mna = mna.Except(diffeq).ToList();
            LogExpressions(Log, "Differential equations:", diffeq);

            // Add the (potentially implicit) numerical integration of the differential equation to the system.
            List<Equal> integrated = diffeq
                .NDIntegrate(dy_dt.Select(i => DOf(i)), t, t0, h, IntegrationMethod.Trapezoid)
                .Select(i => Equal.New(i.Left, i.Right)).ToList();
            mna.AddRange(integrated);
            LogExpressions(Log, "Integrated solutions:", integrated);

            // Add the differential equations back to the system, with solutions now.
            mna.InsertRange(0, diffeq.Evaluate(dy_dt.Select(i => Arrow.New(i, (DOf(i) - DOf(i).Evaluate(t, t0)) / h))).OfType<Equal>());
            LogExpressions(Log, "Discretized system:", mna);

            // Solve the system...
            List<SolutionSet> systems = new List<SolutionSet>();

            // Find trivial solutions for y and substitute them into the system.
            List<Arrow> trivial = mna.Solve(y);
            trivial.RemoveAll(i => i.Right.DependsOn(y));
            mna = mna.Evaluate(trivial).OfType<Equal>().ToList();
            y.RemoveAll(i => trivial.Any(j => j.Left.Equals(i)));
            LogExpressions(Log, "Trivial solutions:", trivial);
            if (trivial.Any())
                systems.Add(new LinearSolutions(trivial));
            
            // Set up J_F*(y - y0) = -F(y0) as a linear system.
            List<Arrow> y0 = y.Select(i => Arrow.New(i, i.Evaluate(t, t0))).ToList();
            List<Expression> F = mna.Select(i => i.Left - i.Right).ToList();

            List<Equal> newton = NewtonIteration(F, y0);
            //y.Reverse();
            List<LinearCombination> S = newton.Select(i => new LinearCombination(y, i.Left - i.Right)).ToList();
            systems.Add(new NewtonRhapsonIteration(null, S, y));

            
            return new TransientSolution(
                h,
                Circuit.Nodes.Select(i => (Expression)i.V).ToList(),
                systems,
                null);
        }

        ///// <summary>
        ///// Solve the given circuit for transient simulation.
        ///// </summary>
        ///// <param name="Circuit">Circuit to simulate.</param>
        ///// <param name="SampleRate">Sampling period.</param>
        //public static TransientSolution SolveCircuit(Circuit Circuit, Quantity TimeStep, ILog Log)
        //{
        //    Stopwatch time = new Stopwatch();
        //    time.Start();

        //    // Length of one timestep in the oversampled simulation.
        //    Expression h = TimeStep;

        //    Log.WriteLine(MessageType.Info, "Building simulation for circuit '{0}', h={1}", Circuit.Name, TimeStep.ToString());

        //    Log.WriteLine(MessageType.Info, "[{0} ms] Performing MNA on circuit...", time.ElapsedMilliseconds);

        //    // Analyze the circuit to get the MNA system and unknowns.
        //    List<Expression> y = new List<Expression>();
        //    List<Equal> mna = new List<Equal>();
        //    Circuit.Analyze(mna, y);
        //    LogExpressions(Log, "MNA system of " + mna.Count + " equations and " + y.Count + " unknowns = {{ " + y.UnSplit(", ") + " }}", mna);

        //    Log.WriteLine(MessageType.Info, "[{0} ms] Solving MNA system...", time.ElapsedMilliseconds);

        //    // Separate y into differential and algebraic unknowns.
        //    List<Expression> dy_dt = y.Where(i => mna.Any(j => j.DependsOn(D(i, t)))).Select(i => D(i, t)).ToList();
        //    y.AddRange(dy_dt);

        //    List<AlgebraicSystem> systems = new List<AlgebraicSystem>();

        //    // Find trivial solutions for y and substitute them into the system.
        //    List<Arrow> trivial = mna.Solve(y);
        //    trivial.RemoveAll(i => i.Right.DependsOn(y));
        //    mna = mna.Evaluate(trivial).OfType<Equal>().ToList();
        //    y.RemoveAll(i => trivial.Any(j => j.Left.Equals(i)));
        //    LogExpressions(Log, "Trivial solutions:", trivial);
        //    if (trivial.Any())
        //        systems.Add(new LinearSystem(trivial));
            
        //    // Linearize the system for solving the differential equations by replacing 
        //    // non-linear terms with constants (from the previous timestep).
        //    List<Arrow> linearization = new List<Arrow>();
        //    List<Equal> linearized = ExtractNonLinear(mna, y, linearization);
        //    LogExpressions(Log, "Linearized system:", linearized);
        //    LogExpressions(Log, "Non-linear terms:", linearization);

        //    // Solve for differential solutions to the linearized system.
        //    y.RemoveAll(i => IsD(i, t));
        //    List<Arrow> differential = linearized
        //        // Solve for the algebraic unknowns in terms of the rest and substitute them.
        //        .Evaluate(linearized.Solve(y.Where(i => dy_dt.None(j => DOf(j).Equals(i))))).OfType<Equal>()
        //        // Solve the resulting system of differential equations.
        //        .NDPartialSolve(dy_dt.Select(i => DOf(i)), t, t0, h, IntegrationMethod.Trapezoid);
        //    y.RemoveAll(i => differential.Any(j => j.Left.Equals(i)));
        //    LogExpressions(Log, "Differential solutions:", differential);
        //    if (differential.Any())
        //        systems.Add(new LinearSystem(differential));

        //    // After solving for the differential unknowns, divide them by h so we don't have 
        //    // to do it during simulation. It's faster to simulate, and we get the benefits of 
        //    // arbitrary precision calculations here.
        //    List<Arrow> dydt = dy_dt.Select(i => Arrow.New(i, (DOf(i) - DOf(i).Evaluate(t, t0)) / h)).ToList();
        //    mna = mna.Evaluate(dydt).Cast<Equal>().ToList();
        //    linearization = linearization.Evaluate(dydt).Cast<Arrow>().ToList();


        //    // Solve the algebraic system.
        //    List<AlgebraicSystem> algebraic = new List<AlgebraicSystem>();

        //    // Find the minimum set of unknowns requiring non-linear methods.
        //    List<Expression> unknowns = y.Where(i => linearization.Any(j => j.DependsOn(i))).ToList();
        //    List<Expression> nonlinear = mna.Evaluate(mna.Solve(y.Except(unknowns))).OfType<Equal>().Select(i => i.Left - i.Right).ToList();
        //    while (y.Any())
        //    {
        //        // Find the smallest independent system we can to numerically solve.
        //        Tuple<List<Expression>, List<Expression>> next = nonlinear.Select(f =>
        //        {
        //            // Starting with f, find the minimal set of equations necessary to solve the system.
        //            // Find the unknowns in this equation.
        //            List<Expression> fy = y.Where(i => f.DependsOn(i)).ToList();
        //            List<Expression> system = new List<Expression>() { f };

        //            // While we have fewer equations than variables...
        //            while (system.Count < fy.Count)
        //            {
        //                // Find the equation that will introduce the fewest variables to the system.
        //                IEnumerable<Expression> ry = y.Except(fy, ExprRefEquality);
        //                List<Expression> candidates = nonlinear.Except(system, ExprRefEquality).ToList();
        //                if (!candidates.Any())
        //                    throw new AlgebraException("Underdeterined MNA system");
        //                Expression add = candidates.ArgMin(i => ry.Count(j => j.DependsOn(i)));
        //                system.Add(add);
        //                fy.AddRange(ry.Where(i => add.DependsOn(i)));
        //            }

        //            return new Tuple<List<Expression>, List<Expression>>(system, fy);
        //        }).ArgMin(i => i.Item1.Count());

        //        // block is a subset of the system that we can solve for by with.
        //        List<Expression> block = next.Item1;
        //        List<Expression> by = next.Item2;

        //        // Remove these from the system.
        //        nonlinear.RemoveAll(i => block.Contains(i));
        //        y.RemoveAll(i => by.Contains(i));

        //        // Try solving+substituting linear solutions first.
        //        List<Arrow> linear = block.Select(i => Equal.New(i, 0)).Solve(by).ToList();
        //        linear.RemoveAll(i => i.Right.DependsOn(by));
        //        by.RemoveAll(i => linear.Any(j => j.Left == i));
        //        block = block.Evaluate(linear).Where(i => i.DependsOn(by)).ToList();
        //        if (linear.Any())
        //            systems.Add(new LinearSystem(linear));
                
        //        systems.Add(new NonLinearSystem(NewtonIteration(block, by.Select(i => Arrow.New(i, i.Evaluate(t, t0)))), by));

        //        // Now that we know solutions to by, we might be able to find other solutions too.
        //        List<Arrow> dependent = linear.Concat(IndependentSolutions(mna.PartialSolve(y), y)).ToList();
        //        y.RemoveAll(i => dependent.Any(j => j.Left == i));

        //        if (dependent.Any())
        //            systems.Add(new LinearSystem(dependent));

        //        //for (int r = 1; r <= y.Count; ++r)
        //        //{
        //        //    foreach (IEnumerable<Expression> yi in y.Combinations(r))
        //        //    {
        //        //        List<Arrow> s = IndependentSolutions(mna.PartialSolve(yi), y).ToList();
        //        //        if (!s.Empty())
        //        //        {
        //        //            linear.AddRange(s);
        //        //            y.RemoveAll(i => s.Any(j => j.Left == i));
        //        //            r = 0;
        //        //            break;
        //        //        }
        //        //    }
        //        //}

        //        LogExpressions(Log, "Nonlinear system of " + block.Count + " equations and " + by.Count + " unknowns {{ " + by.UnSplit(", ") + " }}:", block.Select(i => Equal.New(i, 0)));
        //        LogExpressions(Log, "Dependent solutions:", dependent);
        //    }

        //    // Add solutions for the voltage across all the components.
        //    List<Arrow> components = Circuit.Components.OfType<TwoTerminal>().Select(
        //        i => Arrow.New((Expression)DependentVariable(i.Name, t), i.V.Evaluate(trivial))).ToList();

        //    Log.WriteLine(MessageType.Info, "[{0} ms] System solved!", time.ElapsedMilliseconds);

        //    return new TransientSolution(
        //        TimeStep,
        //        Circuit.Nodes.Select(i => (Expression)i.V).ToList(),
        //        systems,
        //        components,
        //        linearization);
        //}

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

        private static IEqualityComparer<Equal> EqRefEquality = new ReferenceEqualityComparer<Equal>();
        private static IEqualityComparer<Expression> ExprRefEquality = new ReferenceEqualityComparer<Expression>();
    }
}
