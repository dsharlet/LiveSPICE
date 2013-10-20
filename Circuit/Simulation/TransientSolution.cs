using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Reflection;
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
        
        private List<Parameter> parameters = new List<Parameter>();
        /// <summary>
        /// Enumerate the parameters found in this circuit.
        /// </summary>
        public IEnumerable<Parameter> Parameters { get { return parameters; } }
        
        public TransientSolution(
            Quantity TimeStep,
            IEnumerable<Expression> Nodes,
            IEnumerable<SolutionSet> Solutions,
            IEnumerable<Parameter> Parameters)
        {
            h = TimeStep;
            nodes = Nodes.ToList();
            solutions = Solutions.ToList();
            parameters = Parameters.ToList();
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
            LogExpressions(Log, "System of " + mna.Count + " equations and " + y.Count + " unknowns = {{ " + y.UnSplit(", ") + " }}", mna);
            
            // Find and replace the parameters of the simulation.
            List<Parameter> parameters = new List<Parameter>();
            mna = FindParameters(mna, parameters);
            Log.WriteLine(MessageType.Info, "Found " + parameters.Count + " simulation parameters = {{" + parameters.UnSplit(", ") + "}}");

            // Solve the MNA system.
            Log.WriteLine(MessageType.Info, "[{0} ms] Solving system...", time.ElapsedMilliseconds);

            // Separate y into differential and algebraic unknowns.
            List<Expression> dy_dt = y.Where(i => mna.Any(j => j.DependsOn(D(i, t)))).Select(i => D(i, t)).ToList();

            // Separate mna into differential and algebraic equations.
            List<LinearCombination> diffeq = mna.Where(i => i.DependsOn(dy_dt)).InTermsOf(dy_dt).ToList();
            mna = mna.Where(i => !i.DependsOn(dy_dt)).ToList();
            LogList(Log, "Differential equations:", diffeq.Select(i => i.ToString()));

            // Solve the diff eq for dy/dt and integrate the results.
            diffeq.RowReduce(dy_dt);
            diffeq.BackSubstitute(dy_dt);
            List<Equal> integrated = SolveAndRemove(diffeq, dy_dt)
                .NDIntegrate(t, t0, h, IntegrationMethod.Trapezoid)
                .Select(i => Equal.New(i.Left, i.Right)).ToList();
            mna.AddRange(integrated);
            LogExpressions(Log, "Integrated solutions:", integrated);

            // The remaining diffeqs should be algebraic.
            mna.AddRange(diffeq.Select(i => Equal.New(i.ToExpression(), Constant.Zero)));
            LogExpressions(Log, "Discretized system:", mna);

            // Solving the system...
            List<SolutionSet> solutions = new List<SolutionSet>();

            // Find linear solutions for y and substitute them into the system. Linear circuits should be completely solved here.
            List<Arrow> linear = mna.Solve(y);
            linear.RemoveAll(i => i.Right.DependsOn(y));
            mna = mna.Evaluate(linear).OfType<Equal>().ToList();
            y.RemoveAll(i => linear.Any(j => j.Left.Equals(i)));
            if (linear.Any())
            {
                solutions.Add(new LinearSolutions(linear));
                LogExpressions(Log, "Linear solutions:", linear);
            }

            // If there are any variables left, there are some non-linear equations requiring numerical techniques to solve.
            if (y.Any())
            {
                // The variables of this system are the newton iteration updates.
                List<Expression> dy = y.Select(i => NewtonIteration.Delta(i)).ToList();

                // Rearrange the MNA system to be F[y] == 0.
                List<Expression> F = mna.Select(i => i.Left - i.Right).ToList();
                // Compute JxF*dy + F(y0) == 0.
                List<LinearCombination> J = F.Jacobian(y.Select(i => Arrow.New(i, NewtonIteration.Delta(i))));
                foreach (LinearCombination i in J)
                    i[Constant.One] = ((Expression)i.Tag);

                // ly is the subset of y that can be found linearly.
                List<Expression> ly = dy.Where(j => !J.Any(i => i[j].DependsOn(NewtonIteration.DeltaOf(j)))).ToList();
                // Swap the columns such that ly appear first.
                dy = dy.Except(ly).ToList();
                foreach (LinearCombination i in J)
                    i.SwapColumns(ly.Concat(dy));

                // Compute the row-echelon form of the linear part of the Jacobian.
                J.RowReduce(ly);
                // Solutions for each linear update equation.
                List<Arrow> solved = SolveAndRemove(J, ly);

                solutions.Add(new NewtonIteration(solved, J, dy));
                LogExpressions(Log, "Non-linear Newton's method updates:", J.Select(i => Equal.New(i.ToExpression(), Constant.Zero)));
                LogExpressions(Log, "Linear Newton's method updates:", solved);
            }

            Log.WriteLine(MessageType.Info, "[{0} ms] System solved, {1} solution sets for {2} unknowns", 
                time.ElapsedMilliseconds, 
                solutions.Count, 
                solutions.Sum(i => i.Unknowns.Count()));

            return new TransientSolution(
                h,
                Circuit.Nodes.Select(i => (Expression)i.V),
                solutions,
                parameters);
        }

        // Solve S for x, removing the rows of S that are used for a solution.
        private static List<Arrow> SolveAndRemove(IList<LinearCombination> S, IEnumerable<Expression> x)
        {
            // Solve for the variables in x.
            List<Arrow> result = new List<Arrow>();
            foreach (Expression j in x.Reverse())
            {
                // Find the row with the pivot variable in this position.
                LinearCombination i = S.FindPivot(j);

                // If there is no pivot in this position, find any row with a non-zero coefficient of j.
                if (i == null)
                    i = S.FirstOrDefault(s => !s[j].IsZero());

                // Solve the row for i.
                if (i != null)
                {
                    result.Add(Arrow.New(j, i.Solve(j)));
                    S.Remove(i);
                }
            }
            return result;
        }

        // Finds and replaces the parameter expressions in Mna with their variables.
        private static readonly Variable MatchName = Variable.New("name");
        private static readonly Variable MatchDefault = Variable.New("def");
        private static readonly Variable MatchLog = Variable.New("log");
        private static readonly IEnumerable<Expression> MatchP = new Expression[] { "P[name]", "P[name, def]", "P[name, def, log]" };
        private static List<Equal> FindParameters(List<Equal> Mna, List<Parameter> Parameters)
        {
            Dictionary<Expression, Expression> substitutions = new Dictionary<Expression, Expression>();
            foreach (MatchContext match in Mna.SelectMany(i => i.FindMatches(MatchP)))
            {
                // Build the parameter description.
                Expression param = match[MatchName];
                double def = 1.0;
                bool log = false;
                if (match.ContainsKey(MatchDefault))
                    def = (double)match[MatchDefault];
                if (match.ContainsKey(MatchLog))
                    log = match[MatchLog].IsTrue();
                Parameters.Add(new RangeParameter(param.ToString(), def, log));

                // Replace the parameter description with a variable.
                substitutions[match.Matched] = param;
            }

            return Mna.Evaluate(substitutions).Cast<Equal>().ToList();
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

        // Logging helpers.
        private static void LogList(ILog Log, string Title, IEnumerable<string> List)
        {
            if (List.Any())
            {
                Log.WriteLine(MessageType.Info, Title);
                foreach (string i in List)
                    Log.WriteLine(MessageType.Info, "  " + i);
                Log.WriteLine(MessageType.Info, "");
            }
        }

        private static void LogExpressions(ILog Log, string Title, IEnumerable<Expression> Expressions)
        {
            LogList(Log, Title, Expressions.Select(i => i.ToString()));
        }
    }
}
