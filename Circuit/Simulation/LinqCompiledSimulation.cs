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
        /// <param name="Transient">Transient solution to run.</param>
        /// <param name="Log">Log for simulation output.</param>
        public LinqCompiledSimulation(TransientSolution Transient, int Oversample, ILog Log) : base(Transient, Oversample, Log)
        {
            foreach (Expression i in Transient.Solutions.SelectMany(i => i.Unknowns))
            {
                // If any system depends on the previous value of i, we need a global variable for it.
                if (Transient.Solutions.Any(j => j.DependsOn(i.Evaluate(t, t0))))
                    globals[i.Evaluate(t, t0)] = new GlobalExpr<double>(0.0);
            }

            if (Transient.Linearization != null)
                foreach (Arrow i in Transient.Linearization)
                    globals[i.Left] = new GlobalExpr<double>(0.0);
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
            IEnumerable<KeyValuePair<Expression,double[]>> Input, 
            IEnumerable<KeyValuePair<Expression,double[]>> Output, 
            IEnumerable<Arrow> Arguments, 
            int Oversample, int Iterations)
        {
            Delegate processor = Compile(Input.Select(i => i.Key), Output.Select(i => i.Key), Arguments.Select(i => i.Left));

            // Build parameter list for the processor.
            List<object> parameters = new List<object>(3 + Input.Count() + Output.Count() + Arguments.Count());
            parameters.Add(N);
            parameters.Add((double)n * T);
            parameters.Add(T);
            parameters.Add(Oversample);
            parameters.Add(Iterations);
            foreach (KeyValuePair<Expression, double[]> i in Input)
                parameters.Add(i.Value);
            foreach (KeyValuePair<Expression, double[]> i in Output)
                parameters.Add(i.Value);
            if (Arguments != null)
                foreach (Arrow i in Arguments)
                    parameters.Add((double)i.Right);

            processor.DynamicInvoke(parameters.ToArray());
        }
        
        // Compile and cache delegates for processing various IO configurations for this simulation.
        private Dictionary<long, Delegate> compiled = new Dictionary<long, Delegate>();
        private Delegate Compile(IEnumerable<Expression> Input, IEnumerable<Expression> Output, IEnumerable<Expression> Parameters)
        {
            long hash = Input.OrderedHashCode();
            hash = hash * 33 + Output.OrderedHashCode();
            hash = hash * 33 + Parameters.OrderedHashCode();

            Delegate d;
            if (compiled.TryGetValue(hash, out d))
                return d;

            Stopwatch time = new Stopwatch();
            time.Start();

            Log.WriteLine(MessageType.Info, "[{0} ms] Defining sample processing function...", time.ElapsedMilliseconds);
            Log.WriteLine(MessageType.Info, "Inputs = {{ " + Input.UnSplit(", ") + " }}");
            Log.WriteLine(MessageType.Info, "Outputs = {{ " + Output.UnSplit(", ") + " }}");
            Log.WriteLine(MessageType.Info, "Parameters = {{ " + Parameters.UnSplit(", ") + " }}");
            LinqExprs.LambdaExpression lambda = DefineProcessFunction(Input, Output, Parameters);
            Log.WriteLine(MessageType.Info, "[{0} ms] Compiling sample processing function...", time.ElapsedMilliseconds);
            d = lambda.Compile();
            Log.WriteLine(MessageType.Info, "[{0} ms] Done.", time.ElapsedMilliseconds);

            return compiled[hash] = d;
        }
        
        // The resulting lambda processes N samples, using buffers provided for Input and Output:
        //  void Process(int N, double t0, double T, double[] Input0 ..., double[] Output0 ..., double Parameter0 ...)
        //  { ... }
        private LinqExprs.LambdaExpression DefineProcessFunction(IEnumerable<Expression> Input, IEnumerable<Expression> Output, IEnumerable<Expression> Parameters)
        {
            // Map expressions to identifiers in the syntax tree.
            Dictionary<Expression, LinqExpr> map = new Dictionary<Expression, LinqExpr>();
            Dictionary<Expression, LinqExpr> buffers = new Dictionary<Expression, LinqExpr>();
            
            // Lambda definition objects.
            List<ParamExpr> parameters = new List<ParamExpr>();
            List<ParamExpr> locals = new List<ParamExpr>();
            List<LinqExpr> body = new List<LinqExpr>();

            // Create parameters for the basic simulation info (N, t, T, Oversample, Iterations).
            ParamExpr SampleCount = Declare<int>(parameters, "SampleCount");
            ParamExpr t0 = Declare<double>(parameters, map, Simulation.t0);
            ParamExpr T = Declare<double>(parameters, map, Component.T);
            ParamExpr Oversample = Declare<int>(parameters, "Oversample");
            ParamExpr Iterations = Declare<int>(parameters, "Iterations");
            // Create buffer parameters for each input, output.
            foreach (Expression i in Input.Concat(Output))
                Declare<double[]>(parameters, buffers, i);
            // Create constant parameters for simulation parameters.
            foreach (Expression i in Parameters)
                Declare<double>(parameters, map, i);

            // Create globals to store previous values of input.
            foreach (Expression i in Input)
                globals[i.Evaluate(t_t0)] = new GlobalExpr<double>(0.0);

            // Define lambda body.

            // double t = t0
            ParamExpr t = Declare<double>(locals, map, Simulation.t);
            body.Add(LinqExpr.Assign(t, t0));

            // double h = T / Oversample
            ParamExpr h = Declare<double>(locals, "h");
            body.Add(LinqExpr.Assign(h, LinqExpr.Divide(T, LinqExpr.Convert(Oversample, typeof(double)))));

            // double invOversample = 1 / (double)Oversample
            ParamExpr invOversample = Declare<double>(locals, "invOversample");
            body.Add(LinqExpr.Assign(invOversample, Reciprocal(LinqExpr.Convert(Oversample, typeof(double)))));

            // Load the globals to local variables and add them to the map.
            foreach (KeyValuePair<Expression, GlobalExpr<double>> i in globals)
                body.Add(LinqExpr.Assign(Declare<double>(locals, map, i.Key), i.Value));

            // Trivial timestep expressions that are not a function of the input or t can be set once here (outside the sample loop).
            // This might not be necessary if you trust the .Net expression compiler to lift this invariant code out of the loop.
            //foreach (Arrow i in Transient.Trivial.Where(i => !i.Right.DependsOn(Input) && !i.Right.DependsOn(Component.t)))
            //    body.Add(LinqExpression.Assign(Declare<double>(locals, map, i.Left), i.Right.Compile(map)));

            // for (int n = 0; n < SampleCount; ++n)
            ParamExpr n = Declare<int>(locals, "n");
            For(body,
                () => body.Add(LinqExpr.Assign(n, LinqExpr.Constant(0))),
                LinqExpr.LessThan(n, SampleCount),
                () => body.Add(LinqExpr.PreIncrementAssign(n)),
                () =>
            {
                // Prepare input samples for oversampling interpolation.
                Dictionary<Expression, LinqExpr> dVi = new Dictionary<Expression, LinqExpr>();
                foreach (Expression i in Input)
                {
                    LinqExpr Va = map[i.Evaluate(t_t0)];
                    LinqExpr Vb = LinqExpr.MakeIndex(buffers[i], buffers[i].Type.GetProperty("Item"), new LinqExpr[] { n });

                    // double Vi = Va
                    body.Add(LinqExpr.Assign(Declare<double>(locals, map, i, i.ToString()), Va));

                    // dVi = (Vb - Vi) / Oversample
                    body.Add(LinqExpr.Assign(
                        Declare<double>(locals, dVi, i, "d" + i.ToString().Replace("[t]", "")),
                        LinqExpr.Multiply(LinqExpr.Subtract(Vb, Va), invOversample)));

                    // Va = Vb
                    body.Add(LinqExpr.Assign(Va, Vb));
                }

                // Prepare output sample accumulators for low pass filtering.
                Dictionary<Expression, LinqExpr> Vo = new Dictionary<Expression, LinqExpr>();
                foreach (Expression i in Output)
                    body.Add(LinqExpr.Assign(
                        Declare<double>(locals, Vo, i, i.ToString().Replace("[t]", "")),
                        LinqExpr.Constant(0.0)));

                // Create arrays for the newton's method systems.
                Dictionary<NewtonRhapsonIteration, LinqExpr> JxFs = new Dictionary<NewtonRhapsonIteration,LinqExpr>();
                foreach (NewtonRhapsonIteration i in Transient.Solutions.OfType<NewtonRhapsonIteration>())
                    JxFs[i] = Declare(locals, body, "JxF" + JxFs.Count.ToString(), 
                        LinqExpr.NewArrayBounds(typeof(double), LinqExpr.Constant(i.Equations.Count()), LinqExpr.Constant(i.Updates.Count() + 1)));

                // int ov = Oversample; 
                // do { -- ov; } while(ov > 0)
                ParamExpr ov = Declare<int>(locals, "ov");
                body.Add(LinqExpr.Assign(ov, Oversample));
                DoWhile(body, () =>
                {
                    // t += h
                    body.Add(LinqExpr.AddAssign(t, h));

                    // Interpolate the input samples.
                    foreach (Expression i in Input)
                        body.Add(LinqExpr.AddAssign(map[i], dVi[i]));

                    // Compile all of the SolutionSets in the solution.
                    foreach (SolutionSet ss in Transient.Solutions)
                    {
                        if (ss is LinearSolutions)
                        {
                            LinearSolutions S = (LinearSolutions)ss;
                            // Linear solutions are easy.
                            foreach (Arrow i in S.Solutions)
                                body.Add(LinqExpr.Assign(Redeclare<double>(locals, map, i.Left), i.Right.Compile(map)));
                            // Update the old variables.
                            foreach (Expression i in S.Unknowns.Where(i => globals.Keys.Contains(i.Evaluate(t_t0))))
                                body.Add(LinqExpr.Assign(map[i.Evaluate(t_t0)], map[i]));
                        }
                        else if (ss is NewtonRhapsonIteration)
                        {
                            NewtonRhapsonIteration S = (NewtonRhapsonIteration)ss;
                            LinqExpr JxF = JxFs[S];
                            
                            LinearCombination[] eqs = S.Equations.ToArray();
                            Expression[] vars = S.Updates.ToArray();

                            // Set initial guesses to the previous values.
                            // int it
                            ParamExpr it = Redeclare<int>(locals, "it");

                            // it = Oversample
                            // do { ... --it } while(it > 0)
                            body.Add(LinqExpr.Assign(it, Iterations));
                            DoWhile(body, (exit) =>
                            {
                                // Build the system.                    

                                // Initialize the matrix.
                                for (int i = 0; i < eqs.Length; ++i)
                                {
                                    for (int x = 0; x < vars.Length; ++x)
                                        body.Add(LinqExpr.Assign(
                                            LinqExpr.ArrayAccess(JxF, LinqExpr.Constant(i), LinqExpr.Constant(x)),
                                            eqs[i][vars[x]].Compile(map)));
                                    body.Add(LinqExpr.Assign(
                                        LinqExpr.ArrayAccess(JxF, LinqExpr.Constant(i), LinqExpr.Constant(vars.Length)),
                                        eqs[i][Constant.One].Compile(map)));
                                }
                                                                
                                // Gaussian elimination on this turd.

                                // For each variable in the system...
                                LinqExpr j = Redeclare<int>(locals, "j");
                                For(body,
                                    () => body.Add(LinqExpr.Assign(j, LinqExpr.Constant(0))),
                                    LinqExpr.LessThan(j, LinqExpr.Constant(vars.Length)),
                                    () => body.Add(LinqExpr.PreIncrementAssign(j)),
                                    () =>
                                {
                                    LinqExpr pivot = Redeclare(locals, body, "pivot", j);
                                    
                                    // Find a pivot row for this variable.
                                    LinqExpr i = Redeclare<int>(locals, "i");
                                    For(body,
                                        () => body.Add(LinqExpr.Assign(i, LinqExpr.Increment(j))),
                                        LinqExpr.LessThan(i, LinqExpr.Constant(eqs.Length)),
                                        () => body.Add(LinqExpr.PreIncrementAssign(i)),
                                        () =>
                                    {
                                        // If this row contains the max pivot column value, store it as the max.
                                        LinqExpr maxj = Redeclare(locals, body, "maxj", Abs(LinqExpr.ArrayAccess(JxF, i, j)));
                                        body.Add(LinqExpr.IfThen(
                                            LinqExpr.GreaterThan(maxj, Abs(LinqExpr.ArrayAccess(JxF, pivot, j))),
                                            LinqExpr.Assign(pivot, i)));
                                    });
                                    
                                    // (Maybe) swap the pivot row with the current row.
                                    LinqExpr temp = Redeclare<double>(locals, "temp");
                                    body.Add(LinqExpr.IfThen(
                                        LinqExpr.NotEqual(j, pivot),
                                        LinqExpr.Block(Enumerable.Range(0, vars.Length + 1).Select(x => LinqExpr.Block(
                                            LinqExpr.Assign(temp, LinqExpr.ArrayAccess(JxF, j, LinqExpr.Constant(x))),
                                            LinqExpr.Assign(LinqExpr.ArrayAccess(JxF, j, LinqExpr.Constant(x)), LinqExpr.ArrayAccess(JxF, pivot, LinqExpr.Constant(x))),
                                            LinqExpr.Assign(LinqExpr.ArrayAccess(JxF, pivot, LinqExpr.Constant(x)), temp))))));

                                    LinqExpr p = Redeclare(locals, body, "p", LinqExpr.ArrayAccess(JxF, j, j));

                                    // Eliminate the rows after the pivot.
                                    For(body,
                                        () => body.Add(LinqExpr.Assign(i, LinqExpr.Increment(j))),
                                        LinqExpr.LessThan(i, LinqExpr.Constant(eqs.Length)),
                                        () => body.Add(LinqExpr.PreIncrementAssign(i)),
                                        () =>
                                    {
                                        LinqExpr s = Redeclare(locals, body, "scale", LinqExpr.Divide(LinqExpr.ArrayAccess(JxF, i, j), p));

                                        LinqExpr jj = Redeclare(locals, body, "jj", j);
                                        For(body,
                                            () => { },
                                            LinqExpr.LessThan(jj, LinqExpr.Constant(vars.Length + 1)),
                                            () => body.Add(LinqExpr.PreIncrementAssign(jj)),
                                            () => body.Add(LinqExpr.SubtractAssign(LinqExpr.ArrayAccess(JxF, i, jj), LinqExpr.Multiply(LinqExpr.ArrayAccess(JxF, j, jj), s))));
                                    });
                                });

                                // JxF is now upper triangular, solve.
                                for (int v = vars.Length - 1; v >= 0; --v)
                                {
                                    LinqExpr r = LinqExpr.ArrayAccess(JxF, LinqExpr.Constant(v), LinqExpr.Constant(vars.Length));
                                    for (int vj = v + 1; vj < vars.Length; ++vj)
                                        r = LinqExpr.Add(r, LinqExpr.Multiply(
                                            LinqExpr.ArrayAccess(JxF, LinqExpr.Constant(v), LinqExpr.Constant(vj)),
                                            map[vars[vj]]));
                                    r = LinqExpr.Negate(r);
                                    body.Add(LinqExpr.Assign(
                                        Redeclare<double>(locals, map, vars[v]),
                                        LinqExpr.Divide(r, LinqExpr.ArrayAccess(JxF, LinqExpr.Constant(v), LinqExpr.Constant(v)))));
                                }
                                
                                // Compile the pre-solved solutions.
                                if (S.Solved != null)
                                    foreach (Arrow i in S.Solved)
                                        body.Add(LinqExpr.Assign(map[i.Left], i.Right.Compile(map)));

                                // Update the old variables.
                                foreach (Expression i in S.Unknowns.Where(i => globals.Keys.Contains(i.Evaluate(t_t0))))
                                    body.Add(LinqExpr.Assign(map[i.Evaluate(t_t0)], map[i]));

                                // TODO: Break early if all the updates are small.
                                //body.Add(LinqExpression.Goto(exit));
                                
                                // --it;
                                body.Add(LinqExpr.PreDecrementAssign(it));
                            }, LinqExpr.GreaterThan(it, LinqExpr.Constant(0)));
                        }
                    }
                                        
                    // Update the linearization.
                    if (Transient.Linearization != null)
                        foreach (Arrow i in Transient.Linearization)
                            body.Add(LinqExpr.Assign(map[i.Left], i.Right.Compile(map)));
                    
                    // t0 = t
                    body.Add(LinqExpr.Assign(t0, t));

                    // Vo += i
                    foreach (Expression i in Output)
                        body.Add(LinqExpr.AddAssign(Vo[i], CompileOrWarn(i, map)));

                    // Vi_t0 = Vi
                    foreach (Expression i in Input)
                        body.Add(LinqExpr.Assign(map[i.Evaluate(t_t0)], map[i]));

                    // --ov;
                    body.Add(LinqExpr.PreDecrementAssign(ov));
                }, LinqExpr.GreaterThan(ov, LinqExpr.Constant(0)));

                // Output[i][n] = Vo / Oversample
                foreach (Expression i in Output)
                {
                    body.Add(LinqExpr.Assign(
                        LinqExpr.MakeIndex(buffers[i], buffers[i].Type.GetProperty("Item"), new LinqExpr[] { n }),
                        LinqExpr.Multiply(Vo[i], invOversample)));
                }
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
            catch (System.Exception ex)
            {
                Log.WriteLine(MessageType.Warning, "Error compiling output expression '{0}': {1}", x.ToString(), ex.Message);
                return LinqExpr.Constant(0.0);
            }
        }

        // Generate a for loop given the header and body expressions.
        private static void For(
            IList<LinqExpr> Target,
            Action Init,
            LinqExpr Condition,
            Action Step,
            Action<LinqExprs.LabelTarget, LinqExprs.LabelTarget> Body)
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
            Body(end, begin);

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
            Action<LinqExprs.LabelTarget> Body)
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
            Action<LinqExprs.LabelTarget, LinqExprs.LabelTarget> Body)
        {
            string name = (Target.Count + 1).ToString();
            LinqExprs.LabelTarget begin = LinqExpr.Label("while_" + name + "_begin");
            LinqExprs.LabelTarget end = LinqExpr.Label("while_" + name + "_end");

            // Check the condition, exit if necessary.
            Target.Add(LinqExpr.Label(begin));
            Target.Add(LinqExpr.IfThen(LinqExpr.Not(Condition), LinqExpr.Goto(end)));

            // Generate the body code.
            Body(end, begin);

            // Loop.
            Target.Add(LinqExpr.Goto(begin));

            // Exit label.
            Target.Add(LinqExpr.Label(end));
        }

        // Generate a while loop given the condition and body expressions.
        private static void While(
            IList<LinqExpr> Target,
            LinqExpr Condition,
            Action<LinqExprs.LabelTarget> Body)
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
            Action<LinqExprs.LabelTarget, LinqExprs.LabelTarget> Body,
            LinqExpr Condition)
        {
            string name = (Target.Count + 1).ToString();
            LinqExprs.LabelTarget begin = LinqExpr.Label("do_" + name + "_begin");
            LinqExprs.LabelTarget end = LinqExpr.Label("do_" + name + "_end");

            // Check the condition, exit if necessary.
            Target.Add(LinqExpr.Label(begin));

            // Generate the body code.
            Body(end, begin);

            // Loop.
            Target.Add(LinqExpr.IfThen(Condition, LinqExpr.Goto(begin)));

            // Exit label.
            Target.Add(LinqExpr.Label(end));
        }

        // Generate a do-while loop given the condition and body expressions.
        private static void DoWhile(
            IList<LinqExpr> Target,
            Action<LinqExprs.LabelTarget> Body,
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


        private static ParamExpr Declare<T>(IList<ParamExpr> Scope, IDictionary<Expression, LinqExpr> Map, Expression Expr, string Name)
        {
            ParamExpr p = LinqExpr.Parameter(typeof(T), Name);
            Scope.Add(p);
            if (Map != null)
                Map.Add(Expr, p);
            return p;
        }

        private static ParamExpr Declare<T>(IList<ParamExpr> Scope, IDictionary<Expression, LinqExpr> Map, Expression Expr)
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

        // Returns 1 / x.
        private static LinqExpr Reciprocal(LinqExpr x) { return LinqExpr.Divide(ConstantExpr(1.0, x.Type), x); }
        // Returns abs(x).
        private static LinqExpr Abs(LinqExpr x) { return LinqExpr.Condition(LinqExpr.LessThan(x, ConstantExpr(0.0, x.Type)), LinqExpr.Negate(x), x); }
    }
}
