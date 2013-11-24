using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Reflection.Emit;
using ComputerAlgebra;
using ComputerAlgebra.LinqCompiler;
using Util;
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
            Update(Solution, Oversample);

            Reset();
        }

        public override void Update(TransientSolution Solution, int Oversample)
        {
            base.Update(Solution, Oversample);

            foreach (Expression i in Solution.Solutions.SelectMany(i => i.Unknowns))
            {
                // If any system depends on the previous value of i, we need a global variable for it.
                if (Solution.Solutions.Any(j => j.DependsOn(i.Evaluate(t, t0))))
                    AddGlobal(i.Evaluate(t, t0));
            }
            // Also need globals for any Newton's method unknowns.
            foreach (Expression i in Solution.Solutions.OfType<NewtonIteration>().SelectMany(i => i.Unknowns))
                AddGlobal(i.Evaluate(t, t0));
        }

        protected override void Flush()
        {
            base.Flush();
            
            compiled.Clear();
        }

        private void AddGlobal(Expression Name)
        {
            if (!globals.ContainsKey(Name))
                globals.Add(Name, new GlobalExpr<double>(0.0));
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
            Delegate processor = Compile(T, Oversample, Input.Select(i => i.Key), Output.Select(i => i.Key), Arguments.Select(i => i.Key));

            // Build parameter list for the processor.
            object[] parameters = new object[3 + Input.Count() + Output.Count() + Arguments.Count()];
            int p = 0;

            parameters[p++] = N;
            parameters[p++] = (double)n * T;
            parameters[p++] = Iterations;

            foreach (KeyValuePair<Expression, double[]> i in Input)
                parameters[p++] = i.Value;
            foreach (KeyValuePair<Expression, double[]> i in Output)
                parameters[p++] = i.Value;
            foreach (KeyValuePair<Expression, double> i in Arguments)
                parameters[p++] = i.Value;

            try
            {
                processor.DynamicInvoke(parameters);
            }
            catch (TargetInvocationException Ex)
            {
                throw Ex.InnerException;
            }
        }
        
        // Compile and cache delegates for processing various IO configurations for this simulation.
        private Dictionary<int, Delegate> compiled = new Dictionary<int, Delegate>();
        private Delegate Compile(double T, int Oversample, IEnumerable<Expression> Input, IEnumerable<Expression> Output, IEnumerable<Expression> Parameters)
        {
            int hash = OrderedHashCode(
                T.GetHashCode(),
                Oversample.GetHashCode(),
                Input.OrderedHashCode(), 
                Output.OrderedHashCode(), 
                Parameters.OrderedHashCode());

            Delegate d;
            if (compiled.TryGetValue(hash, out d))
                return d;

            Log.WriteLine(MessageType.Info, "Defining sample processing function...");
            Log.WriteLine(MessageType.Verbose, "Inputs = {{ " + Input.UnSplit(", ") + " }}");
            Log.WriteLine(MessageType.Verbose, "Outputs = {{ " + Output.UnSplit(", ") + " }}");
            Log.WriteLine(MessageType.Verbose, "Parameters = {{ " + Parameters.UnSplit(", ") + " }}");
            CodeGen code = DefineProcessFunction(T, Oversample, Input, Output, Parameters);
            Log.WriteLine(MessageType.Info, "Building sample processing function...");
            LinqExprs.LambdaExpression lambda = code.Build();
            Log.WriteLine(MessageType.Info, "Compiling sample processing function...");
            d = lambda.Compile();
            Log.WriteLine(MessageType.Info, "Done.");

            return compiled[hash] = d;
        }
        
        // The resulting lambda processes N samples, using buffers provided for Input and Output:
        //  void Process(int N, double t0, double T, double[] Input0 ..., double[] Output0 ..., double Parameter0 ...)
        //  { ... }
        private CodeGen DefineProcessFunction(double T, int Oversample, IEnumerable<Expression> Input, IEnumerable<Expression> Output, IEnumerable<Expression> Parameters)
        {
            // Map expressions to identifiers in the syntax tree.
            List<KeyValuePair<Expression, LinqExpr>> inputs = new List<KeyValuePair<Expression, LinqExpr>>();
            List<KeyValuePair<Expression, LinqExpr>> outputs = new List<KeyValuePair<Expression, LinqExpr>>();
            
            // Lambda code generator.
            CodeGen code = new CodeGen();

            // Create parameters for the basic simulation info (N, t, Iterations).
            ParamExpr SampleCount = code.Decl<int>(Scope.Parameter, "SampleCount");
            ParamExpr t0 = code.Decl(Scope.Parameter, Simulation.t0);
            ParamExpr Iterations = code.Decl<int>(Scope.Parameter, "Iterations");
            // Create buffer parameters for each input, output.
            foreach (Expression i in Input)
            {
                ParamExpr p = code.Decl<double[]>(Scope.Parameter, i.ToString());
                inputs.Add(new KeyValuePair<Expression, LinqExpr>(i, p));
            }
            foreach (Expression i in Output)
            {
                ParamExpr p = code.Decl<double[]>(Scope.Parameter, i.ToString());
                outputs.Add(new KeyValuePair<Expression, LinqExpr>(i, p));
            }
            // Create constant parameters for simulation parameters.
            foreach (Expression i in Parameters)
                code.Decl(Scope.Parameter, i);

            // Create globals to store previous values of input.
            foreach (Expression i in Input.Distinct())
                if (!globals.ContainsKey(i.Evaluate(t_t0)))
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
            LinqExpr JxF = code.DeclInit<double[][]>("JxF", LinqExpr.NewArrayBounds(typeof(double[]), LinqExpr.Constant(M)));
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
                            
                            LinearCombination[] eqs = S.Equations.ToArray();
                            Expression[] deltas = S.Updates.ToArray();

                            // Start with the initial guesses from the solution.
                            foreach (Arrow i in S.Guesses)
                                code.DeclInit(i.Left, i.Right);
                            
                            // int it = iterations
                            LinqExpr it = code.ReDeclInit<int>("it", Iterations);
                            // do { ... --it } while(it > 0)
                            code.DoWhile((Break) =>
                            {
                                // Initialize the matrix.
                                for (int i = 0; i < eqs.Length; ++i)
                                {
                                    LinqExpr JxFi = code.ReDeclInit<double[]>("JxFi", LinqExpr.ArrayAccess(JxF, LinqExpr.Constant(i)));
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
                                    LinqExpr JxFj = code.ReDeclInit<double[]>("JxFj", LinqExpr.ArrayAccess(JxF, _j));
                                    // int pi = j
                                    LinqExpr pi = code.ReDeclInit<int>("pi", _j);
                                    // double max = |JxF[j][j]|
                                    LinqExpr max = code.ReDeclInit<double>("max", Abs(LinqExpr.ArrayAccess(JxFj, _j)));
                                    
                                    // Find a pivot row for this variable.
                                    for (int i = j + 1; i < eqs.Length; ++i)
                                    {
                                        LinqExpr _i = LinqExpr.Constant(i);

                                        // if(|JxF[i][j]| > max) { pi = i, max = |JxF[i][j]| }
                                        LinqExpr maxj = code.ReDeclInit<double>("maxj", Abs(LinqExpr.ArrayAccess(LinqExpr.ArrayAccess(JxF, _i), _j)));
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
                                    LinqExpr p = code.ReDeclInit<double>("p", LinqExpr.ArrayAccess(JxFj, _j));
                                    for (int i = j + 1; i < eqs.Length; ++i)
                                    {
                                        LinqExpr _i = LinqExpr.Constant(i);
                                        LinqExpr JxFi = code.ReDeclInit<double[]>("JxFi", LinqExpr.ArrayAccess(JxF, _i));

                                        // s = JxF[i][j] / p
                                        LinqExpr s = code.ReDeclInit<double>("scale", LinqExpr.Divide(LinqExpr.ArrayAccess(JxFi, _j), p));
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
                                    LinqExpr JxFj = code.ReDeclInit<double[]>("JxFj", LinqExpr.ArrayAccess(JxF, _j));

                                    LinqExpr r = LinqExpr.ArrayAccess(JxFj, LinqExpr.Constant(deltas.Length));
                                    for (int ji = j + 1; ji < deltas.Length; ++ji)
                                        r = LinqExpr.Add(r, LinqExpr.Multiply(LinqExpr.ArrayAccess(JxFj, LinqExpr.Constant(ji)), code[deltas[ji]]));
                                    code.DeclInit(deltas[j], LinqExpr.Divide(LinqExpr.Negate(r), LinqExpr.ArrayAccess(JxFj, _j)));
                                }

                                // Compile the pre-solved solutions.
                                if (S.Solved != null)
                                    foreach (Arrow i in S.Solved)
                                        code.DeclInit(i.Left, i.Right);

                                // bool done = true
                                LinqExpr done = code.ReDeclInit("done", true);
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
                        code.Add(LinqExpr.AddAssign(Vo[i], code.Compile(i)));
                    
                    // Vi_t0 = Vi
                    foreach (Expression i in Input.Distinct())
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

            return code;
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
