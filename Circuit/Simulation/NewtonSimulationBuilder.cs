using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using ComputerAlgebra;
using ComputerAlgebra.Extensions;
using ComputerAlgebra.LinqCompiler;
using Util;
using Util.Cancellation;
using LinqExpr = System.Linq.Expressions.Expression;
using ParamExpr = System.Linq.Expressions.ParameterExpression;

namespace Circuit
{

    public record NewtonSimulationSettings(
        int SampleRate,
        int Oversample,
        int Iterations,
        bool Optimize) : SimulationSettings(SampleRate, Oversample);

    public sealed class NewtonSimulationBuilder : ISimulationBuilder<NewtonSimulationSettings>
    {
        protected static readonly Variable t = TransientSolution.t;

        // Largest delay we expect to see. BDF6 is the largest possible
        // realistic (or theoretically possible?) method.
        private const int MaxDelay = -6;

        private readonly ILog _log;

        public NewtonSimulationBuilder(ILog log)
        {
            _log = log;
        }

        public Simulation Build(Analysis mna,
                                NewtonSimulationSettings settings,
                                IEnumerable<Expression> inputs,
                                IEnumerable<Expression> outputs,
                                ICancellationStrategy cancellationStrategy)
        {
            var solution = TransientSolution.Solve(mna, settings.TimeStep / settings.Oversample, _log);

            cancellationStrategy.ThrowIfCancelled();

            var (process, state) = DefineProcess(solution, settings, inputs.ToArray(), outputs.ToArray(), cancellationStrategy);

            return new Simulation(process, state, settings, solution.Parameters);
        }


        // The resulting lambda processes N samples, using buffers provided for Input and Output:
        //  void Process(int N, double t0, double T, double[] Input0 ..., double[] Output0 ...)
        //  { ... }
        private (Action<int, double, double[][], double[][], double[], double[]>, IReadOnlyDictionary<Expression, double>) DefineProcess(
            TransientSolution solution,
            NewtonSimulationSettings settings,
            Expression[] input,
            Expression[] output,
            ICancellationStrategy cancellationStrategy)
        {
            cancellationStrategy.ThrowIfCancelled();

            // Map expressions to identifiers in the syntax tree.
            var inputs = new List<KeyValuePair<Expression, LinqExpr>>();
            var outputs = new List<KeyValuePair<Expression, LinqExpr>>();

            // Lambda code generator.
            CodeGen code = new CodeGen();

            // Create parameters for the basic simulation info (N, t, Iterations).
            ParamExpr sampleCount = code.Decl<int>(Scope.Parameter, "sampleCount");
            ParamExpr t = code.Decl(Scope.Parameter, NewtonSimulationBuilder.t);
            var ins = code.Decl<double[][]>(Scope.Parameter, "ins");
            var outs = code.Decl<double[][]>(Scope.Parameter, "outs");
            var parameters = code.Decl<double[]>(Scope.Parameter, "params");
            var state = code.Decl<double[]>(Scope.Parameter, "state");

            // Create buffer parameters for each input...
            for (int i = 0; i < input.Length; i++)
            {
                inputs.Add(new KeyValuePair<Expression, LinqExpr>(input[i], LinqExpr.ArrayAccess(ins, LinqExpr.Constant(i))));
            }

            // ... and output.
            for (int i = 0; i < output.Length; i++)
            {
                outputs.Add(new KeyValuePair<Expression, LinqExpr>(output[i], LinqExpr.ArrayAccess(outs, LinqExpr.Constant(i))));
            }

            Arrow t_t1 = Arrow.New(NewtonSimulationBuilder.t, NewtonSimulationBuilder.t - solution.TimeStep);
            Arrow t1_t = Arrow.New(NewtonSimulationBuilder.t - solution.TimeStep, NewtonSimulationBuilder.t);

            // Create globals to store previous values of inputs.
            var globals = new Dictionary<Expression, double>();

            void AddGlobal(Expression Name) => globals.TryAdd(Name, 0d);

            foreach (Expression i in input.Distinct())
                AddGlobal(i.Evaluate(t_t1));

            //If any system depends on the previous value of an unknown, we need a global variable for it.

            for (int i = -1; i >= MaxDelay; i--)
            {
                Arrow t_tn = Arrow.New(NewtonSimulationBuilder.t, NewtonSimulationBuilder.t + i * solution.TimeStep);
                IEnumerable<Expression> unknowns_tn = solution.Solutions
                    .SelectMany(solutionSet => solutionSet.Unknowns)
                    //.Where(unknown => output.Any(o => o.DependsOn(unknown)))
                    .Select(expression => expression.Evaluate(t_tn));

                // Do something smarter here - like returning a list of variables that a solution depends on, as this is very slow
                foreach (var unknown in unknowns_tn.Where(u => solution.Solutions.Any(solutionSet => solutionSet.DependsOn(u))))
                {
                    AddGlobal(unknown);
                }
            }

            //for (int i = -1; i >= MaxDelay; i--)
            //{
            //    Arrow t_tn = Arrow.New(NewtonSimulationBuilder.t, NewtonSimulationBuilder.t + i * solution.TimeStep);
            //    IEnumerable<Expression> unknowns_tn = solution.Solutions
            //        .SelectMany(solutionSet => solutionSet.Unknowns)
            //        .Select(expression => expression.Evaluate(t_tn));
            //    if (!solution.Solutions.Any(solutionSet => solutionSet.DependsOn(unknowns_tn)))
            //        break;

            //    foreach (Expression expression in solution.Solutions.SelectMany(solutionSet => solutionSet.Unknowns))
            //        AddGlobal(expression.Evaluate(t_tn));
            //}

            // Also need globals for any Newton's method unknowns.
            foreach (Expression i in solution.Solutions.OfType<NewtonIteration>().SelectMany(i => i.Unknowns))
                AddGlobal(i.Evaluate(t_t1));

            // Set the global values to the initial conditions of the solution.
            foreach (var i in globals)
            {
                // Dumb hack to get f[t - x] -> f[0] for any x.
                Expression i_t0 = i.Key.Evaluate(NewtonSimulationBuilder.t, Real.Infinity).Substitute(Real.Infinity, 0);
                Expression init = i_t0.Evaluate(solution.InitialConditions);
                globals[i.Key] = init is Constant ? (double)init : 0.0;
            }

            // Define lambda body.

            // int Zero = 0
            LinqExpr Zero = LinqExpr.Constant(0);

            // double h = T / Oversample
            LinqExpr h = LinqExpr.Constant((double)settings.TimeStep / settings.Oversample);

            // double invOversample = 1 / Oversample
            LinqExpr invOversample = LinqExpr.Constant(1.0 / settings.Oversample);

            foreach (var (param, index) in solution.Parameters.WithIndex())
            {
                code.Map(Scope.Intermediate, param.Expression, LinqExpr.ArrayAccess(parameters, LinqExpr.Constant(index)));
            }

            // Load the globals to local variables and add them to the map.
            foreach (var (i, idx) in globals.WithIndex())
                code.DeclInit(i.Key, LinqExpr.ArrayAccess(state, LinqExpr.Constant(idx)));

            foreach (KeyValuePair<Expression, LinqExpr> i in inputs)
                code.DeclInit(i.Key, code[i.Key.Evaluate(t_t1)]);

            // Create arrays for linear systems.
            int M = solution.Solutions.OfType<NewtonIteration>().Max(i => i.Equations.Count(), 0);
            int N = solution.Solutions.OfType<NewtonIteration>().Max(i => i.UnknownDeltas.Count(), 0) + 1;
            // If there is an underdetermined system of equations, avoid out of bounds reads.
            M = Math.Max(M, N);
            // Add a column for the solution vector.
            ++N;
            _log.WriteLine(MessageType.Verbose, Vector.IsHardwareAccelerated ? "Vector hardware acceleration enabled" : "No vector hardware acceleration");

            LinqExpr JxF = code.DeclInit<double[][]>("JxF", LinqExpr.NewArrayBounds(typeof(double[]), LinqExpr.Constant(M)));
            for (int j = 0; j < M; ++j)
                code.Add(LinqExpr.Assign(LinqExpr.ArrayAccess(JxF, LinqExpr.Constant(j)), LinqExpr.NewArrayBounds(typeof(double), Vector.IsHardwareAccelerated ? LinqExpr.Constant(N + Vector<double>.Count - 1) : LinqExpr.Constant(N))));

            // for (int n = 0; n < SampleCount; ++n)
            ParamExpr n = code.Decl<int>("n");
            code.For(
                () => code.Add(LinqExpr.Assign(n, Zero)),
                LinqExpr.LessThan(n, sampleCount),
                () => code.Add(LinqExpr.PreIncrementAssign(n)),
                () =>
                {
                    // Prepare input samples for oversampling interpolation.
                    Dictionary<Expression, LinqExpr> dVi = new Dictionary<Expression, LinqExpr>();
                    foreach (Expression i in input.Distinct())
                    {
                        LinqExpr Va = code[i];
                        // Sum all inputs with this key.
                        IEnumerable<LinqExpr> Vbs = inputs.Where(j => j.Key.Equals(i)).Select(j => j.Value);
                        LinqExpr Vb = LinqExpr.ArrayAccess(Vbs.First(), n);
                        foreach (LinqExpr j in Vbs.Skip(1))
                            Vb = LinqExpr.Add(Vb, LinqExpr.ArrayAccess(j, n));

                        // dVi = (Vb - Va) / Oversample
                        code.Add(LinqExpr.Assign(
                            Decl<double>(code, dVi, i, "d" + i.ToString().Replace("[t]", "")),
                            LinqExpr.Multiply(LinqExpr.Subtract(Vb, Va), invOversample)));
                    }

                    // Prepare output sample accumulators for low pass filtering.
                    Dictionary<Expression, LinqExpr> Vo = new Dictionary<Expression, LinqExpr>();
                    foreach (Expression i in output.Distinct())
                        code.Add(LinqExpr.Assign(
                            Decl<double>(code, Vo, i, i.ToString().Replace("[t]", "")),
                            LinqExpr.Constant(0.0)));

                    // int ov = Oversample; 
                    // do { -- ov; } while(ov > 0)
                    ParamExpr ov = code.Decl<int>("ov");
                    code.Add(LinqExpr.Assign(ov, LinqExpr.Constant(settings.Oversample)));
                    code.DoWhile(() =>
                    {
                        // t += h
                        code.Add(LinqExpr.AddAssign(t, h));

                        // Interpolate the input samples.
                        foreach (Expression i in input.Distinct())
                            code.Add(LinqExpr.AddAssign(code[i], dVi[i]));

                        // Compile all of the SolutionSets in the solution.
                        foreach (SolutionSet ss in solution.Solutions)
                        {
                            cancellationStrategy.ThrowIfCancelled();

                            if (ss is LinearSolutions)
                            {
                                code.Add(LinqExpr.Label(LinqExpr.Label("linear_solutions")));

                                // Linear solutions are easy.
                                LinearSolutions S = (LinearSolutions)ss;
                                foreach (Arrow i in S.Solutions)
                                    code.DeclInit(i.Left, i.Right);
                            }
                            else if (ss is NewtonIteration S)
                            {
                                code.Add(LinqExpr.Label(LinqExpr.Label("newton_iterations")));

                                _log.WriteLine(MessageType.Info, $"Newton iteration size: {S.UnknownDeltas.Count()}");
                                foreach (var delta in S.UnknownDeltas)
                                {
                                    var done = code.Decl<bool>();
                                    code[Call.New("Done", delta)] = done;
                                }

                                // Start with the initial guesses from the solution.
                                foreach (Arrow i in S.Guesses)
                                    code.DeclInit(i.Left, i.Right);

                                // int it = iterations
                                LinqExpr it = code.ReDeclInit<int>("it", settings.Iterations);
                                // do { ... --it } while(it > 0)
                                code.DoWhile((@break) =>
                                {
                                    code.Add(LinqExpr.Label(LinqExpr.Label("solve")));

                                    // Solve the un-solved system.
                                    var deps = Solve(code, JxF, S.Equations, S.UnknownDeltas, new HashSet<Expression>(S.Unknowns.Concat(S.Unknowns.Select(u => u.Evaluate(t_t1)))));

                                    deps = new HashSet<Expression>(deps.Select(d => d.Evaluate(t1_t)).Distinct());

                                    code.Add(LinqExpr.Label(LinqExpr.Label("after_solve")));

                                    IEnumerable<Arrow> GetDependentDeltas(Expression expression, IEnumerable<Arrow> deltas, ISet<Expression> variables)
                                    {
                                        cancellationStrategy.ThrowIfCancelled();

                                        var dependencies = expression.GetDependecnies(variables);
                                        var dependent = deltas.Where(d => dependencies.Contains(NewtonIteration.DeltaOf(d.Left)));
                                        variables.ExceptWith(dependencies);
                                        return dependent.Concat(dependent.SelectMany(d => GetDependentDeltas(d.Right, deltas, variables)));
                                    }

                                    // deltas required by outputs
                                    var outputDeltas = output.SelectMany(o => GetDependentDeltas(o, S.KnownDeltas, new HashSet<Expression>(S.Unknowns))).ToArray();

                                    // known deltas required by newton iteration
                                    var newtonDeltas = deps.SelectMany(d => GetDependentDeltas(d, S.KnownDeltas, new HashSet<Expression>(S.Unknowns))).ToArray();

                                    var allRequiredDeltas = newtonDeltas.Concat(outputDeltas).ToHashSet();

                                    // Compile the pre-solved solutions. Order matters.
                                    foreach (Arrow i in S.KnownDeltas.Where(d => allRequiredDeltas.Contains(d)))
                                        code.DeclInit(i.Left, i.Right);

                                    // bool done = true
                                    Func<Expression, LinqExpr> check = expression => LinqExpr.LessThan(Abs(code[NewtonIteration.Delta(expression)]), MultiplyAdd(Abs(code[expression]), LinqExpr.Constant(1e-3), LinqExpr.Constant(1e-5)));

                                    var requiredUnknowns = allRequiredDeltas.Select(d => d.Left).Concat(S.UnknownDeltas).Select(d => NewtonIteration.DeltaOf(d)).ToArray();

                                    LinqExpr done = code.ReDeclInit<bool>("done", requiredUnknowns.Skip(1).Aggregate(check(requiredUnknowns.First()), (le, e) => LinqExpr.AndAlso(le, check(e))));

                                    foreach (Expression i in requiredUnknowns)
                                    {
                                        cancellationStrategy.ThrowIfCancelled();

                                        LinqExpr v = code[i];
                                        LinqExpr dv = code[NewtonIteration.Delta(i)];
                                        code.Add(LinqExpr.AddAssign(v, dv));
                                    }

                                    // if (done) break
                                    code.Add(LinqExpr.IfThen(done, @break));

                                    // --it;
                                    code.Add(LinqExpr.PreDecrementAssign(it));
                                }, LinqExpr.GreaterThan(it, Zero));
                            }
                        }

                        // Update the previous timestep variables.
                        foreach (SolutionSet S in solution.Solutions)
                        {
                            for (int m = MaxDelay; m < 0; m++)
                            {
                                cancellationStrategy.ThrowIfCancelled();

                                Arrow t_tm = Arrow.New(NewtonSimulationBuilder.t, NewtonSimulationBuilder.t + m * solution.TimeStep);
                                Arrow t_tm1 = Arrow.New(NewtonSimulationBuilder.t, NewtonSimulationBuilder.t + (m + 1) * solution.TimeStep);
                                foreach (Expression i in S.Unknowns.Where(i => globals.Keys.Contains(i.Evaluate(t_tm))))
                                    code.Add(LinqExpr.Assign(code[i.Evaluate(t_tm)], code[i.Evaluate(t_tm1)]));
                            }
                        }

                        // Vo += i
                        foreach (Expression i in output.Distinct())
                        {
                            cancellationStrategy.ThrowIfCancelled();

                            LinqExpr Voi = LinqExpr.Constant(0.0);
                            try
                            {
                                Voi = code.Compile(i);
                            }
                            catch (Exception Ex)
                            {
                                _log.WriteLine(MessageType.Warning, Ex.Message);
                            }
                            code.Add(LinqExpr.AddAssign(Vo[i], Voi));
                        }

                        // Vi_t0 = Vi
                        foreach (Expression i in input.Distinct())
                            code.Add(LinqExpr.Assign(code[i.Evaluate(t_t1)], code[i]));

                        // --ov;
                        code.Add(LinqExpr.PreDecrementAssign(ov));
                    }, LinqExpr.GreaterThan(ov, Zero));

                    // Output[i][n] = Vo / Oversample
                    foreach (KeyValuePair<Expression, LinqExpr> i in outputs)
                        code.Add(LinqExpr.Assign(LinqExpr.ArrayAccess(i.Value, n), LinqExpr.Multiply(Vo[i.Key], invOversample)));

                    // Every 256 samples, check for divergence.
                    if (Vo.Any())
                        code.Add(LinqExpr.IfThen(LinqExpr.Equal(LinqExpr.And(n, LinqExpr.Constant(0xFF)), Zero),
                            LinqExpr.Block(Vo.Select(i => LinqExpr.IfThenElse(IsNotReal(i.Value),
                                ThrowSimulationDiverged(n),
                                LinqExpr.Assign(i.Value, i.Value))))));
                });

            // Copy the global state variables back to the globals.
            foreach (var (i, idx) in globals.WithIndex())
                code.Add(LinqExpr.Assign(LinqExpr.ArrayAccess(state, LinqExpr.Constant(idx)), code[i.Key]));

            var lambda = code.Build<Action<int, double, double[][], double[][], double[], double[]>>();
            if (settings.Optimize)
            {
                var visitor = new FindVariablesUsedOnce(cancellationStrategy);
                visitor.Visit(lambda);

                var replacer = new RemoveVariablesUsedOnce(visitor.Usage, cancellationStrategy);
                lambda = replacer.Visit(lambda) as System.Linq.Expressions.Expression<Action<int, double, double[][], double[][], double[], double[]>>;
                Console.WriteLine($"ToReplace: {visitor.Usage.Count}, replaced: {replacer.removed.Count}");
            }

            //var str = lambda.ToString("C#");
            //Console.WriteLine("Generated code:");
            //Console.WriteLine(str);

            var process = lambda.Compile();

            return (process, globals);
        }

        // Solve a system of linear equations
        private static ISet<Expression> Solve(CodeGen code, LinqExpr Ab, IEnumerable<LinearCombination> Equations, IEnumerable<Expression> Unknowns, ISet<Expression> dependencies)
        {
            LinearCombination[] eqs = Equations.ToArray();
            Expression[] deltas = Unknowns.ToArray();

            int M = eqs.Length;
            int N = deltas.Length;

            var all = new HashSet<Expression>();

            // Initialize the matrix.
            for (int i = 0; i < M; ++i)
            {
                LinqExpr Abi = code.ReDeclInit<double[]>("Abi", LinqExpr.ArrayAccess(Ab, LinqExpr.Constant(i)));
                for (int x = 0; x < N; ++x)
                {
                    var delta = deltas[x];
                    var derivative = eqs[i][delta];

                    var deps = derivative.GetDependecnies(dependencies);
                    all.UnionWith(deps);

                    code.Add(LinqExpr.Assign(
                            LinqExpr.ArrayAccess(Abi, LinqExpr.Constant(x)),
                            code.Compile(derivative)));
                }
                var value = eqs[i][1];
                var deps2 = value.GetDependecnies(dependencies);
                all.UnionWith(deps2);

                code.Add(LinqExpr.Assign(
                    LinqExpr.ArrayAccess(Abi, LinqExpr.Constant(N)),
                    code.Compile(value)));
            }

            //code.Add(LinqExpr.Call(dump, Ab));

            // In case we have fewer equations than unknowns, we can avoid dumb failures to converge by just
            // avoiding "uninitialized" memory left over in the buffer from previous solutions.
            for (int i = M; i < N; ++i)
            {
                LinqExpr Abi = code.ReDeclInit<double[]>("Abi", LinqExpr.ArrayAccess(Ab, LinqExpr.Constant(i)));
                code.Add(LinqExpr.Assign(
                    LinqExpr.ArrayAccess(Abi, LinqExpr.Constant(N)),
                    LinqExpr.Constant(0.0)));
            }

            // Gaussian elimination on this turd.
            code.Add(LinqExpr.Call(
                GetMethod<Simulation>(Vector.IsHardwareAccelerated ? nameof(Simulation.RowReduceVector) : nameof(Simulation.RowReduce), Ab.Type, typeof(int), typeof(int)),
                Ab,
                LinqExpr.Constant(M),
                LinqExpr.Constant(N)));

            //code.Add(LinqExpr.Call(dump, Ab));

            // Ab is now upper triangular, solve it.
            for (int j = N - 1; j >= 0; --j)
            {
                LinqExpr _j = LinqExpr.Constant(j);
                LinqExpr Abj = code.ReDeclInit<double[]>("Abi", LinqExpr.ArrayAccess(Ab, _j));

                LinqExpr r = LinqExpr.ArrayAccess(Abj, LinqExpr.Constant(N));
                for (int ji = j + 1; ji < N; ++ji)
                    r = LinqExpr.Add(r, LinqExpr.Multiply(LinqExpr.ArrayAccess(Abj, LinqExpr.Constant(ji)), code[deltas[ji]]));
                code.DeclInit(deltas[j], LinqExpr.Divide(LinqExpr.Negate(r), LinqExpr.ArrayAccess(Abj, _j)));
            }

            return all;
        }


        // Get a method of T with the given name/param types.
        private static MethodInfo GetMethod(Type T, string Name, params Type[] ParamTypes) { return T.GetMethod(Name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, ParamTypes, null); }
        private static MethodInfo GetMethod<T>(string Name, params Type[] ParamTypes) { return GetMethod(typeof(T), Name, ParamTypes); }

        // Returns a throw SimulationDiverged expression at At.
        private LinqExpr ThrowSimulationDiverged(LinqExpr At)
        {
            return LinqExpr.Throw(LinqExpr.New(typeof(SimulationDivergedException).GetConstructor(new Type[] { At.Type }), At));
        }

        private static ParamExpr Decl<T>(CodeGen Target, ICollection<KeyValuePair<Expression, LinqExpr>> Map, Expression Expr, string Name)
        {
            ParamExpr p = Target.Decl<T>(Name);
            Map.Add(new KeyValuePair<Expression, LinqExpr>(Expr, p));
            return p;
        }

        private static ParamExpr Decl<T>(CodeGen Target, ICollection<KeyValuePair<Expression, LinqExpr>> Map, Expression Expr)
        {
            return Decl<T>(Target, Map, Expr, Expr.ToString());
        }

        private static LinqExpr ConstantExpr(double x, Type T)
        {
            if (T == typeof(double))
                return LinqExpr.Constant(x);
            else if (T == typeof(float))
                return LinqExpr.Constant((float)x);
            else
                throw new NotImplementedException("Constant");
        }

        // Returns a * b + c.
        private static LinqExpr MultiplyAdd(LinqExpr a, LinqExpr b, LinqExpr c) { return LinqExpr.Add(LinqExpr.Multiply(a, b), c); }
        // Returns 1 / x.
        private static LinqExpr Reciprocal(LinqExpr x) { return LinqExpr.Divide(ConstantExpr(1.0, x.Type), x); }
        // Returns abs(x).
        private static LinqExpr Abs(LinqExpr x) { return LinqExpr.Call(GetMethod(typeof(Math), "Abs", x.Type), x); }
        // Returns x*x.
        private static LinqExpr Square(LinqExpr x) { return LinqExpr.Multiply(x, x); }

        // Returns true if x is not NaN or Inf
        private static LinqExpr IsNotReal(LinqExpr x)
        {
            return LinqExpr.Or(
                LinqExpr.Call(GetMethod(x.Type, "IsNaN", x.Type), x),
                LinqExpr.Call(GetMethod(x.Type, "IsInfinity", x.Type), x));
        }

    }
}
