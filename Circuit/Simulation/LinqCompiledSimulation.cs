using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Reflection.Emit;
using SyMath;
using LinqExprs = System.Linq.Expressions;
using LinqExpr = System.Linq.Expressions.Expression;
using ParamExpr = System.Linq.Expressions.ParameterExpression;

namespace Circuit
{
    /// <summary>
    /// Simulate a circuit by compiling the solution via LINQ expressions.
    /// </summary>
    public class LinqCompiledSimulation : Simulation
    {
        // Stores any global state in the simulation (previous state values, mostly).
        private Dictionary<Expression, GlobalExpr<double>> globals = new Dictionary<Expression, GlobalExpr<double>>();

        /// <summary>
        /// Create a simulation for the given system solution.
        /// </summary>
        /// <param name="Solution">Transient solution to run.</param>
        /// <param name="Log">Log for simulation output.</param>
        public LinqCompiledSimulation(TransientSolution Solution, int Oversample, ILog Log) : base(Solution, Oversample, Log)
        {
            foreach (Expression i in Solution.Solutions.SelectMany(i => i.Unknowns))
            {
                // If any system depends on the previous value of i, we need a global variable for it.
                if (Solution.Solutions.Any(j => j.DependsOn(i.Evaluate(t, t0))))
                    globals[i.Evaluate(t, t0)] = new GlobalExpr<double>(0.0);
            }
            // Also need globals for any Newton's method unknowns.
            foreach (Expression i in Solution.Solutions.OfType<NewtonIteration>().SelectMany(i => i.Unknowns))
                globals[i.Evaluate(t, t0)] = new GlobalExpr<double>(0.0);

            Reset();
        }

        public override void Reset()
        {
            base.Reset();

            foreach (KeyValuePair<Expression, GlobalExpr<double>> i in globals)
            {
                Expression init = i.Key.Evaluate(t0, 0).Evaluate(Solution.InitialConditions);
                i.Value.Value = init is Constant ? (double)init : 0.0;
            }
        }

        protected override void Process(
            long n, double T,
            int N,
            IEnumerable<KeyValuePair<Expression, double[]>> Input,
            IEnumerable<KeyValuePair<Expression, double[]>> Output,
            IEnumerable<KeyValuePair<Expression, double>> Arguments,
            int Oversample, int Iterations)
        {
            Delegate processor = Compile(T, Oversample, Iterations, Input.Select(i => i.Key), Output.Select(i => i.Key), Arguments.Select(i => i.Key));

            // Build parameter list for the processor.
            List<object> parameters = new List<object>(3 + Input.Count() + Output.Count() + Arguments.Count());
            parameters.Add(N);
            parameters.Add((double)n * T);
            foreach (KeyValuePair<Expression, double[]> i in Input)
                parameters.Add(i.Value);
            foreach (KeyValuePair<Expression, double[]> i in Output)
                parameters.Add(i.Value);
            foreach (KeyValuePair<Expression, double> i in Arguments)
                parameters.Add(i.Value);

            try
            {
                processor.DynamicInvoke(parameters.ToArray());
            }
            catch (TargetInvocationException Ex)
            {
                throw Ex.InnerException;
            }
        }
        
        // Compile and cache delegates for processing various IO configurations for this simulation.
        private Dictionary<int, Delegate> compiled = new Dictionary<int, Delegate>();
        private Delegate Compile(double T, int Oversample, int Iterations, IEnumerable<Expression> Input, IEnumerable<Expression> Output, IEnumerable<Expression> Parameters)
        {
            int hash = OrderedHashCode(
                T.GetHashCode(),
                Oversample.GetHashCode(),
                Iterations.GetHashCode(),
                Input.OrderedHashCode(), 
                Output.OrderedHashCode(), 
                Parameters.OrderedHashCode());

            Delegate d;
            if (compiled.TryGetValue(hash, out d))
                return d;

            Timer time = new Timer();

            Log.WriteLine(MessageType.Info, "[{0}] Defining sample processing function...", time);
            Log.WriteLine(MessageType.Verbose, "Inputs = {{ " + Input.UnSplit(", ") + " }}");
            Log.WriteLine(MessageType.Verbose, "Outputs = {{ " + Output.UnSplit(", ") + " }}");
            Log.WriteLine(MessageType.Verbose, "Parameters = {{ " + Parameters.UnSplit(", ") + " }}");
            LinqExprs.LambdaExpression lambda = DefineProcessFunction(T, Oversample, Iterations, Input, Output, Parameters);
            Log.WriteLine(MessageType.Info, "[{0}] Compiling sample processing function...", time);
            d = lambda.Compile();
            Log.WriteLine(MessageType.Info, "[{0}] Done.", time);

            return compiled[hash] = d;
        }
        
        // The resulting lambda processes N samples, using buffers provided for Input and Output:
        //  void Process(int N, double t0, double T, double[] Input0 ..., double[] Output0 ..., double Parameter0 ...)
        //  { ... }
        private LinqExprs.LambdaExpression DefineProcessFunction(double T, int Oversample, int Iterations, IEnumerable<Expression> Input, IEnumerable<Expression> Output, IEnumerable<Expression> Parameters)
        {
            // Map expressions to identifiers in the syntax tree.
            Dictionary<Expression, LinqExpr> inputs = new Dictionary<Expression, LinqExpr>();
            List<KeyValuePair<Expression, LinqExpr>> outputs = new List<KeyValuePair<Expression, LinqExpr>>();
            
            // Lambda code generator.
            LinqCodeGen code = new LinqCodeGen();

            // Create parameters for the basic simulation info (N, t, T, Oversample, Iterations).
            ParamExpr SampleCount = code.DeclParameter<int>("SampleCount");
            ParamExpr t0 = code.DeclParameter(Simulation.t0);
            // Create buffer parameters for each input, output.
            foreach (Expression i in Input)
            {
                ParamExpr p = code.DeclParameter<double[]>(i.ToString());
                inputs.Add(i, p);
            }
            foreach (Expression i in Output)
            {
                ParamExpr p = code.DeclParameter<double[]>(i.ToString());
                outputs.Add(new KeyValuePair<Expression, LinqExpr>(i, p));
            }
            // Create constant parameters for simulation parameters.
            foreach (Expression i in Parameters)
                code.DeclParameter(i);

            // Create globals to store previous values of input.
            foreach (Expression i in Input)
                globals[i.Evaluate(t_t0)] = new GlobalExpr<double>(0.0);

            // Define lambda body.

            // int Zero = 0
            LinqExpr Zero = LinqExpr.Constant(0);

            // double t = t0
            ParamExpr t = code.Decl(Simulation.t);
            code.Add(LinqExpr.Assign(t, t0));

            // double h = T / Oversample
            LinqExpr h = LinqExpr.Constant((double)T / (double)Oversample);

            // Load the globals to local variables and add them to the map.
            foreach (KeyValuePair<Expression, GlobalExpr<double>> i in globals)
                code.Add(LinqExpr.Assign(code.Decl(i.Key), i.Value));

            foreach (KeyValuePair<Expression, LinqExpr> i in inputs)
                code.Add(LinqExpr.Assign(code.Decl(i.Key), code[i.Key.Evaluate(t_t0)]));

            // Create arrays for the newton's method systems.
            int M = Solution.Solutions.OfType<NewtonIteration>().Max(i => i.Equations.Count(), 0);
            int N = Solution.Solutions.OfType<NewtonIteration>().Max(i => i.Updates.Count(), 0) + 1;
            LinqExpr JxF = code.Decl<double[][]>("JxF", LinqExpr.NewArrayBounds(typeof(double[]), LinqExpr.Constant(M)));
            for (int j = 0; j < M; ++j)
                code.Add(LinqExpr.Assign(LinqExpr.ArrayAccess(JxF, LinqExpr.Constant(j)), LinqExpr.NewArrayBounds(typeof(double), LinqExpr.Constant(N))));

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
                foreach (Expression i in Input)
                {
                    LinqExpr Va = code[i];
                    LinqExpr Vb = LinqExpr.ArrayAccess(inputs[i], n);

                    // dVi = (Vb - Va) / Oversample
                    code.Add(LinqExpr.Assign(
                        Decl<double>(code, dVi, i, "d" + i.ToString().Replace("[t]", "")),
                        LinqExpr.Multiply(LinqExpr.Subtract(Vb, Va), LinqExpr.Constant(1.0 / (double)Oversample))));
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
                    foreach (Expression i in Input)
                        code.Add(LinqExpr.AddAssign(code[i], dVi[i]));

                    // Compile all of the SolutionSets in the solution.
                    foreach (SolutionSet ss in Solution.Solutions)
                    {
                        if (ss is LinearSolutions)
                        {
                            // Linear solutions are easy.
                            LinearSolutions S = (LinearSolutions)ss;
                            foreach (Arrow i in S.Solutions)
                                code.Decl(i.Left, i.Right);
                        }
                        else if (ss is NewtonIteration)
                        {
                            NewtonIteration S = (NewtonIteration)ss;
                            
                            LinearCombination[] eqs = S.Equations.ToArray();
                            Expression[] deltas = S.Updates.ToArray();

                            // Start with the initial guesses from the solution.
                            foreach (Arrow i in S.Guesses)
                                code.Decl(i.Left, i.Right);
                            
                            // int it = iterations
                            ParamExpr it = code.ReDecl("it", Iterations);
                            // do { ... --it } while(it > 0)
                            code.DoWhile((Break) =>
                            {
                                // Initialize the matrix.
                                for (int i = 0; i < eqs.Length; ++i)
                                {
                                    LinqExpr JxFi = code.ReDecl<double[]>("JxFi", LinqExpr.ArrayAccess(JxF, LinqExpr.Constant(i)));
                                    for (int x = 0; x < deltas.Length; ++x)
                                        code.Add(LinqExpr.Assign(
                                            LinqExpr.ArrayAccess(JxFi, LinqExpr.Constant(x)),
                                            code.Compile(eqs[i][deltas[x]])));
                                    code.Add(LinqExpr.Assign(
                                        LinqExpr.ArrayAccess(JxFi, LinqExpr.Constant(deltas.Length)),
                                        code.Compile(eqs[i][1])));
                                }
                                                                
                                // Gaussian elimination on this turd.

                                // For each variable in the system...
                                for (int j = 0; j < deltas.Length; ++j)
                                {
                                    LinqExpr _j = LinqExpr.Constant(j);
                                    LinqExpr JxFj = code.ReDecl<double[]>("JxFj", LinqExpr.ArrayAccess(JxF, _j));
                                    // int pi = j
                                    LinqExpr pi = code.ReDecl<int>("pi", _j);
                                    // double max = |JxF[j][j]|
                                    LinqExpr max = code.ReDecl<double>("max", Abs(LinqExpr.ArrayAccess(JxFj, _j)));
                                    
                                    // Find a pivot row for this variable.
                                    for (int i = j + 1; i < eqs.Length; ++i)
                                    {
                                        LinqExpr _i = LinqExpr.Constant(i);

                                        // if(|JxF[i][j]| > max) { pi = i, max = |JxF[i][j]| }
                                        LinqExpr maxj = code.ReDecl<double>("maxj", Abs(LinqExpr.ArrayAccess(LinqExpr.ArrayAccess(JxF, _i), _j)));
                                        code.Add(LinqExpr.IfThen(
                                            LinqExpr.GreaterThan(maxj, max),
                                            LinqExpr.Block(
                                                LinqExpr.Assign(pi, _i), 
                                                LinqExpr.Assign(max, maxj))));
                                    }
                                    
                                    // (Maybe) swap the pivot row with the current row.
                                    LinqExpr JxFpi = code.ReDecl<double[]>("JxFpi");
                                    code.Add(LinqExpr.IfThen(
                                        LinqExpr.NotEqual(_j, pi), LinqExpr.Block(
                                            new[] { LinqExpr.Assign(JxFpi, LinqExpr.ArrayAccess(JxF, pi)) }.Concat(
                                            Enumerable.Range(j, deltas.Length + 1 - j).Select(x => Swap(
                                                LinqExpr.ArrayAccess(JxFj, LinqExpr.Constant(x)),
                                                LinqExpr.ArrayAccess(JxFpi, LinqExpr.Constant(x)),
                                                code.ReDecl<double>("swap")))))));

                                    //// It's hard to believe this swap isn't faster than the above...
                                    //code.Add(LinqExpr.IfThen(LinqExpr.NotEqual(_j, pi), LinqExpr.Block(
                                    //    Swap(LinqExpr.ArrayAccess(JxF, _j), LinqExpr.ArrayAccess(JxF, pi), Redeclare<double[]>(code, "temp")),
                                    //    LinqExpr.Assign(JxFj, LinqExpr.ArrayAccess(JxF, _j)))));
                                    
                                    // Eliminate the rows after the pivot.
                                    LinqExpr p = code.ReDecl<double>("p", LinqExpr.ArrayAccess(JxFj, _j));
                                    for (int i = j + 1; i < eqs.Length; ++i)
                                    {
                                        LinqExpr _i = LinqExpr.Constant(i);
                                        LinqExpr JxFi = code.ReDecl<double[]>("JxFi", LinqExpr.ArrayAccess(JxF, _i));

                                        // s = JxF[i][j] / p
                                        LinqExpr s = code.ReDecl<double>("scale", LinqExpr.Divide(LinqExpr.ArrayAccess(JxFi, _j), p));
                                        // JxF[i] -= JxF[j] * s
                                        for (int ji = j + 1; ji < deltas.Length + 1; ++ji)
                                        {
                                            LinqExpr _ji = LinqExpr.Constant(ji);
                                            code.Add(LinqExpr.SubtractAssign(
                                                LinqExpr.ArrayAccess(JxFi, _ji),
                                                LinqExpr.Multiply(LinqExpr.ArrayAccess(JxFj, _ji), s)));
                                        }
                                    }
                                }

                                // JxF is now upper triangular, solve it.
                                for (int j = deltas.Length - 1; j >= 0; --j)
                                {
                                    LinqExpr _j = LinqExpr.Constant(j);
                                    LinqExpr JxFj = code.ReDecl<double[]>("JxFj", LinqExpr.ArrayAccess(JxF, _j));

                                    LinqExpr r = LinqExpr.ArrayAccess(JxFj, LinqExpr.Constant(deltas.Length));
                                    for (int ji = j + 1; ji < deltas.Length; ++ji)
                                        r = LinqExpr.Add(r, LinqExpr.Multiply(LinqExpr.ArrayAccess(JxFj, LinqExpr.Constant(ji)), code[deltas[ji]]));
                                    code.Decl(deltas[j], LinqExpr.Divide(LinqExpr.Negate(r), LinqExpr.ArrayAccess(JxFj, _j)));
                                }

                                // Compile the pre-solved solutions.
                                if (S.Solved != null)
                                    foreach (Arrow i in S.Solved)
                                        code.Decl(i.Left, i.Right);

                                // bool done = true
                                LinqExpr done = code.ReDecl("done", true);
                                foreach (Expression i in S.Unknowns)
                                {
                                    LinqExpr v = code[i];
                                    LinqExpr dv = code[NewtonIteration.Delta(i)];

                                    // done &= (|dv| < |v|*epsilon)
                                    code.Add(LinqExpr.AndAssign(done, LinqExpr.LessThan(LinqExpr.Multiply(Abs(dv), LinqExpr.Constant(1e4)), Abs(v))));
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
                        foreach (Expression i in S.Unknowns.Where(i => globals.Keys.Contains(i.Evaluate(t_t0))))
                            code.Add(LinqExpr.Assign(code[i.Evaluate(t_t0)], code[i]));

                    // t0 = t
                    code.Add(LinqExpr.Assign(t0, t));

                    // Vo += i
                    foreach (Expression i in Output.Distinct())
                        code.Add(LinqExpr.AddAssign(Vo[i], CompileOrWarn(i, code)));
                    
                    // Vi_t0 = Vi
                    foreach (Expression i in Input)
                        code.Add(LinqExpr.Assign(code[i.Evaluate(t_t0)], code[i]));

                    // --ov;
                    code.Add(LinqExpr.PreDecrementAssign(ov));
                }, LinqExpr.GreaterThan(ov, Zero));
                
                // Output[i][n] = Vo / Oversample
                foreach (KeyValuePair<Expression, LinqExpr> i in outputs)
                    code.Add(LinqExpr.Assign(LinqExpr.ArrayAccess(i.Value, n), LinqExpr.Multiply(Vo[i.Key], LinqExpr.Constant(1.0 / (double)Oversample))));

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

            return code.CreateLambda();
        }
        
        // If x fails to compile, return 0. 
        private LinqExpr CompileOrWarn(Expression x, IDictionary<Expression, LinqExpr> map)
        {
            try
            {
                return x.Compile(map);
            }
            catch (Exception ex)
            {
                Log.WriteLine(MessageType.Warning, "Warning: Error compiling output expression '{0}': {1}", x.ToString(), ex.Message);
                return LinqExpr.Constant(0.0);
            }
        }

        // Returns a throw SimulationDiverged expression at At.
        private LinqExpr ThrowSimulationDiverged(LinqExpr At)
        {
            return LinqExpr.Throw(LinqExpr.New(typeof(SimulationDiverged).GetConstructor(new Type[] { At.Type }), At));
        }
                        
        private static ParamExpr Decl<T>(LinqCodeGen Target, ICollection<KeyValuePair<Expression, LinqExpr>> Map, Expression Expr, string Name)
        {
            ParamExpr p = Target.Decl<T>(Name);
            Map.Add(new KeyValuePair<Expression, LinqExpr>(Expr, p));
            return p;
        }

        private static ParamExpr Decl<T>(LinqCodeGen Target, ICollection<KeyValuePair<Expression, LinqExpr>> Map, Expression Expr)
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

        private static int OrderedHashCode(params int[] Hashes)
        {
            int hash = 17;
            foreach (int i in Hashes)
                hash = hash * 33 + i;
            return hash;
        }

        // Returns 1 / x.
        private static LinqExpr Reciprocal(LinqExpr x) { return LinqExpr.Divide(ConstantExpr(1.0, x.Type), x); }
        // Returns abs(x).
        private static LinqExpr Abs(LinqExpr x) { return LinqExpr.Call(typeof(System.Math).GetMethod("Abs", new Type[] { x.Type }), x); }
        // Returns x*x.
        private static LinqExpr Square(LinqExpr x) { return LinqExpr.Multiply(x, x); }
        
        private static MethodInfo IsNaN = typeof(double).GetMethod("IsNaN");
        private static MethodInfo IsInfinity = typeof(double).GetMethod("IsInfinity");
        // Returns true if x is not NaN or Inf
        private static LinqExpr IsNotReal(LinqExpr x) { return LinqExpr.Or(LinqExpr.Call(null, IsNaN, x), LinqExpr.Call(null, IsInfinity, x)); }
        // Round x to zero if it is sub-normal.
        private static LinqExpr RoundDenormToZero(LinqExpr x) { return x; }
        // Generate expression to swap a and b, using t as a temporary.
        private static LinqExpr Swap(LinqExpr a, LinqExpr b, LinqExpr t)
        {
            return LinqExpr.Block(
                LinqExpr.Assign(t, a),
                LinqExpr.Assign(a, b),
                LinqExpr.Assign(b, t));
        }
    }
}
