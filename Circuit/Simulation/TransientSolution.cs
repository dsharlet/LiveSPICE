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
            // Define dq = delta function.
            // TODO: This should probably be centered around 0, and also have an integral of 1 (i.e. a height of 1 / h).
            globals.Add(ExprFunction.New("d", Call.If((0 <= t) & (t < h), 1, 0), t));
            // Define uq = step function.
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
                // Use the default parameter values.
                .Substitute(Analysis.Parameters.Select(i => Arrow.New(i.Expression, i.Value)))
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
            Arrow[] defaults = Analysis.Parameters.Select(i => Arrow.New(i.Expression, i.Value)).ToArray();
            foreach (SystemOfEquations F in system.Partition())
            {
                Log.WriteLine(MessageType.Verbose, "Partition unknowns: {0}", String.Join(", ", F.Unknowns));
                // Keep parameter-dependent elimination in the numerical solve.
                bool parameterized = F.Any(i => i.DependsOn(Analysis.Parameters.Select(p => p.Expression)));
                List<Expression> linear = F.Unknowns.Where(j => !parameterized || F.All(i => i[j] is Constant)).ToList();
                F.RowReduce(linear);
                List<Arrow> linearFwd = new List<Arrow>();
                List<Arrow> linearBack = new List<Arrow>();
                F.PartialSolve(linear, linearFwd, linearBack);
                if (linearFwd.Any())
                {
                    solutions.Add(new LinearSolutions(linearFwd));
                    LogExpressions(Log, MessageType.Verbose, "Linear solutions:", linearFwd);
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
                    List<Expression> ly = dy.Where(j => (!parameterized || nonlinear.All(i => i[j] is Constant)) && !nonlinear.Any(i => i[j].DependsOn(NewtonIteration.DeltaOf(j)))).ToList();

                    // Find linear solutions for dy. 
                    nonlinear.RowReduce(ly);
                    IEnumerable<Arrow> solved = nonlinear.Solve(ly);

                    // Deltas x with parameter-only coefficients are solved from equations A·x + R·b == 0, with b the per-sample factors
                    // of the other terms, numerically once per block as x = M·b. M are fresh symbols, so the remaining equations stay
                    // polynomial in them and nothing symbolic divides by a parameter expression.
                    List<Expression> lin = nonlinear.Unknowns.Where(j => parameterized && nonlinear.All(i => !i[j].DependsOn(t))).ToList();
                    ParameterSolutions block = null;
                    if (lin.Any())
                    {
                        List<LinearCombination> eqs = nonlinear.Equations.ToList();
                        List<int> rows = Enumerable.Range(0, eqs.Count).Where(i => lin.Any(j => !eqs[i][j].EqualsZero())).ToList();
                        // Pick the rows to solve with on the default parameter values: the shortest row with a pivot within a factor
                        // 1000 of the largest, because M fills in with the terms of the rows it solves.
                        double[][] A0 = rows.Select(i => lin.Select(j => (double)eqs[i][j].Evaluate(defaults)).ToArray()).ToArray();
                        int[] size = rows.Select(i => Sum.TermsOf(eqs[i].Expand()).Count()).ToArray();
                        List<int> pick = new List<int>();
                        for (int j = 0; j < lin.Count; ++j)
                        {
                            List<int> free = Enumerable.Range(0, rows.Count).Except(pick).ToList();
                            double max = free.Max(i => (double?)Math.Abs(A0[i][j])) ?? 0;
                            if (max == 0)
                                throw new Exception("Parameter-dependent linear system is singular at the default parameter values.");
                            int p = free.Where(i => Math.Abs(A0[i][j]) >= max / 1000).OrderBy(i => size[i]).First();
                            pick.Add(p);
                            foreach (int i in free)
                                for (int k = j + 1; k < lin.Count; ++k)
                                    A0[i][k] -= A0[p][k] * A0[i][j] / A0[p][j];
                        }
                        rows = pick.Select(i => rows[i]).ToList();

                        // The unknowns of the block are the updated values y + dy, so the equations only depend on them through the same A.
                        // The other equations see them through z = B·(y + dy), so they stay as sparse as the per-sample factors b.
                        List<Arrow> zero = lin.SelectMany(j => new[] { Arrow.New(j, 0), Arrow.New(NewtonIteration.DeltaOf(j), 0) }).ToList();
                        List<int> rest = Enumerable.Range(0, eqs.Count).Except(rows).ToList();
                        List<int> proj = rest.Where(i => lin.Any(j => !eqs[i][j].EqualsZero())).ToList();
                        List<Expression> x = lin.Concat(proj.Select(i => Variable.New("z" + solutions.Count + "_" + i))).ToList();
                        List<LinearCombination> A = rows.Select(i => LinearCombination.New(Factors(eqs[i].Substitute(zero)).Concat(lin.Select(j => new KeyValuePair<Expression, Expression>(j, eqs[i][j])))))
                            .Concat(proj.Select((i, r) => LinearCombination.New(lin.Select(j => new KeyValuePair<Expression, Expression>(j, eqs[i][j])).Append(new KeyValuePair<Expression, Expression>(x[lin.Count + r], -1))))).ToList();
                        List<Expression> b = A.SelectMany(i => i.Select(k => k.Key)).Distinct().Except(x).ToList();

                        // M only needs the structural nonzeros of A⁻¹·R, solve for them with random values to avoid accidental cancellation.
                        // Entries at rounding level of the largest are structural cancellations, some rows are only that.
                        Random random = new Random(0);
                        double[][] S = A.Select(i => x.Concat(b).Select(j => i[j].EqualsZero() ? 0 : 1 + random.NextDouble()).ToArray()).ToArray();
                        Simulation.Solve(S, x.Count, x.Count + b.Count);
                        double cutoff = 1e-12 * S.SelectMany(i => i.Skip(x.Count)).DefaultIfEmpty().Max(Math.Abs);
                        List<Arrow> Mb = x.Select((j, r) => Arrow.New(j, LinearCombination.New(b.Where((k, c) => Math.Abs(S[r][x.Count + c]) > cutoff)
                            .Select(k => new KeyValuePair<Expression, Expression>(k, Variable.New("M" + solutions.Count + "_" + r + "_" + b.IndexOf(k))))))).ToList();

                        // The part of M·b that is fixed within a sample is computed once, before the Newton iterations.
                        List<Arrow> w = Mb.Select((i, r) => Arrow.New(Component.DependentVariable("w" + solutions.Count + "_" + r, t), Sum.New(((LinearCombination)i.Right).Where(k => !k.Key.DependsOn(F.Unknowns)).Select(k => Product.New(k.Key, k.Value))))).ToList();
                        List<Expression> xw = Mb.Select((i, r) => Sum.New(w[r].Left, Sum.New(((LinearCombination)i.Right).Where(k => k.Key.DependsOn(F.Unknowns)).Select(k => Product.New(k.Key, k.Value))))).ToList();
                        block = new ParameterSolutions(w, Mb, A);
                        solved = lin.Select((i, r) => Arrow.New(i, xw[r] - NewtonIteration.DeltaOf(i))).Concat(solved).ToList();
                        List<Expression> ny = nonlinear.Unknowns.Except(lin).ToList();
                        nonlinear = new SystemOfEquations(rest.Select(i => LinearCombination.New(ny, eqs[i].Substitute(zero) + (proj.Contains(i) ? xw[lin.Count + proj.IndexOf(i)] : 0))
                            .Select(k => new KeyValuePair<Expression, Expression>(k.Key, Sum.New(Factors(k.Value).Select(j => Product.New(j.Value, j.Key)))))), ny);
                    }

                    // Initial guess for y[t] = y[t - h].
                    IEnumerable<Arrow> guess = F.Unknowns.Select(i => Arrow.New(i, i.Substitute(t, t - h))).ToList();

                    // Newton system equations.
                    IEnumerable<LinearCombination> equations = nonlinear.Equations.Buffer();

                    solutions.Add(new NewtonIteration(solved, equations, nonlinear.Unknowns, guess));
                    if (block != null)
                        solutions.Add(block);
                    LogExpressions(Log, MessageType.Verbose, String.Format("Non-linear Newton's method updates ({0}):", String.Join(", ", nonlinear.Unknowns)), equations.Select(i => Equal.New(i, 0)));
                    LogExpressions(Log, MessageType.Verbose, "Linear Newton's method updates:", solved);
                }

                if (linearBack.Any())
                {
                    solutions.Add(new LinearSolutions(linearBack));
                    LogExpressions(Log, MessageType.Verbose, "Linear solutions:", linearBack);
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
        // Terms of f grouped by their per-sample factor, with parameter-only coefficients.
        private static IEnumerable<KeyValuePair<Expression, Expression>> Factors(Expression f)
        {
            return Sum.TermsOf(f.Expand())
                .GroupBy(j => Product.New(Product.TermsOf(j).Where(k => k.DependsOn(t))), j => Product.New(Product.TermsOf(j).Where(k => !k.DependsOn(t))))
                .Select(j => new KeyValuePair<Expression, Expression>(j.Key, Sum.New(j)));
        }

        public static TransientSolution Solve(Analysis Analysis, Expression TimeStep, ILog Log) { return Solve(Analysis, TimeStep, new Arrow[] { }, Log); }
        public static TransientSolution Solve(Analysis Analysis, Expression TimeStep) { return Solve(Analysis, TimeStep, new Arrow[] { }, new NullLog()); }

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

    /// <summary>
    /// Solutions x = M·b of Equations A·x + R·b == 0, where A and R only depend on parameters and M are symbols the
    /// simulation solves for numerically when the parameters change. The linear solutions are the part of M·b that
    /// is fixed within a sample.
    /// </summary>
    public class ParameterSolutions : LinearSolutions
    {
        public IEnumerable<Arrow> Block { get; }
        public IEnumerable<LinearCombination> Equations { get; }

        public ParameterSolutions(IEnumerable<Arrow> Solutions, IEnumerable<Arrow> Block, IEnumerable<LinearCombination> Equations) : base(Solutions)
        {
            this.Block = Block.ToList();
            this.Equations = Equations.ToList();
        }
    }
}
