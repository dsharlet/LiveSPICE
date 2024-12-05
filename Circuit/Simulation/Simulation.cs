using ComputerAlgebra;
using ComputerAlgebra.LinqCompiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Util;
using LinqExpr = System.Linq.Expressions.Expression;
using ParamExpr = System.Linq.Expressions.ParameterExpression;

namespace Circuit
{
    /// <summary>
    /// Exception thrown when a simulation does not converge.
    /// </summary>
    public class SimulationDiverged : FailedToConvergeException
    {
        private long at;
        /// <summary>
        /// Sample number at which the simulation diverged.
        /// </summary>
        public long At { get { return at; } }

        public SimulationDiverged(string Message, long At) : base(Message) { at = At; }

        public SimulationDiverged(int At) : base("Simulation diverged.") { at = At; }
    }

    /// <summary>
    /// Simulate a circuit.
    /// </summary>
    public class Simulation
    {
        protected static readonly Variable t = TransientSolution.t;

        // Largest delay we expect to see. BDF6 is the largest possible
        // realistic (or theoretically possible?) method.
        protected const int MaxDelay = -6;

        private long n = 0;
        /// <summary>
        /// Get which sample the simulation is at.
        /// </summary>
        public long At { get { return n; } }
        /// <summary>
        /// Get the simulation time.
        /// </summary>
        public double Time { get { return At * TimeStep; } }

        /// <summary>
        /// Get the timestep for the simulation.
        /// </summary>
        public double TimeStep { get { return (double)(Solution.TimeStep * oversample); } }

        private ILog log = new NullLog();
        /// <summary>
        /// Log associated with this simulation.
        /// </summary>
        public ILog Log { get { return log; } set { log = value; } }

        private TransientSolution solution;
        /// <summary>
        /// Solution of the circuit we are simulating.
        /// </summary>
        public TransientSolution Solution
        {
            get { return solution; }
            set { solution = value; InvalidateProcess(); }
        }

        private int oversample = 8;
        /// <summary>
        /// Oversampling factor for this simulation.
        /// </summary>
        public int Oversample { get { return oversample; } set { oversample = value; InvalidateProcess(); } }

        private int iterations = 8;
        /// <summary>
        /// Maximum number of iterations allowed for the simulation to converge.
        /// </summary>
        public int Iterations { get { return iterations; } set { iterations = value; InvalidateProcess(); } }

        /// <summary>
        /// The sampling rate of this simulation, the sampling rate of the transient solution divided by the oversampling factor.
        /// </summary>
        public Expression SampleRate { get { return 1 / (Solution.TimeStep * oversample); } }

        private Expression[] input = new Expression[] { };
        /// <summary>
        /// Expressions representing input samples.
        /// </summary>
        public IEnumerable<Expression> Input { get { return input; } set { input = value.ToArray(); InvalidateProcess(); } }

        private Expression[] output = new Expression[] { };
        /// <summary>
        /// Expressions for output samples.
        /// </summary>
        public IEnumerable<Expression> Output { get { return output; } set { output = value.ToArray(); InvalidateProcess(); } }

        // Stores any global state in the simulation (previous state values, mostly).
        private Dictionary<Expression, GlobalExpr<double>> globals = new Dictionary<Expression, GlobalExpr<double>>();
        // Add a new global and set it to 0 if it didn't already exist.
        private void AddGlobal(Expression Name)
        {
            if (!globals.ContainsKey(Name))
                globals.Add(Name, new GlobalExpr<double>(0.0));
        }

        /// <summary>
        /// Create a simulation using the given solution and the specified inputs/outputs.
        /// </summary>
        /// <param name="Solution">Transient solution to run.</param>
        /// <param name="Input">Expressions in the solution to be defined by input samples.</param>
        /// <param name="Output">Expressions describing outputs to be saved from the simulation.</param>
        public Simulation(TransientSolution Solution)
        {
            solution = Solution;

            // If any system depends on the previous value of an unknown, we need a global variable for it.
            for (int n = -1; n >= MaxDelay; n--)
            {
                Arrow t_tn = Arrow.New(t, t + n * Solution.TimeStep);
                IEnumerable<Expression> unknowns_tn = Solution.Solutions.SelectMany(i => i.Unknowns).Select(i => i.Evaluate(t_tn));
                if (!Solution.Solutions.Any(i => i.DependsOn(unknowns_tn)))
                    break;
                
                foreach (Expression i in Solution.Solutions.SelectMany(i => i.Unknowns))
                    AddGlobal(i.Evaluate(t_tn));
            }

            // Also need globals for any Newton's method unknowns.
            Arrow t_t1 = Arrow.New(t, t - Solution.TimeStep);
            foreach (Expression i in Solution.Solutions.OfType<NewtonIteration>().SelectMany(i => i.Unknowns))
                AddGlobal(i.Evaluate(t_t1));

            // Set the global values to the initial conditions of the solution.
            foreach (KeyValuePair<Expression, GlobalExpr<double>> i in globals)
            {
                // Dumb hack to get f[t - x] -> f[0] for any x.
                Expression i_t0 = i.Key.Evaluate(t, Real.Infinity).Substitute(Real.Infinity, 0);
                Expression init = i_t0.Evaluate(Solution.InitialConditions);
                i.Value.Value = init is Constant ? (double)init : 0.0;
            }

            InvalidateProcess();
        }

        /// <summary>
        /// Process some samples with this simulation. The Input and Output buffers must match the enumerations provided
        /// at initialization.
        /// </summary>
        /// <param name="N">Number of samples to process.</param>
        /// <param name="Input">Buffers that describe the input samples.</param>
        /// <param name="Output">Buffers to receive output samples.</param>
        public void Run(int N, IEnumerable<double[]> Input, IEnumerable<double[]> Output)
        {
            if (_process == null)
                _process = DefineProcess();

            try
            {
                try
                {
                    _process(N, n*TimeStep, Input.AsArray(), Output.AsArray());
                    n += N;
                }
                catch (TargetInvocationException Ex)
                {
                    throw Ex.InnerException;
                }
            }
            catch (SimulationDiverged Ex)
            {
                throw new SimulationDiverged("Simulation diverged near t = " + Quantity.ToString(Time, Units.s) + " + " + Ex.At, n + Ex.At);
            }
        }
        public void Run(int N, IEnumerable<double[]> Output) { Run(N, new double[][] { }, Output); }
        public void Run(double[] Input, IEnumerable<double[]> Output) { Run(Input.Length, new[] { Input }, Output); }
        public void Run(double[] Input, double[] Output) { Run(Input.Length, new[] { Input }, new[] { Output }); }

        private Action<int, double, double[][], double[][]> _process;
        // Force rebuilding of the process function.
        private void InvalidateProcess()
        {
            _process = null;
        }

        // The resulting lambda processes N samples, using buffers provided for Input and Output:
        //  void Process(int N, double t0, double T, double[] Input0 ..., double[] Output0 ...)
        //  { ... }
        private Action<int, double, double[][], double[][]> DefineProcess()
        {
            // Map expressions to identifiers in the syntax tree.
            var inputs = new List<KeyValuePair<Expression, LinqExpr>>();
            var outputs = new List<KeyValuePair<Expression, LinqExpr>>();

            // Lambda code generator.
            CodeGen code = new CodeGen();

            // Create parameters for the basic simulation info (N, t, Iterations).
            ParamExpr SampleCount = code.Decl<int>(Scope.Parameter, "SampleCount");
            ParamExpr t = code.Decl(Scope.Parameter, Simulation.t);
            var ins = code.Decl<double[][]>(Scope.Parameter, "ins");
            var outs = code.Decl<double[][]>(Scope.Parameter, "outs");

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

            Arrow t_t1 = Arrow.New(Simulation.t, Simulation.t - Solution.TimeStep);

            // Create globals to store previous values of inputs.
            foreach (Expression i in Input.Distinct())
                AddGlobal(i.Evaluate(t_t1));

            // Define lambda body.

            // int Zero = 0
            LinqExpr Zero = LinqExpr.Constant(0);

            // double h = T / Oversample
            LinqExpr h = LinqExpr.Constant(TimeStep / (double)Oversample);

            // double invOversample = 1 / Oversample
            LinqExpr invOversample = LinqExpr.Constant(1.0 / (double)Oversample);

            // Load the globals to local variables and add them to the map.
            foreach (KeyValuePair<Expression, GlobalExpr<double>> i in globals)
                code.DeclInit(i.Key, i.Value);

            foreach (KeyValuePair<Expression, LinqExpr> i in inputs)
                code.DeclInit(i.Key, code[i.Key.Evaluate(t_t1)]);

            // Create arrays for linear systems.
            int M = Solution.Solutions.OfType<NewtonIteration>().Max(i => i.Equations.Count(), 0);
            int N = Solution.Solutions.OfType<NewtonIteration>().Max(i => i.UnknownDeltas.Count(), 0);
            // If there is an underdetermined system of equations, avoid out of bounds reads.
            M = Math.Max(M, N);
            // Add a column for the solution vector.
            ++N;
            Log.WriteLine(MessageType.Verbose, Vector.IsHardwareAccelerated ? "Vector hardware acceleration enabled" : "No vector hardware acceleration");

            LinqExpr JxF = code.DeclInit<double[][]>("JxF", LinqExpr.NewArrayBounds(typeof(double[]), LinqExpr.Constant(M)));
            for (int j = 0; j < M; ++j)
                code.Add(LinqExpr.Assign(LinqExpr.ArrayAccess(JxF, LinqExpr.Constant(j)), LinqExpr.NewArrayBounds(typeof(double), Vector.IsHardwareAccelerated ? LinqExpr.Constant(N + Vector<double>.Count - 1) : LinqExpr.Constant(N))));

            // for (int n = 0; n < SampleCount; ++n)
            ParamExpr n = code.Decl<int>("n");
            code.For(
                () => code.Add(LinqExpr.Assign(n, Zero)),
                LinqExpr.LessThan(n, SampleCount),
                () => code.Add(LinqExpr.PreIncrementAssign(n)),
                () =>
                {
                    // Prepare input samples for oversampling interpolation.
                    Dictionary<Expression, LinqExpr> dVi = new Dictionary<Expression, LinqExpr>();
                    foreach (Expression i in Input.Distinct())
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
                    foreach (Expression i in Output.Distinct())
                        code.Add(LinqExpr.Assign(
                            Decl<double>(code, Vo, i, i.ToString().Replace("[t]", "")),
                            LinqExpr.Constant(0.0)));

                    // int ov = Oversample; 
                    // do { -- ov; } while(ov > 0)
                    ParamExpr ov = code.Decl<int>("ov");
                    code.Add(LinqExpr.Assign(ov, LinqExpr.Constant(Oversample)));
                    code.DoWhile(() =>
                    {
                        // t += h
                        code.Add(LinqExpr.AddAssign(t, h));

                        // Interpolate the input samples.
                        foreach (Expression i in Input.Distinct())
                            code.Add(LinqExpr.AddAssign(code[i], dVi[i]));

                        // Compile all of the SolutionSets in the solution.
                        foreach (SolutionSet ss in Solution.Solutions)
                        {
                            if (ss is LinearSolutions)
                            {
                                // Linear solutions are easy.
                                LinearSolutions S = (LinearSolutions)ss;
                                foreach (Arrow i in S.Solutions)
                                    code.DeclInit(i.Left, i.Right);
                            }
                            else if (ss is NewtonIteration)
                            {
                                NewtonIteration S = (NewtonIteration)ss;

                                // Start with the initial guesses from the solution.
                                foreach (Arrow i in S.Guesses)
                                    code.DeclInit(i.Left, i.Right);

                                // int it = iterations
                                LinqExpr it = code.ReDeclInit<int>("it", Iterations);
                                // do { ... --it } while(it > 0)
                                code.DoWhile((Break) =>
                                {
                                    // Solve the un-solved system.
                                    Solve(code, JxF, S.Equations, S.UnknownDeltas);

                                    // Compile the pre-solved solutions.
                                    if (S.KnownDeltas != null)
                                        foreach (Arrow i in S.KnownDeltas)
                                            code.DeclInit(i.Left, i.Right);

                                    // bool done = true
                                    LinqExpr done = code.ReDeclInit("done", true);
                                    foreach (Expression i in S.Unknowns)
                                    {
                                        LinqExpr v = code[i];
                                        LinqExpr dv = code[NewtonIteration.Delta(i)];

                                        // done &= (|dv| < |v|*epsilon)
                                        code.Add(LinqExpr.AndAssign(done, LinqExpr.LessThan(Abs(dv), MultiplyAdd(Abs(v), LinqExpr.Constant(1e-4), LinqExpr.Constant(1e-6)))));
                                        // v += dv
                                        code.Add(LinqExpr.AddAssign(v, dv));
                                    }
                                    // if (done) break
                                    code.Add(LinqExpr.IfThen(done, Break));

                                    // --it;
                                    code.Add(LinqExpr.PreDecrementAssign(it));
                                }, LinqExpr.GreaterThan(it, Zero));

                                //// bool failed = false
                                //LinqExpr failed = Decl(code, code, "failed", LinqExpr.Constant(false));
                                //for (int i = 0; i < eqs.Length; ++i)
                                //    // failed |= |JxFi| > epsilon
                                //    code.Add(LinqExpr.OrAssign(failed, LinqExpr.GreaterThan(Abs(eqs[i].ToExpression().Compile(map)), LinqExpr.Constant(1e-3))));

                                //code.Add(LinqExpr.IfThen(failed, ThrowSimulationDiverged(n)));
                            }
                        }

                        // Update the previous timestep variables.
                        foreach (SolutionSet S in Solution.Solutions)
                        {
                            for (int m = MaxDelay; m < 0; m++)
                            {
                                Arrow t_tm = Arrow.New(Simulation.t, Simulation.t + m * Solution.TimeStep);
                                Arrow t_tm1 = Arrow.New(Simulation.t, Simulation.t + (m + 1) * Solution.TimeStep);
                                foreach (Expression i in S.Unknowns.Where(i => globals.Keys.Contains(i.Evaluate(t_tm))))
                                    code.Add(LinqExpr.Assign(code[i.Evaluate(t_tm)], code[i.Evaluate(t_tm1)]));
                            }
                        }

                        // Vo += i
                        foreach (Expression i in Output.Distinct())
                        {
                            LinqExpr Voi = LinqExpr.Constant(0.0);
                            try
                            {
                                Voi = code.Compile(i);
                            }
                            catch (Exception Ex)
                            {
                                Log.WriteLine(MessageType.Warning, Ex.Message);
                            }
                            code.Add(LinqExpr.AddAssign(Vo[i], Voi));
                        }

                        // Vi_t0 = Vi
                        foreach (Expression i in Input.Distinct())
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
                                LinqExpr.Assign(i.Value, RoundDenormToZero(i.Value)))))));
                });

            // Copy the global state variables back to the globals.
            foreach (KeyValuePair<Expression, GlobalExpr<double>> i in globals)
                code.Add(LinqExpr.Assign(i.Value, code[i.Key]));

            var lambda = code.Build<Action<int, double, double[][], double[][]>>();
            return lambda.Compile();
        }

        // Solve a system of linear equations
        private static void Solve(CodeGen code, LinqExpr Ab, IEnumerable<LinearCombination> Equations, IEnumerable<Expression> Unknowns)
        {
            LinearCombination[] eqs = Equations.ToArray();
            Expression[] deltas = Unknowns.ToArray();

            int M = eqs.Length;
            int N = deltas.Length;

            // Initialize the matrix.
            for (int i = 0; i < M; ++i)
            {
                LinqExpr Abi = code.ReDeclInit<double[]>("Abi", LinqExpr.ArrayAccess(Ab, LinqExpr.Constant(i)));
                for (int x = 0; x < N; ++x)
                    code.Add(LinqExpr.Assign(
                        LinqExpr.ArrayAccess(Abi, LinqExpr.Constant(x)),
                        code.Compile(eqs[i][deltas[x]])));
                code.Add(LinqExpr.Assign(
                    LinqExpr.ArrayAccess(Abi, LinqExpr.Constant(N)),
                    code.Compile(eqs[i][1])));
            }
            // In case we have fewer equations than unknowns, we can avoid dumb failures to converge by just
            // avoiding "uninitialized" memory left over in the buffer from previous solutions.
            for (int i = M; i < N; ++i)
            {
                LinqExpr Abi = code.ReDeclInit<double[]>("Abi", LinqExpr.ArrayAccess(Ab, LinqExpr.Constant(i)));
                code.Add(LinqExpr.Assign(
                    LinqExpr.ArrayAccess(Abi, LinqExpr.Constant(N)), 
                    LinqExpr.Constant(0.0)));
            }

            // Fully solve this system of equations.
            code.Add(LinqExpr.Call(
                GetMethod<Simulation>(Vector.IsHardwareAccelerated ? nameof(SolveVector) : nameof(Solve), Ab.Type, typeof(int), typeof(int)),
                Ab,
                LinqExpr.Constant(M),
                LinqExpr.Constant(N + 1)));

            // Extract the solutions.
            for (int j = 0; j < N; ++j)
                code.DeclInit(deltas[j], LinqExpr.Negate(LinqExpr.ArrayAccess(LinqExpr.ArrayAccess(Ab, LinqExpr.Constant(j)), LinqExpr.Constant(N))));
        }

        // A human readable implementation of RowReduce.
        public static void Solve(double[][] Ab, int M, int N)
        {
            // Solve for dx.
            // For each column...
            for (int j = 0; j < Math.Min(M, N); ++j)
            {
                int pi = j;
                double max = Math.Abs(Ab[j][j]);

                // Find a pivot row for this variable.
                for (int i = j + 1; i < M; ++i)
                {
                    double[] Abi = Ab[i];
                    // if(|JxF[i][j]| > max) { pi = i, max = |JxF[i][j]| }
                    double maxj = Math.Abs(Abi[j]);
                    if (maxj > max)
                    {
                        pi = i;
                        max = maxj;
                    }
                }

                // Swap pivot row with the current row.
                if (pi != j)
                {
                    var Abpi = Ab[pi];
                    Ab[pi] = Ab[j];
                    Ab[j] = Abpi;
                }

                double[] Abj = Ab[j];

                // Eliminate all other rows.
                double p = Abj[j];
                if (p == 0) continue;
                for (int i = 0; i < M; ++i)
                {
                    if (i == j) continue;
                    double[] Abi = Ab[i];
                    if (Abi[j] == 0.0) continue;

                    double s = Abi[j] / p;
                    for (int ij = j + 1; ij < N; ++ij)
                        Abi[ij] -= Abj[ij] * s;
                }

                // Scale the pivot row, so the pivot is one.
                double inv_p = 1.0 / p;
                for (int ij = j + 1; ij < N; ++ij)
                    Abj[ij] *= inv_p;
            }
        }

        //This algorith has no tail-loop - it requires arrays to be padded to N + Vector.Count - 1
        private static void SolveVector(double[][] Ab, int M, int N)
        {
            var vectorLength = Vector<double>.Count;

            // Solve for dx.
            // For each variable in the system...
            for (int j = 0; j < Math.Min(M, N); ++j)
            {
                int pi = j;
                double max = Math.Abs(Ab[j][j]);

                // Find a pivot row for this variable.
                for (int i = j + 1; i < M; ++i)
                {
                    // if(|JxF[i][j]| > max) { pi = i, max = |JxF[i][j]| }
                    double maxj = Math.Abs(Ab[i][j]);
                    if (maxj > max)
                    {
                        pi = i;
                        max = maxj;
                    }
                }

                // Swap pivot row with the current row.
                if (pi != j)
                {
                    var tmp = Ab[pi];
                    Ab[pi] = Ab[j];
                    Ab[j] = tmp;
                }

                double[] Abj = Ab[j];

                // Eliminate all other rows.
                double p = Abj[j];
                if (p == 0) continue;
                for (int i = 0; i < M; ++i)
                {
                    if (i == j) continue;
                    double[] Abi = Ab[i];
                    if (Abi[j] == 0) continue;

                    double s = Abi[j] / p;
                    for (int ij = j + 1; ij < N; ij += vectorLength)
                    {
                        var source = new Vector<double>(Abj, ij);
                        var target = new Vector<double>(Abi, ij);
                        var res = target - (source * s);
                        res.CopyTo(Abi, ij);
                    }
                }

                // Scale the pivot row, so the pivot is one.
                // TODO: Vectorize
                double inv_p = 1.0 / p;
                for (int ij = j + 1; ij < N; ++ij)
                    Abj[ij] *= inv_p;
            }
        }

        // Returns a throw SimulationDiverged expression at At.
        private LinqExpr ThrowSimulationDiverged(LinqExpr At)
        {
            return LinqExpr.Throw(LinqExpr.New(typeof(SimulationDiverged).GetConstructor(new Type[] { At.Type }), At));
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

        // Get a method of T with the given name/param types.
        private static MethodInfo GetMethod(Type T, string Name, params Type[] ParamTypes) { return T.GetMethod(Name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, ParamTypes, null); }
        private static MethodInfo GetMethod<T>(string Name, params Type[] ParamTypes) { return GetMethod(typeof(T), Name, ParamTypes); }

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
        // Round x to zero if it is sub-normal.
        private static LinqExpr RoundDenormToZero(LinqExpr x) { return x; }
    }
}
