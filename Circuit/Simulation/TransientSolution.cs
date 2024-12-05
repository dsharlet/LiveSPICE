using ComputerAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;
using Util;

namespace Circuit
{
    /// <summary>
    /// Represents the solutions of a system of equations derived from a Circuit for transient analysis.
    /// </summary>
    public class TransientSolution
    {
        public static readonly Variable t = Component.t;
        public static readonly Expression T = Component.T;

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="TimeStep">Describes the timestep of the solution.</param>
        /// <param name="Solutions">Enumeration of SolutionSets describing the unknowns solved by this solution.</param>
        /// <param name="InitialConditions">Initial conditions for which the solution is valid.</param>
        /// <param name="Parameters">Description of the parameters in the solution.</param>
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
            LogExpressions(Log, MessageType.Verbose, "System of " + mna.Count + " equations and " + y.Count + " unknowns = {{ " + String.Join(", ", y) + " }}", mna);

            // Evaluate for simulation functions.
            // Define T = step size.
            DynamicNamespace globals = new DynamicNamespace();
            globals.Add("T", h);
            // Define d[t] = delta function.
            // TODO: This should probably be centered around 0, and also have an integral of 1 (i.e. a height of 1 / h).
            globals.Add(ExprFunction.New("d", Call.If((0 <= t) & (t < h), 1, 0), t));
            // Define u[t] = step function.
            globals.Add(ExprFunction.New("u", Call.If(t >= 0, 1, 0), t));
            mna = mna.Resolve(Analysis).Resolve(globals).OfType<Equal>().ToList();

            // Find out what variables have differential relationships.
            List<Expression> dy_dt = y.Where(i => mna.Any(j => j.DependsOn(D(i, t)))).Select(i => D(i, t)).ToList();
            Log.WriteLine(MessageType.Verbose, "Differential unknowns: {0}", String.Join(", ", dy_dt));

            // Find steady state solution for initial conditions.
            List<Arrow> initial = InitialConditions.ToList();
            Log.WriteLine(MessageType.Info, "Performing steady state analysis...");
            LogExpressions(Log, MessageType.Verbose, "Initial conditions for solve:", initial);
            LogExpressions(Log, MessageType.Verbose, "Initial conditions from analysis:", Analysis.InitialConditions);

            SystemOfEquations dc = new SystemOfEquations(mna
                // Derivatives, t, and T are zero in the steady state.
                .Substitute(dy_dt.Select(i => Arrow.New(i, 0)).Append(Arrow.New(t, 0), Arrow.New(T, 0), SinglePoleSwitch.IncludeOpen))
                // Use the initial conditions from analysis.
                .Substitute(Analysis.InitialConditions)
                // Evaluate variables at t=0.
                .OfType<Equal>(), y.Select(j => j.Substitute(t, 0)));

            // Solve partitions independently.
            foreach (SystemOfEquations i in dc.Partition())
            {
                LogExpressions(Log, MessageType.Verbose, "Steady state system for partition:", i.Select(j => Equal.New(j, 0)));
                try
                {
                    List<Arrow> part = i.Equations.Select(j => Equal.New(j, 0)).NSolve(i.Unknowns.Select(j => Arrow.New(j, 0)));
                    initial.AddRange(part);
                    LogExpressions(Log, MessageType.Verbose, "Initial conditions:", part); 
                }
                catch (Exception)
                {
                    Log.WriteLine(MessageType.Warning, "Failed to find partition initial conditions, simulation may be unstable.");
                }
            }

            // Transient analysis of the system.
            Log.WriteLine(MessageType.Info, "Performing transient analysis...");

            SystemOfEquations system = new SystemOfEquations(mna.Substitute(SinglePoleSwitch.ExcludeOpen).OfType<Equal>(), dy_dt.Concat(y));

            // Solve the diff eq for dy/dt and integrate the results.
            system.RowReduce(dy_dt);
            system.BackSubstitute(dy_dt);
            LogExpressions(Log, MessageType.Verbose, "Differential equations:", system.Where(i => i.DependsOn(dy_dt)).Select(i => Equal.New(i, 0)));
            IEnumerable<Equal> integrated = system.Solve(dy_dt)
                .NDIntegrate(t, h, IntegrationMethod.BackwardDifferenceFormula2)
                .Select(i => Equal.New(i.Left, i.Right)).Buffer();
            system.AddRange(integrated);
            LogExpressions(Log, MessageType.Verbose, "Integrated solutions:", integrated);

            LogExpressions(Log, MessageType.Verbose, "Discretized system:", system.Select(i => Equal.New(i, 0)));

            if (system.DependsOn(dy_dt))
                throw new Exception("Failed to eliminate differentials from system of equations.");

            // Solving the system...
            List<SolutionSet> solutions = new List<SolutionSet>();

            // Partition the system into independent systems of equations.
            foreach (SystemOfEquations F in system.Partition())
            {
                Log.WriteLine(MessageType.Verbose, "Partition unknowns: {0}", String.Join(", ", F.Unknowns));
                // Find linear solutions for y. Linear systems should be completely solved here.
                F.RowReduce();
                IEnumerable<Arrow> linear = F.Solve();
                if (linear.Any())
                {
                    linear = Factor(linear);
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
                    IEnumerable<Arrow> solved = nonlinear.Solve(ly);
                    solved = Factor(solved);

                    // Initial guess for y[t] = y[t - h].
                    IEnumerable<Arrow> guess = F.Unknowns.Select(i => Arrow.New(i, i.Substitute(t, t - h))).ToList();
                    guess = Factor(guess);

                    // Newton system equations.
                    IEnumerable<LinearCombination> equations = nonlinear.Equations.Buffer();
                    equations = Factor(equations);

                    solutions.Add(new NewtonIteration(solved, equations, nonlinear.Unknowns, guess));
                    LogExpressions(Log, MessageType.Verbose, String.Format("Non-linear Newton's method updates ({0}):", String.Join(", ", nonlinear.Unknowns)), equations.Select(i => Equal.New(i, 0)));
                    LogExpressions(Log, MessageType.Verbose, "Linear Newton's method updates:", solved);
                }
            }

            Log.WriteLine(MessageType.Info, "System solved, {0} solution sets for {1} unknowns.",
                solutions.Count,
                solutions.Sum(i => i.Unknowns.Count()));

            // Solutions from `Solve` might depend on previous solutions, so we need to make sure to emit the solutions in the order that satisifies such dependencies.
            solutions.Reverse();

            return new TransientSolution(
                h,
                solutions,
                initial);
        }
        public static TransientSolution Solve(Analysis Analysis, Expression TimeStep, ILog Log) { return Solve(Analysis, TimeStep, new Arrow[] { }, Log); }
        public static TransientSolution Solve(Analysis Analysis, Expression TimeStep) { return Solve(Analysis, TimeStep, new Arrow[] { }, new NullLog()); }

        private static IEnumerable<Arrow> Factor(IEnumerable<Arrow> x) { return x.Select(i => Arrow.New(i.Left, i.Right.Factor())).Buffer(); }
        private static IEnumerable<LinearCombination> Factor(IEnumerable<LinearCombination> x) { return x.Select(i => LinearCombination.New(i.Select(j => new KeyValuePair<Expression, Expression>(j.Key, j.Value.Factor())))).Buffer(); }

        // Shorthand for df/dx.
        protected static Expression D(Expression f, Expression x) { return Call.D(f, x); }

        // Check if x is a derivative
        protected static bool IsD(Expression f, Expression x)
        {
            if (f is Call C)
                return C.Target.Name == "D" && C.Arguments.ElementAt(1).Equals(x);
            return false;
        }

        // Logging helpers.
        private static void LogList(ILog Log, MessageType Type, string Title, IEnumerable<string> List)
        {
            if (Log is NullLog) return;
            if (List.Any())
            {
                Log.WriteLine(Type, Title);
                Log.WriteLines(Type, List.Select(i => "  " + i));
                Log.WriteLine(Type, "");
            }
        }

        private static void LogExpressions(ILog Log, MessageType Type, string Title, IEnumerable<Expression> Expressions)
        {
            LogList(Log, Type, Title, Expressions.Select(i => i.ToString()));
        }
    }
}
