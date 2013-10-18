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
            LogExpressions(Log, "Newton iteration:", S.Select(i => i.ToExpression()));
            systems.Add(new NewtonRhapsonIteration(null, S, y));

            //systems.Add(new LinearSolutions(newton.Solve(y)));

            
            return new TransientSolution(
                h,
                Circuit.Nodes.Select(i => (Expression)i.V).ToList(),
                systems,
                null);
        }
        
        private static List<Equal> NewtonIteration(IEnumerable<Expression> f, IEnumerable<Arrow> y)
        {
            return f.NewtonRhapson(y);
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
    }
}
