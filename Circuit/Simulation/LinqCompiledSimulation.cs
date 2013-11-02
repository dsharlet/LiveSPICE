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
        }

        public override void Reset()
        {
            base.Reset();
            foreach (GlobalExpr<double> i in globals.Values)
                i.Value = 0.0;
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
            Log.WriteLine(MessageType.Info, "Inputs = {{ " + Input.UnSplit(", ") + " }}");
            Log.WriteLine(MessageType.Info, "Outputs = {{ " + Output.UnSplit(", ") + " }}");
            Log.WriteLine(MessageType.Info, "Parameters = {{ " + Parameters.UnSplit(", ") + " }}");
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
            Dictionary<Expression, LinqExpr> map = new Dictionary<Expression, LinqExpr>();
            Dictionary<Expression, LinqExpr> inputs = new Dictionary<Expression, LinqExpr>();
            List<KeyValuePair<Expression, LinqExpr>> outputs = new List<KeyValuePair<Expression, LinqExpr>>();
            
            // Lambda definition objects.
            List<ParamExpr> parameters = new List<ParamExpr>();
            List<ParamExpr> locals = new List<ParamExpr>();
            List<LinqExpr> body = new List<LinqExpr>();

            // Create parameters for the basic simulation info (N, t, T, Oversample, Iterations).
            ParamExpr SampleCount = Declare<int>(parameters, "SampleCount");
            ParamExpr t0 = Declare<double>(parameters, map, Simulation.t0);
            // Create buffer parameters for each input, output.
            foreach (Expression i in Input)
                Declare<double[]>(parameters, inputs, i);
            foreach (Expression i in Output)
                Declare<double[]>(parameters, outputs, i);
            // Create constant parameters for simulation parameters.
            foreach (Expression i in Parameters)
                Declare<double>(parameters, map, i);

            // Create globals to store previous values of input.
            foreach (Expression i in Input)
                globals[i.Evaluate(t_t0)] = new GlobalExpr<double>(0.0);

            // Define lambda body.

            // int Zero = 0
            LinqExpr Zero = LinqExpr.Constant(0);

            // double t = t0
            ParamExpr t = Declare<double>(locals, map, Simulation.t);
            body.Add(LinqExpr.Assign(t, t0));

            // double h = T / Oversample
            LinqExpr h = LinqExpr.Constant((double)T / (double)Oversample);

            // Load the globals to local variables and add them to the map.
            foreach (KeyValuePair<Expression, GlobalExpr<double>> i in globals)
                body.Add(LinqExpr.Assign(Declare<double>(locals, map, i.Key), i.Value));

            foreach (KeyValuePair<Expression, LinqExpr> i in inputs)
                body.Add(LinqExpr.Assign(Declare<double>(locals, map, i.Key), map[i.Key.Evaluate(t_t0)]));

            // Create arrays for the newton's method systems.
            int M = Solution.Solutions.OfType<NewtonIteration>().Max(i => i.Equations.Count(), 0);
            int N = Solution.Solutions.OfType<NewtonIteration>().Max(i => i.Updates.Count(), 0) + 1;
            LinqExpr JxF = Declare(locals, body, "JxF", LinqExpr.NewArrayBounds(typeof(double[]), LinqExpr.Constant(M)));
            for (int j = 0; j < M; ++j)
                body.Add(LinqExpr.Assign(LinqExpr.ArrayAccess(JxF, LinqExpr.Constant(j)), LinqExpr.NewArrayBounds(typeof(double), LinqExpr.Constant(N))));

            // for (int n = 0; n < SampleCount; ++n)
            ParamExpr n = Declare<int>(locals, "n");
            For(body,
                () => body.Add(LinqExpr.Assign(n, Zero)),
                LinqExpr.LessThan(n, SampleCount),
                () => body.Add(LinqExpr.PreIncrementAssign(n)),
                () =>
            {
                // Prepare input samples for oversampling interpolation.
                Dictionary<Expression, LinqExpr> dVi = new Dictionary<Expression, LinqExpr>();
                foreach (Expression i in Input)
                {
                    LinqExpr Va = map[i];
                    LinqExpr Vb = LinqExpr.ArrayAccess(inputs[i], n);

                    // dVi = (Vb - Va) / Oversample
                    body.Add(LinqExpr.Assign(
                        Declare<double>(locals, dVi, i, "d" + i.ToString().Replace("[t]", "")),
                        LinqExpr.Multiply(LinqExpr.Subtract(Vb, Va), LinqExpr.Constant(1.0 / (double)Oversample))));
                }

                // Prepare output sample accumulators for low pass filtering.
                Dictionary<Expression, LinqExpr> Vo = new Dictionary<Expression, LinqExpr>();
                foreach (Expression i in Output.Distinct())
                    body.Add(LinqExpr.Assign(
                        Declare<double>(locals, Vo, i, i.ToString().Replace("[t]", "")),
                        LinqExpr.Constant(0.0)));
                
                // int ov = Oversample; 
                // do { -- ov; } while(ov > 0)
                ParamExpr ov = Declare<int>(locals, "ov");
                body.Add(LinqExpr.Assign(ov, LinqExpr.Constant(Oversample)));
                DoWhile(body, () =>
                {
                    // t += h
                    body.Add(LinqExpr.AddAssign(t, h));

                    // Interpolate the input samples.
                    foreach (Expression i in Input)
                        body.Add(LinqExpr.AddAssign(map[i], dVi[i]));

                    // Compile all of the SolutionSets in the solution.
                    foreach (SolutionSet ss in Solution.Solutions)
                    {
                        if (ss is LinearSolutions)
                        {
                            // Linear solutions are easy.
                            LinearSolutions S = (LinearSolutions)ss;
                            foreach (Arrow i in S.Solutions)
                                body.Add(LinqExpr.Assign(Declare<double>(locals, map, i.Left), i.Right.Compile(map)));
                        }
                        else if (ss is NewtonIteration)
                        {
                            NewtonIteration S = (NewtonIteration)ss;
                            
                            LinearCombination[] eqs = S.Equations.ToArray();
                            Expression[] deltas = S.Updates.ToArray();

                            // Start with the initial guesses from the solution.
                            foreach (Arrow i in S.Guesses)
                                body.Add(LinqExpr.Assign(Declare<double>(locals, map, i.Left), i.Right.Compile(map)));
                            
                            // int it = iterations
                            ParamExpr it = Redeclare(locals, body, "it", LinqExpr.Constant(Iterations));
                            // do { ... --it } while(it > 0)
                            DoWhile(body, (Break) =>
                            {
                                // Initialize the matrix.
                                for (int i = 0; i < eqs.Length; ++i)
                                {
                                    LinqExpr JxFi = Redeclare(locals, body, "JxFi", LinqExpr.ArrayAccess(JxF, LinqExpr.Constant(i)));
                                    for (int x = 0; x < deltas.Length; ++x)
                                        body.Add(LinqExpr.Assign(
                                            LinqExpr.ArrayAccess(JxFi, LinqExpr.Constant(x)),
                                            eqs[i][deltas[x]].Compile(map)));
                                    body.Add(LinqExpr.Assign(
                                        LinqExpr.ArrayAccess(JxFi, LinqExpr.Constant(deltas.Length)),
                                        eqs[i][Constant.One].Compile(map)));
                                }
                                                                
                                // Gaussian elimination on this turd.

                                // For each variable in the system...
                                for (int j = 0; j < deltas.Length; ++j)
                                {
                                    LinqExpr _j = LinqExpr.Constant(j);
                                    LinqExpr JxFj = Redeclare(locals, body, "JxFj", LinqExpr.ArrayAccess(JxF, _j));
                                    // int pi = j
                                    LinqExpr pi = Redeclare(locals, body, "pi", _j);
                                    // double max = |JxF[j][j]|
                                    LinqExpr max = Redeclare(locals, body, "max", Abs(LinqExpr.ArrayAccess(JxFj, _j)));
                                    
                                    // Find a pivot row for this variable.
                                    for (int i = j + 1; i < eqs.Length; ++i)
                                    {
                                        LinqExpr _i = LinqExpr.Constant(i);

                                        // if(|JxF[i][j]| > max) { pi = i, max = |JxF[i][j]| }
                                        LinqExpr maxj = Redeclare(locals, body, "maxj", Abs(LinqExpr.ArrayAccess(LinqExpr.ArrayAccess(JxF, _i), _j)));
                                        body.Add(LinqExpr.IfThen(
                                            LinqExpr.GreaterThan(maxj, max),
                                            LinqExpr.Block(
                                                LinqExpr.Assign(pi, _i), 
                                                LinqExpr.Assign(max, maxj))));
                                    }
                                    
                                    // (Maybe) swap the pivot row with the current row.
                                    LinqExpr JxFpi = Redeclare<double[]>(locals, "JxFpi");
                                    body.Add(LinqExpr.IfThen(
                                        LinqExpr.NotEqual(_j, pi), LinqExpr.Block(
                                            new[] { LinqExpr.Assign(JxFpi, LinqExpr.ArrayAccess(JxF, pi)) }.Concat(
                                            Enumerable.Range(j, deltas.Length + 1 - j).Select(x => Swap(
                                                LinqExpr.ArrayAccess(JxFj, LinqExpr.Constant(x)),
                                                LinqExpr.ArrayAccess(JxFpi, LinqExpr.Constant(x)),
                                                Redeclare<double>(locals, "swap")))))));

                                    //// It's hard to believe this swap isn't faster than the above...
                                    //body.Add(LinqExpr.IfThen(LinqExpr.NotEqual(_j, pi), LinqExpr.Block(
                                    //    Swap(LinqExpr.ArrayAccess(JxF, _j), LinqExpr.ArrayAccess(JxF, pi), Redeclare<double[]>(locals, "temp")),
                                    //    LinqExpr.Assign(JxFj, LinqExpr.ArrayAccess(JxF, _j)))));
                                    
                                    // Eliminate the rows after the pivot.
                                    LinqExpr p = Redeclare(locals, body, "p", LinqExpr.ArrayAccess(JxFj, _j));
                                    for (int i = j + 1; i < eqs.Length; ++i)
                                    {
                                        LinqExpr _i = LinqExpr.Constant(i);
                                        LinqExpr JxFi = Redeclare(locals, body, "JxFi", LinqExpr.ArrayAccess(JxF, _i));

                                        // s = JxF[i][j] / p
                                        LinqExpr s = Redeclare(locals, body, "scale", LinqExpr.Divide(LinqExpr.ArrayAccess(JxFi, _j), p));
                                        // JxF[i] -= JxF[j] * s
                                        for (int ji = j + 1; ji < deltas.Length + 1; ++ji)
                                        {
                                            LinqExpr _ji = LinqExpr.Constant(ji);
                                            body.Add(LinqExpr.SubtractAssign(
                                                LinqExpr.ArrayAccess(JxFi, _ji),
                                                LinqExpr.Multiply(LinqExpr.ArrayAccess(JxFj, _ji), s)));
                                        }
                                    }
                                }

                                // JxF is now upper triangular, solve it.
                                for (int j = deltas.Length - 1; j >= 0; --j)
                                {
                                    LinqExpr _j = LinqExpr.Constant(j);
                                    LinqExpr JxFj = Redeclare(locals, body, "JxFj", LinqExpr.ArrayAccess(JxF, _j));

                                    LinqExpr r = LinqExpr.ArrayAccess(JxFj, LinqExpr.Constant(deltas.Length));
                                    for (int ji = j + 1; ji < deltas.Length; ++ji)
                                        r = LinqExpr.Add(r, LinqExpr.Multiply(LinqExpr.ArrayAccess(JxFj, LinqExpr.Constant(ji)), map[deltas[ji]]));
                                    body.Add(LinqExpr.Assign(
                                        Declare<double>(locals, map, deltas[j]),
                                        LinqExpr.Divide(LinqExpr.Negate(r), LinqExpr.ArrayAccess(JxFj, _j))));
                                }

                                // Compile the pre-solved solutions.
                                if (S.Solved != null)
                                    foreach (Arrow i in S.Solved)
                                        body.Add(LinqExpr.Assign(
                                            Declare<double>(locals, map, i.Left), 
                                            i.Right.Compile(map)));

                                // bool done = true
                                LinqExpr done = Redeclare(locals, body, "done", LinqExpr.Constant(true));
                                foreach (Expression i in S.Unknowns)
                                {
                                    LinqExpr v = map[i];
                                    LinqExpr dv = map[NewtonIteration.Delta(i)];

                                    // done &= (|dv| < |v|*epsilon)
                                    body.Add(LinqExpr.AndAssign(done, LinqExpr.LessThan(LinqExpr.Multiply(Abs(dv), LinqExpr.Constant(1e4)), Abs(v))));
                                    // v += dv
                                    body.Add(LinqExpr.AddAssign(v, dv));
                                }
                                // if (done) break
                                body.Add(LinqExpr.IfThen(done, Break));
                                
                                // --it;
                                body.Add(LinqExpr.PreDecrementAssign(it));
                            }, LinqExpr.GreaterThan(it, Zero));
                            
                            //// bool failed = false
                            //LinqExpr failed = Declare(locals, body, "failed", LinqExpr.Constant(false));
                            //for (int i = 0; i < eqs.Length; ++i)
                            //    // failed |= |JxFi| > epsilon
                            //    body.Add(LinqExpr.OrAssign(failed, LinqExpr.GreaterThan(Abs(eqs[i].ToExpression().Compile(map)), LinqExpr.Constant(1e-3))));

                            //body.Add(LinqExpr.IfThen(failed, ThrowSimulationDiverged(n)));
                        }
                    }

                    // Update the previous timestep variables.
                    foreach (SolutionSet S in Solution.Solutions)
                        foreach (Expression i in S.Unknowns.Where(i => globals.Keys.Contains(i.Evaluate(t_t0))))
                            body.Add(LinqExpr.Assign(map[i.Evaluate(t_t0)], map[i]));

                    // t0 = t
                    body.Add(LinqExpr.Assign(t0, t));

                    // Vo += i
                    foreach (Expression i in Output.Distinct())
                        body.Add(LinqExpr.AddAssign(Vo[i], CompileOrWarn(i, map)));
                    
                    // Vi_t0 = Vi
                    foreach (Expression i in Input)
                        body.Add(LinqExpr.Assign(map[i.Evaluate(t_t0)], map[i]));

                    // --ov;
                    body.Add(LinqExpr.PreDecrementAssign(ov));
                }, LinqExpr.GreaterThan(ov, Zero));
                
                // Output[i][n] = Vo / Oversample
                foreach (KeyValuePair<Expression, LinqExpr> i in outputs)
                    body.Add(LinqExpr.Assign(LinqExpr.ArrayAccess(i.Value, n), LinqExpr.Multiply(Vo[i.Key], LinqExpr.Constant(1.0 / (double)Oversample))));

                // Every 256 samples, check for divergence.
                body.Add(LinqExpr.IfThen(LinqExpr.Equal(LinqExpr.And(n, LinqExpr.Constant(0xFF)), Zero),
                    LinqExpr.Block(Vo.Select(i => LinqExpr.IfThenElse(IsNotReal(i.Value), 
                        ThrowSimulationDiverged(n), 
                        LinqExpr.Assign(i.Value, RoundDenormToZero(i.Value)))))));
            });

            // Copy the global state variables back to the globals.
            foreach (KeyValuePair<Expression, GlobalExpr<double>> i in globals)
                body.Add(LinqExpr.Assign(i.Value, map[i.Key]));

            // Put it all together.
            return LinqExpr.Lambda(LinqExpr.Block(locals, body), parameters);
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
                Log.WriteLine(MessageType.Warning, "Error compiling output expression '{0}': {1}", x.ToString(), ex.Message);
                return LinqExpr.Constant(0.0);
            }
        }

        // Returns a throw SimulationDiverged expression at At.
        private LinqExpr ThrowSimulationDiverged(LinqExpr At)
        {
            return LinqExpr.Throw(LinqExpr.New(typeof(SimulationDiverged).GetConstructor(new Type[] { At.Type }), At));
        }

        // Generate a for loop given the header and body expressions.
        private static void For(
            IList<LinqExpr> Target,
            Action Init,
            LinqExpr Condition,
            Action Step,
            Action<LinqExpr, LinqExpr> Body)
        {
            string name = Target.Count.ToString();
            LinqExprs.LabelTarget begin = LinqExpr.Label("for_" + name + "_begin");
            LinqExprs.LabelTarget end = LinqExpr.Label("for_" + name + "_end");

            // Generate the init code.
            Init();

            // Check the condition, exit if necessary.
            Target.Add(LinqExpr.Label(begin));
            Target.Add(LinqExpr.IfThen(LinqExpr.Not(Condition), LinqExpr.Goto(end)));

            // Generate the body code.
            Body(LinqExpr.Goto(end), LinqExpr.Goto(begin));

            // Generate the step code.
            Step();
            Target.Add(LinqExpr.Goto(begin));

            // Exit point.
            Target.Add(LinqExpr.Label(end));
        }

        // Generate a for loop given the header and body expressions.
        private static void For(
            IList<LinqExpr> Target,
            Action Init,
            LinqExpr Condition,
            Action Step,
            Action<LinqExpr> Body)
        {
            For(Target, Init, Condition, Step, (end, y) => Body(end));
        }

        // Generate a for loop given the header and body expressions.
        private static void For(
            IList<LinqExpr> Target,
            Action Init,
            LinqExpr Condition,
            Action Step,
            Action Body)
        {
            For(Target, Init, Condition, Step, (x, y) => Body());
        }

        // Generate a while loop given the condition and body expressions.
        private static void While(
            IList<LinqExpr> Target,
            LinqExpr Condition,
            Action<LinqExpr, LinqExpr> Body)
        {
            string name = (Target.Count + 1).ToString();
            LinqExprs.LabelTarget begin = LinqExpr.Label("while_" + name + "_begin");
            LinqExprs.LabelTarget end = LinqExpr.Label("while_" + name + "_end");

            // Check the condition, exit if necessary.
            Target.Add(LinqExpr.Label(begin));
            Target.Add(LinqExpr.IfThen(LinqExpr.Not(Condition), LinqExpr.Goto(end)));

            // Generate the body code.
            Body(LinqExpr.Goto(end), LinqExpr.Goto(begin));

            // Loop.
            Target.Add(LinqExpr.Goto(begin));

            // Exit label.
            Target.Add(LinqExpr.Label(end));
        }

        // Generate a while loop given the condition and body expressions.
        private static void While(
            IList<LinqExpr> Target,
            LinqExpr Condition,
            Action<LinqExpr> Body)
        {
            While(Target, Condition, (end, y) => Body(end));
        }

        // Generate a while loop given the condition and body expressions.
        private static void While(
            IList<LinqExpr> Target,
            LinqExpr Condition,
            Action Body)
        {
            While(Target, Condition, (x, y) => Body());
        }

        // Generate a do-while loop given the condition and body expressions.
        private static void DoWhile(
            IList<LinqExpr> Target,
            Action<LinqExpr, LinqExpr> Body,
            LinqExpr Condition)
        {
            string name = (Target.Count + 1).ToString();
            LinqExprs.LabelTarget begin = LinqExpr.Label("do_" + name + "_begin");
            LinqExprs.LabelTarget end = LinqExpr.Label("do_" + name + "_end");

            // Check the condition, exit if necessary.
            Target.Add(LinqExpr.Label(begin));

            // Generate the body code.
            Body(LinqExpr.Goto(end), LinqExpr.Goto(begin));

            // Loop.
            Target.Add(LinqExpr.IfThen(Condition, LinqExpr.Goto(begin)));

            // Exit label.
            Target.Add(LinqExpr.Label(end));
        }

        // Generate a do-while loop given the condition and body expressions.
        private static void DoWhile(
            IList<LinqExpr> Target,
            Action<LinqExpr> Body,
            LinqExpr Condition)
        {
            DoWhile(Target, (end, y) => Body(end), Condition);
        }

        // Generate a do-while loop given the condition and body expressions.
        private static void DoWhile(
            IList<LinqExpr> Target,
            Action Body,
            LinqExpr Condition)
        {
            DoWhile(Target, (x, y) => Body(), Condition);
        }
                
        private static ParamExpr Declare<T>(IList<ParamExpr> Scope, ICollection<KeyValuePair<Expression, LinqExpr>> Map, Expression Expr, string Name)
        {
            ParamExpr p = LinqExpr.Parameter(typeof(T), Name);
            Scope.Add(p);
            if (Map != null)
                Map.Add(new KeyValuePair<Expression, LinqExpr>(Expr, p));
            return p;
        }

        private static ParamExpr Declare<T>(IList<ParamExpr> Scope, ICollection<KeyValuePair<Expression, LinqExpr>> Map, Expression Expr)
        {
            return Declare<T>(Scope, Map, Expr, Expr.ToString());
        }

        private static ParamExpr Redeclare<T>(IList<ParamExpr> Scope, IDictionary<Expression, LinqExpr> Map, Expression Expr)
        {
            LinqExpr decl;
            if (Map.TryGetValue(Expr, out decl))
                return (ParamExpr)decl;
            else
                return Declare<T>(Scope, Map, Expr, Expr.ToString());
        }

        private static ParamExpr Redeclare<T>(IList<ParamExpr> Scope, IList<LinqExpr> Target, IDictionary<Expression, LinqExpr> Map, Expression Expr, LinqExpr Init)
        {
            LinqExpr decl;
            if (!Map.TryGetValue(Expr, out decl))
                decl = Declare<T>(Scope, Map, Expr, Expr.ToString());
            Target.Add(LinqExpr.Assign(decl, Init));
            return (ParamExpr)decl;
        }

        private static ParamExpr Declare<T>(IList<ParamExpr> Scope, string Name)
        {
            return Declare<T>(Scope, null, null, Name);
        }

        private static ParamExpr Redeclare<T>(IList<ParamExpr> Scope, string Name)
        {
            ParamExpr def = Scope.FirstOrDefault(i => i.Name == Name && i.Type == typeof(T));
            if (def != null)
                return def;
            else
                return Declare<T>(Scope, null, null, Name);
        }

        private static ParamExpr Declare(IList<ParamExpr> Scope, IList<LinqExpr> Target, string Name, LinqExpr Init)
        {
            ParamExpr p = LinqExpr.Parameter(Init.Type, Name);
            Scope.Add(p);
            Target.Add(LinqExpr.Assign(p, Init));
            return p;
        }

        private static ParamExpr Redeclare(IList<ParamExpr> Scope, IList<LinqExpr> Target, string Name, LinqExpr Init)
        {
            ParamExpr p = Scope.FirstOrDefault(i => i.Name == Name && i.Type == Init.Type);
            if (p == null)
            {
                p = LinqExpr.Parameter(Init.Type, Name);
                Scope.Add(p);
            }
            Target.Add(LinqExpr.Assign(p, Init));
            return p;
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
