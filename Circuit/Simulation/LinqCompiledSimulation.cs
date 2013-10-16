using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Reflection.Emit;
using SyMath;
using LinqExpressions = System.Linq.Expressions;
using LinqExpression = System.Linq.Expressions.Expression;
using ParameterExpression = System.Linq.Expressions.ParameterExpression;

namespace Circuit
{
    /// <summary>
    /// Simulate a circuit.
    /// </summary>
    public class LinqCompiledSimulation : Simulation
    {
        // Stores any global state in the simulation (previous state values, mostly).
        private Dictionary<Expression, GlobalExpr<double>> globals = new Dictionary<Expression, GlobalExpr<double>>();
                
        /// <summary>
        /// Create a simulation for the given circuit.
        /// </summary>
        /// <param name="C">Circuit to simulate.</param>
        /// <param name="T">Sampling period.</param>
        public LinqCompiledSimulation(Circuit Circuit, Quantity SampleRate, int Oversample, ILog Log) : base(Circuit, SampleRate, Oversample, Log)
        {
            // Create globals for the state variables (differentials).
            foreach (Arrow i in differential)
                globals[i.Left.Evaluate(t, t0)] = new GlobalExpr<double>(0.0);

            // Create globals for iterative unknowns
            foreach (AlgebraicSystem i in algebraic)
                foreach (Expression j in i.Unknowns)
                    globals[j.Evaluate(t, t0)] = new GlobalExpr<double>(0.0);
            
            // Create a globals for the value of each f0.
            foreach (Arrow i in f0)
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
            int Iterations)
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
        
        Dictionary<long, Delegate> compiled = new Dictionary<long, Delegate>();
        // Compile and cache delegates for processing various IO configurations for this simulation.
        private Delegate Compile(IEnumerable<Expression> Input, IEnumerable<Expression> Output, IEnumerable<Expression> Parameters)
        {
            long hash = Input.OrderedHashCode();
            hash = hash * 33 + Output.OrderedHashCode();
            hash = hash * 33 + Parameters.OrderedHashCode();

            Delegate d;
            if (compiled.TryGetValue(hash, out d))
                return d;

            LogTime(MessageType.Info, "Defining sample processing function...");
            LogExpressions("Input:", Input);
            LogExpressions("Output:", Output);
            LogExpressions("Parameters:", Parameters);
            LinqExpressions.LambdaExpression lambda = DefineProcessFunction(Input, Output, Parameters);
            LogTime(MessageType.Info, "Compiling sample processing function...");
            d = lambda.Compile();
            LogTime(MessageType.Info, "Done.");

            return compiled[hash] = d;
        }
        
        // The resulting lambda processes N samples, using buffers provided for Input and Output:
        //  void Process(int N, double t0, double T, double[] Input0 ..., double[] Output0 ..., double Parameter0 ...)
        //  { ... }
        private LinqExpressions.LambdaExpression DefineProcessFunction(IEnumerable<Expression> Input, IEnumerable<Expression> Output, IEnumerable<Expression> Parameters)
        {
            // Map expressions to identifiers in the syntax tree.
            Dictionary<Expression, LinqExpression> map = new Dictionary<Expression, LinqExpression>();
            Dictionary<Expression, LinqExpression> buffers = new Dictionary<Expression, LinqExpression>();
            
            // Lambda definition objects.
            List<ParameterExpression> parameters = new List<ParameterExpression>();
            List<ParameterExpression> locals = new List<ParameterExpression>();
            List<LinqExpression> body = new List<LinqExpression>();

            // Create parameters for the basic simulation info (N, t, T, Oversample, Iterations).
            ParameterExpression SampleCount = Declare<int>(parameters, "SampleCount");
            ParameterExpression t0 = Declare<double>(parameters, map, Simulation.t0);
            ParameterExpression T = Declare<double>(parameters, map, Component.T);
            ParameterExpression Oversample = Declare<int>(parameters, "Oversample");
            ParameterExpression Iterations = Declare<int>(parameters, "Iterations");
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
            ParameterExpression t = Declare<double>(locals, map, Simulation.t);
            body.Add(LinqExpression.Assign(t, t0));

            // double h = T / Oversample
            ParameterExpression h = Declare<double>(locals, "h");
            body.Add(LinqExpression.Assign(h, LinqExpression.Divide(T, LinqExpression.Convert(Oversample, typeof(double)))));

            // double invOversample = 1 / (double)Oversample
            ParameterExpression invOversample = Declare<double>(locals, "invOversample");
            body.Add(LinqExpression.Assign(invOversample, Reciprocal(LinqExpression.Convert(Oversample, typeof(double)))));

            // Load the globals to local variables and add them to the map.
            foreach (KeyValuePair<Expression, GlobalExpr<double>> i in globals)
                body.Add(LinqExpression.Assign(Declare<double>(locals, map, i.Key), i.Value));

            // Trivial timestep expressions that are not a function of the input or t can be set once here (outside the sample loop).
            // This might not be necessary if you trust the .Net expression compiler to lift this invariant code out of the loop.
            foreach (Arrow i in trivial.Where(i => !i.Right.DependsOn(Input) && !i.Right.DependsOn(Component.t)))
                body.Add(LinqExpression.Assign(Declare<double>(locals, map, i.Left), i.Right.Compile(map)));

            // for (int n = 0; n < SampleCount; ++n)
            ParameterExpression n = Declare<int>(locals, "n");
            For(body,
                () => body.Add(LinqExpression.Assign(n, LinqExpression.Constant(0))),
                LinqExpression.LessThan(n, SampleCount),
                () => body.Add(LinqExpression.PreIncrementAssign(n)),
                () =>
            {
                // Prepare input samples for oversampling interpolation.
                Dictionary<Expression, LinqExpression> dVi = new Dictionary<Expression, LinqExpression>();
                foreach (Expression i in Input)
                {
                    LinqExpression Va = map[i.Evaluate(t_t0)];
                    LinqExpression Vb = LinqExpression.MakeIndex(buffers[i], buffers[i].Type.GetProperty("Item"), new LinqExpression[] { n });

                    // double Vi = Va
                    body.Add(LinqExpression.Assign(Declare<double>(locals, map, i, i.ToString()), Va));

                    // dVi = (Vb - Vi) / Oversample
                    body.Add(LinqExpression.Assign(
                        Declare<double>(locals, dVi, i, "d" + i.ToString().Replace("[t]", "")),
                        LinqExpression.Multiply(LinqExpression.Subtract(Vb, Va), invOversample)));

                    // Va = Vb
                    body.Add(LinqExpression.Assign(Va, Vb));
                }

                // Prepare output sample accumulators for low pass filtering.
                Dictionary<Expression, LinqExpression> Vo = new Dictionary<Expression, LinqExpression>();
                foreach (Expression i in Output)
                    body.Add(LinqExpression.Assign(
                        Declare<double>(locals, Vo, i, i.ToString().Replace("[t]", "")),
                        LinqExpression.Constant(0.0)));

                // int ov = Oversample; 
                // do { -- ov; } while(ov > 0)
                ParameterExpression ov = Declare<int>(locals, "ov");
                body.Add(LinqExpression.Assign(ov, Oversample));
                DoWhile(body,() =>
                {
                    // t += h
                    body.Add(LinqExpression.AddAssign(t, h));

                    // Interpolate the input samples.
                    foreach (Expression i in Input)
                        body.Add(LinqExpression.AddAssign(map[i], dVi[i]));

                    // Compile the trivial timestep expressions that are a function of the input.
                    foreach (Arrow i in trivial.Where(i => !map.ContainsKey(i.Left)))
                        body.Add(LinqExpression.Assign(Declare<double>(locals, map, i.Left), i.Right.Compile(map)));

                    // Compile the differential timestep expressions.
                    foreach (Arrow i in differential)
                    {
                        body.Add(LinqExpression.Assign(
                            Declare<double>(locals, map, i.Left),
                            i.Right.Compile(map)));

                        Expression di_dt = D(i.Left, Simulation.t);
                        LinqExpression Vt0 = map[i.Left.Evaluate(t_t0)];

                        // double dV = (Vt - Vt0) / h, but we already divided by h when solving the system.
                        body.Add(LinqExpression.Assign(
                            Declare<double>(locals, map, di_dt, "d" + i.Left.ToString().Replace("[t]", "")), 
                            LinqExpression.Subtract(map[i.Left], Vt0)));
                    }
                    // Vt0 = Vt
                    foreach (Arrow i in differential)
                        body.Add(LinqExpression.Assign(map[i.Left.Evaluate(t_t0)], map[i.Left]));

                    // int it
                    ParameterExpression it = Declare<int>(locals, "it");

                    // Compile the algebraic systems' solutions.
                    foreach (AlgebraicSystem i in algebraic)
                    {
                        // it = Oversample
                        // do { ... --it } while(it > 0)
                        body.Add(LinqExpression.Assign(it, Iterations));
                        DoWhile(body, () =>
                        {
                            // Compile the numerical scheme to solve this system.
                            List<Arrow> iteration = i.Nonlinear.Solve(i.Unknowns);
                            foreach (Arrow j in iteration)
                            {
                                LinqExpression Vt0 = map[j.Left.Evaluate(t_t0)];
                                body.Add(LinqExpression.Assign(Vt0, j.Right.Compile(map)));
                                map[j.Left] = Vt0;
                            }

                            // --it;
                            body.Add(LinqExpression.PreDecrementAssign(it));
                        }, LinqExpression.GreaterThan(it, LinqExpression.Constant(0)));

                        // Compile the linear solutions.
                        foreach (Arrow j in i.Linear)
                            body.Add(LinqExpression.Assign(Declare<double>(locals, map, j.Left), j.Right.Compile(map)));
                    }

                    // Update f0.
                    foreach (Arrow i in f0)
                        body.Add(LinqExpression.Assign(map[i.Left], i.Right.Compile(map)));

                    // Compile the component voltage expressions.
                    foreach (Arrow i in components.Where(i => !map.ContainsKey(i.Left)))
                        body.Add(LinqExpression.Assign(Declare<double>(locals, map, i.Left), i.Right.Compile(map)));

                    // t0 = t
                    body.Add(LinqExpression.Assign(t0, t));

                    // Vo += i
                    foreach (Expression i in Output)
                        body.Add(LinqExpression.AddAssign(Vo[i], CompileOrWarn(i, map)));

                    // Vi_t0 = Vi
                    foreach (Expression i in Input)
                        body.Add(LinqExpression.Assign(map[i.Evaluate(t_t0)], map[i]));

                    // --ov;
                    body.Add(LinqExpression.PreDecrementAssign(ov));
                }, LinqExpression.GreaterThan(ov, LinqExpression.Constant(0)));

                // Output[i][n] = Vo / Oversample
                foreach (Expression i in Output)
                {
                    body.Add(LinqExpression.Assign(
                        LinqExpression.MakeIndex(buffers[i], buffers[i].Type.GetProperty("Item"), new LinqExpression[] { n }),
                        LinqExpression.Multiply(Vo[i], invOversample)));
                }
            });

            // Copy the global state variables back to the globals.
            foreach (KeyValuePair<Expression, GlobalExpr<double>> i in globals)
                body.Add(LinqExpression.Assign(i.Value, map[i.Key]));
            
            // Put it all together.
            return LinqExpression.Lambda(LinqExpression.Block(locals, body), parameters);
        }
        
        // If x fails to compile, return 0. 
        private LinqExpression CompileOrWarn(Expression x, IDictionary<Expression, LinqExpression> map)
        {
            try
            {
                return x.Compile(map);
            }
            catch (System.Exception ex)
            {
                Log.WriteLine(MessageType.Warning, "Error compiling output expression '{0}': {1}", x.ToString(), ex.Message);
                return LinqExpression.Constant(0.0);
            }
        }

        // Generate a for loop given the header and body expressions.
        private static void For(
            IList<LinqExpression> Target,
            Action Init,
            LinqExpression Condition,
            Action Step,
            Action<LinqExpressions.LabelTarget, LinqExpressions.LabelTarget> Body)
        {
            string name = Target.Count.ToString();
            LinqExpressions.LabelTarget begin = LinqExpression.Label("for_" + name + "_begin");
            LinqExpressions.LabelTarget end = LinqExpression.Label("for_" + name + "_end");

            // Generate the init code.
            Init();

            // Check the condition, exit if necessary.
            Target.Add(LinqExpression.Label(begin));
            Target.Add(LinqExpression.IfThen(LinqExpression.Not(Condition), LinqExpression.Goto(end)));

            // Generate the body code.
            Body(end, begin);

            // Generate the step code.
            Step();
            Target.Add(LinqExpression.Goto(begin));

            // Exit point.
            Target.Add(LinqExpression.Label(end));
        }

        // Generate a for loop given the header and body expressions.
        private static void For(
            IList<LinqExpression> Target,
            Action Init,
            LinqExpression Condition,
            Action Step,
            Action Body)
        {
            For(Target, Init, Condition, Step, (x, y) => Body());
        }

        // Generate a while loop given the condition and body expressions.
        private static void While(
            IList<LinqExpression> Target,
            LinqExpression Condition,
            Action<LinqExpressions.LabelTarget, LinqExpressions.LabelTarget> Body)
        {
            string name = (Target.Count + 1).ToString();
            LinqExpressions.LabelTarget begin = LinqExpression.Label("while_" + name + "_begin");
            LinqExpressions.LabelTarget end = LinqExpression.Label("while_" + name + "_end");

            // Check the condition, exit if necessary.
            Target.Add(LinqExpression.Label(begin));
            Target.Add(LinqExpression.IfThen(LinqExpression.Not(Condition), LinqExpression.Goto(end)));

            // Generate the body code.
            Body(end, begin);

            // Loop.
            Target.Add(LinqExpression.Goto(begin));

            // Exit label.
            Target.Add(LinqExpression.Label(end));
        }

        // Generate a while loop given the condition and body expressions.
        private static void While(
            IList<LinqExpression> Target,
            LinqExpression Condition,
            Action Body)
        {
            While(Target, Condition, (x, y) => Body());
        }

        // Generate a do-while loop given the condition and body expressions.
        private static void DoWhile(
            IList<LinqExpression> Target,
            Action<LinqExpressions.LabelTarget, LinqExpressions.LabelTarget> Body,
            LinqExpression Condition)
        {
            string name = (Target.Count + 1).ToString();
            LinqExpressions.LabelTarget begin = LinqExpression.Label("do_" + name + "_begin");
            LinqExpressions.LabelTarget end = LinqExpression.Label("do_" + name + "_end");

            // Check the condition, exit if necessary.
            Target.Add(LinqExpression.Label(begin));

            // Generate the body code.
            Body(end, begin);

            // Loop.
            Target.Add(LinqExpression.IfThen(Condition, LinqExpression.Goto(begin)));

            // Exit label.
            Target.Add(LinqExpression.Label(end));
        }

        // Generate a do-while loop given the condition and body expressions.
        private static void DoWhile(
            IList<LinqExpression> Target,
            Action Body,
            LinqExpression Condition)
        {
            DoWhile(Target, (x, y) => Body(), Condition);
        }

        private static ParameterExpression Declare<T>(IList<ParameterExpression> Scope, IDictionary<Expression, LinqExpression> Map, Expression Expr, string Name)
        {
            ParameterExpression p = LinqExpression.Parameter(typeof(T), Name);
            Scope.Add(p);
            if (Map != null)
                Map.Add(Expr, p);
            return p;
        }

        private static ParameterExpression Declare<T>(IList<ParameterExpression> Scope, IDictionary<Expression, LinqExpression> Map, Expression Expr)
        {
            return Declare<T>(Scope, Map, Expr, Expr.ToString());
        }

        private static ParameterExpression Declare<T>(IList<ParameterExpression> Scope, string Name)
        {
            return Declare<T>(Scope, null, null, Name);
        }

        private static ParameterExpression Declare(IList<ParameterExpression> Scope, IList<LinqExpression> Target, string Name, LinqExpression Init)
        {
            ParameterExpression p = LinqExpression.Parameter(Init.Type, Name);
            Scope.Add(p);
            Target.Add(LinqExpression.Assign(p, Init));
            return p;
        }
        
        // Returns 1 / x.
        private static LinqExpression Reciprocal(LinqExpression x)
        {
            LinqExpression one = null;
            if (x.Type == typeof(double))
                one = LinqExpression.Constant(1.0);
            else if (x.Type == typeof(float))
                one = LinqExpression.Constant(1.0f);
            else
                throw new NotImplementedException("Reciprocal");
            return LinqExpression.Divide(one, x);
        }
    }
}
