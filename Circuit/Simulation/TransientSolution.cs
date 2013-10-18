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
    /// Represents the solutions of a system of equations derived from a Circuit for transient analysis.
    /// </summary>
    public class TransientSolution
    {
        // Expression for t at the previous timestep.
        public static readonly Variable t0 = Component.t0;
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
        /// Ordered list of SolutionSet objects that describe the overall solution. If SolutionSet
        /// a follows SolutionSet b in this enumeration, b's solution may depend on a's solutions.
        /// </summary>
        public IEnumerable<SolutionSet> Solutions { get { return solutions; } }
        
        private List<Arrow> linearizations;
        /// <summary>
        /// Defines any linearization expressions used in the solution.
        /// </summary>
        public IEnumerable<Arrow> Linearizations { get { return linearizations; } }

        public TransientSolution(
            Quantity TimeStep,
            List<Expression> Nodes,
            List<SolutionSet> Solutions,
            List<Arrow> Linearizations)
        {
            h = TimeStep;
            nodes = Nodes;
            solutions = Solutions;
            linearizations = Linearizations;
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

            // Solving the system...
            List<SolutionSet> systems = new List<SolutionSet>();

            // Find linear solutions for y and substitute them into the system. Linear circuits should be completely solved here.
            List<Arrow> linear = mna.Solve(y);
            linear.RemoveAll(i => i.Right.DependsOn(y));
            mna = mna.Evaluate(linear).OfType<Equal>().ToList();
            y.RemoveAll(i => linear.Any(j => j.Left.Equals(i)));
            if (linear.Any())
                systems.Add(new LinearSolutions(linear));
            LogExpressions(Log, "Linear solutions:", linear);
            
            // Rearrange the MNA system to be F[y] == 0.
            List<Expression> F = mna.Select(i => i.Left - i.Right).ToList();
            // Compute JxF.
            List<LinearCombination> J = F.Jacobian(y);

            //// Goal is to permute (J, y) such that we can find the row-echelon form for all the linear equations in the system.
            //// To do this, we need to move all of the terms of y that have a non-constant entry in the Jacobian to the end
            //// of the list. These terms need to be solved via newton's method.
            //List<Expression> ly = y.Where(j => !J.Any(i => i[j].DependsOn(j))).ToList();
            //// Compute the row-echelon form of the linear part of the Jacobian.
            //J.ToRowEchelon(ly);
            //// Solutions for each linear term.
            //List<Arrow> solved = J.Solve(ly);
            //// The linear terms are no longer unknowns to be solved for.
            //y = y.Except(ly).ToList();
            List<Arrow> solved = new List<Arrow>();

            // Initial guesses of y[t] = y[t0].
            List<Arrow> y0 = y.Select(i => Arrow.New(i, i.Evaluate(t, t0))).ToList();
            // Now get the rows of the Jacobian that we couldn't find a linear solution for.
            J = J.Where(i => y.Contains(i.PivotVariable)).ToList();
            List<LinearCombination> newton = new List<LinearCombination>();

            for (int i = 0; i < J.Count; ++i)
            {
                // Compute J * (x - x0)
                Expression Jx = Add.New(y0.Select(j => J[i][j.Left].Evaluate(y0) * (j.Left - j.Right)));
                // Solve for x.
                newton.Add(new LinearCombination(y, Jx + ((Expression)J[i].Tag).Evaluate(y0)));
            }
                        
            LogExpressions(Log, "Newton iteration:", newton.Select(i => i.ToExpression()));
            systems.Add(new NewtonRhapsonIteration(solved, newton, y));
                        
            return new TransientSolution(
                h,
                Circuit.Nodes.Select(i => (Expression)i.V).ToList(),
                systems,
                null);
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
