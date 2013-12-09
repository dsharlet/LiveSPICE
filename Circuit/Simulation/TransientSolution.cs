using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Reflection;
using ComputerAlgebra;
using Util;

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

        public static readonly Variable T = Component.T;

        private Expression h;
        /// <summary>
        /// The length of a timestep given by this solution.
        /// </summary>
        public Expression TimeStep { get { return h; } }

        private IEnumerable<SolutionSet> solutions;
        /// <summary>
        /// Ordered list of SolutionSet objects that describe the overall solution. If SolutionSet
        /// a follows SolutionSet b in this enumeration, b's solution may depend on a's solutions.
        /// </summary>
        public IEnumerable<SolutionSet> Solutions { get { return solutions; } }

        private IEnumerable<Arrow> initialConditions;
        /// <summary>
        /// Set of expressions describing the initial conditions of the variables in this solution.
        /// </summary>
        public IEnumerable<Arrow> InitialConditions { get { return initialConditions; } }

        public TransientSolution(
            Expression TimeStep,
            IEnumerable<SolutionSet> Solutions,
            IEnumerable<Arrow> InitialConditions)
        {
            h = TimeStep;
            solutions = Solutions.Buffer();
            initialConditions = InitialConditions.Buffer();
        }

        /// <summary>
        /// Check if any of the SolutionSets in this solution depend on x.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public bool DependsOn(Expression x) { return solutions.Any(i => i.DependsOn(x)); }

        /// <summary>
        /// Solve the circuit for transient simulation.
        /// </summary>
        /// <param name="Analysis">Analysis from the circuit to solve.</param>
        /// <param name="TimeStep">Discretization timestep.</param>
        /// <param name="Log">Where to send output.</param>
        /// <returns>TransientSolution describing the solution of the circuit.</returns>
        public static TransientSolution Solve(Analysis Analysis, Expression TimeStep, IEnumerable<Arrow> InitialConditions, ILog Log)
        {
            Expression h = TimeStep;

            Log.WriteLine(MessageType.Info, "Building solution for h={0}", TimeStep.ToString());

            // Analyze the circuit to get the MNA system and unknowns.
            List<Equal> mna = Analysis.Equations.ToList();
            List<Expression> y = Analysis.Unknowns.ToList();
            LogExpressions(Log, MessageType.Verbose, "System of " + mna.Count + " equations and " + y.Count + " unknowns = {{ " + y.UnSplit(", ") + " }}", mna);

            // Evaluate for simulation functions.
            // Define T = step size.
            Analysis.Add("T", h);
            // Define d[t] = delta function.
            Analysis.Add(ExprFunction.New("d", Call.If((0 <= t) & (t < h), 1, 0), t));
            // Define u[t] = step function.
            Analysis.Add(ExprFunction.New("u", Call.If(t >= 0, 1, 0), t));
            mna = mna.Resolve(Analysis).OfType<Equal>().ToList();
            
            // Find out what variables have differential relationships.
            List<Expression> dy_dt = y.Where(i => mna.Any(j => j.DependsOn(D(i, t)))).Select(i => D(i, t)).ToList();

            // Find steady state solution for initial conditions.
            List<Arrow> initial = InitialConditions.ToList();
            try
            {
                Log.WriteLine(MessageType.Info, "Performing steady state analysis...");

                List<Arrow> arguments = Analysis.Parameters.Select(i => Arrow.New(i.Expression, i.Default)).ToList();

                List<Equal> dc = mna
                    // Substitute default arguments for the parameters.
                    .Evaluate(arguments)
                    // Derivatives, t, and t0 are zero in the steady state.
                    .Evaluate(dy_dt.Select(i => Arrow.New(i, 0)).Append(Arrow.New(t, 0), Arrow.New(t0, 0)))
                    // Use the initial conditions from analysis.
                    .Evaluate(Analysis.InitialConditions)
                    .OfType<Equal>().ToList();
                initial = dc.NSolve(y.Select(i => Arrow.New(i.Evaluate(t, 0), 0)));
                LogExpressions(Log, MessageType.Verbose, "Initial conditions:", initial);
            }
            catch (Exception)
            {
                Log.WriteLine(MessageType.Warning, "Failed to find steady state for initial conditions, circuit may be unstable.");
            }
            
            // Transient analysis of the system.
            Log.WriteLine(MessageType.Info, "Performing transient analysis...");

            SystemOfEquations system = new SystemOfEquations(mna, dy_dt.Concat(y));

            // Solve the diff eq for dy/dt and integrate the results.
            system.RowReduce(dy_dt);
            system.BackSubstitute(dy_dt);
            IEnumerable<Equal> integrated = system.Solve(dy_dt)
                .NDIntegrate(t, t0, h, IntegrationMethod.Trapezoid)
                .Select(i => Equal.New(i.Left, i.Right)).Buffer();
            system.AddRange(integrated);
            LogExpressions(Log, MessageType.Verbose, "Integrated solutions:", integrated);

            LogExpressions(Log, MessageType.Verbose, "Discretized system:", system.Select(i => Equal.New(i, 0)));

            // Solving the system...
            List<SolutionSet> solutions = new List<SolutionSet>();

            // Partition the system into independent systems of equations.
            foreach (SystemOfEquations F in system.Partition())
            {
                // Find linear solutions for y. Linear systems should be completely solved here.
                F.RowReduce();
                List<Arrow> linear = F.Solve();
                if (linear.Any())
                {
                    // Factor for more efficient arithmetic.
                    linear = linear.Select(i => Arrow.New(i.Left, i.Right.Factor())).ToList();

                    solutions.Add(new LinearSolutions(linear));
                    LogExpressions(Log, MessageType.Verbose, "Linear solutions:", linear);
                }


                // If there are any variables left, there are some non-linear equations requiring numerical techniques to solve.
                if (F.Unknowns.Any())
                {
                    // The variables of this system are the newton iteration updates.
                    List<Expression> dy = F.Unknowns.Select(i => NewtonIteration.Delta(i)).ToList();
                    
                    // Compute JxF*dy + F(y0) == 0.
                    SystemOfEquations nonlinear = new SystemOfEquations(
                        F.Select(i => i.Gradient(F.Unknowns).Select(j => new KeyValuePair<Expression, Expression>(NewtonIteration.Delta(j.Key), j.Value))
                            .Append(new KeyValuePair<Expression, Expression>(1, i))),
                        dy);

                    // ly is the subset of y that can be found linearly.
                    List<Expression> ly = dy.Where(j => !nonlinear.Any(i => i[j].DependsOn(NewtonIteration.DeltaOf(j)))).ToList();

                    // Find linear solutions for dy. 
                    nonlinear.RowReduce(ly);
                    List<Arrow> solved = nonlinear.Solve(ly);

                    // Initial guess for y(t) = y(t0).
                    List<Arrow> guess = F.Unknowns.Select(i => Arrow.New(i, i.Evaluate(t, t0))).ToList();

                    // Factor for more efficient arithmetic.
                    solved = solved.Select(i => Arrow.New(i.Left, i.Right.Factor())).ToList();
                    guess = guess.Select(i => Arrow.New(i.Left, i.Right.Factor())).ToList();
                    IEnumerable<LinearCombination> equations = nonlinear.Select(i => LinearCombination.New(i.Select(j => new KeyValuePair<Expression, Expression>(j.Key, j.Value.Factor())))).ToList();

                    solutions.Add(new NewtonIteration(solved, equations, nonlinear.Unknowns, guess));
                    LogList(Log, MessageType.Verbose, String.Format("Non-linear Newton's method updates ({0}):", nonlinear.Unknowns.UnSplit(", ")), equations.Select(i => i.ToString() + " == 0"));
                    LogExpressions(Log, MessageType.Verbose, "Linear Newton's method updates:", solved);
                }
            }

            Log.WriteLine(MessageType.Info, "System solved, {0} solution sets for {1} unknowns.",
                solutions.Count,
                solutions.Sum(i => i.Unknowns.Count()));
            
            return new TransientSolution(
                h,
                solutions,
                initial);
        }
        public static TransientSolution Solve(Analysis Analysis, Expression TimeStep, ILog Log)
        {
            return Solve(Analysis, TimeStep, new Arrow[] { }, Log);
        }

        // Shorthand for df/dx.
        protected static Expression D(Expression f, Expression x) { return Call.D(f, x); }

        // Check if x is a derivative
        protected static bool IsD(Expression f, Expression x)
        {
            Call C = f as Call;
            if (!ReferenceEquals(C, null))
                return C.Target.Name == "D" && C.Arguments.ElementAt(1).Equals(x);
            return false;
        }

        // Logging helpers.
        private static void LogList(ILog Log, MessageType Type, string Title, IEnumerable<string> List)
        {
            if (List.Any())
            {
                Log.WriteLine(Type, Title);
                foreach (string i in List)
                    Log.WriteLine(Type, "  " + i);
                Log.WriteLine(Type, "");
            }
        }

        private static void LogExpressions(ILog Log, MessageType Type, string Title, IEnumerable<Expression> Expressions)
        {
            LogList(Log, Type, Title, Expressions.Select(i => i.ToString()));
        }
    }
}
