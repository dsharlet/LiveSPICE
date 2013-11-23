using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComputerAlgebra.LinqCompiler;
using LinqExprs = System.Linq.Expressions;
using LinqExpr = System.Linq.Expressions.Expression;
using ParamExpr = System.Linq.Expressions.ParameterExpression;

namespace ComputerAlgebra
{
    /// <summary>
    /// Extensions for solving equations.
    /// </summary>
    public static class NSolveExtension
    {
        /// <summary>
        /// Compute the Jacobian of F(x).
        /// </summary>
        /// <param name="F"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        public static List<LinearCombination> Jacobian(this IEnumerable<Expression> F, IEnumerable<Arrow> x)
        {
            List<Expression> B = x.Select(i => i.Right).ToList();
            List<LinearCombination> J = new List<LinearCombination>();
            foreach (Expression i in F)
            {
                LinearCombination Ji = new LinearCombination(B);
                Ji.Tag = i;
                foreach (Arrow j in x)
                    Ji[j.Right] = i.Differentiate(j.Left);
                J.Add(Ji);
            }
            return J;
        }

        /// <summary>
        /// Compute the Jacobian of F(x).
        /// </summary>
        /// <param name="F"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        public static List<LinearCombination> Jacobian(this IEnumerable<Expression> F, IEnumerable<Expression> x)
        {
            return Jacobian(F, x.Select(i => Arrow.New(i, i)));
        }
        
        // Use neton's method to solve F(x) = 0, with initial guess x0.
        private static List<Arrow> NewtonsMethod(List<Expression> F, List<Arrow> x0, double Epsilon, int MaxIterations)
        {
            // Numerically approximate the solution of F = 0 with Newton's method, 
            // i.e. solve JF(x0)*(x - x0) = -F(x0) for x.
            List<LinearCombination> J = Jacobian(F, x0.Select(i => i.Left));

            int M = F.Count;
            int N = x0.Count;

            // Define a function to evaluate JxF(x0).
            CodeGen code = new CodeGen();

            ParamExpr _J = code.Decl<double[,]>(Scope.Parameter, "J");
            ParamExpr _x0 = code.Decl<double[]>(Scope.Parameter, "x0");

            // Load x_j from the input array and add them to the map.
            for (int j = 0; j < N; ++j)
                code.DeclInit(x0[j].Left, LinqExpr.ArrayAccess(_x0, LinqExpr.Constant(j)));

            LinqExpr error = code.Decl<double>("error");

            // Compile the expressions to assign J.
            for (int i = 0; i < M; ++i)
            {
                LinqExpr _i = LinqExpr.Constant(i);
                for (int j = 0; j < N; ++j)
                    code.Add(LinqExpr.Assign(
                        LinqExpr.ArrayAccess(_J, _i, LinqExpr.Constant(j)), 
                        code.Compile(J[i][x0[j].Left])));
                LinqExpr e = code.Compile(F[i]);
                code.Add(LinqExpr.Assign(LinqExpr.ArrayAccess(_J, _i, LinqExpr.Constant(N)), e));
                // error += e * e
                code.Add(LinqExpr.AddAssign(error, LinqExpr.Multiply(e, e)));
            }

            // return error
            code.Return<double>(error);

            Func<double[,], double[], double> EvalJ = code.Build<Func<double[,], double[], double>>().Compile();
            
            double[,] JxF = new double[M, N + 1];
            double[] x = new double[N];
            double[] dx = new double[N];
            for (int j = 0; j < N; ++j)
            {
                x[j] = (double)x0[j].Right;
                dx[j] = 0.0;
            }

            double epsilon = Epsilon * Epsilon * N;

            for (int n = 0; n < MaxIterations; ++n)
            {
                // Evaluate JxF and F.
                if (EvalJ(JxF, x) < epsilon)
                    return Enumerable.Range(0, N).Select(i => Arrow.New(x0[i].Left, x[i])).ToList();

                // solve for dx.
                // For each variable in the system...
                for (int j = 0; j < N; ++j)
                {
                    int pi = j;
                    double max = Math.Abs(JxF[j, j]);

                    // Find a pivot row for this variable.
                    for (int i = j + 1; i < M; ++i)
                    {
                        // if(|JxF[i][j]| > max) { pi = i, max = |JxF[i][j]| }
                        double maxj = Math.Abs(JxF[i, j]);
                        if (maxj > max)
                        {
                            pi = i;
                            max = maxj;
                        }
                    }

                    // Swap pivot row with the current row.
                    if (pi != j)
                        for (int ij = j; ij <= N; ++ij)
                            Swap(ref JxF[j, ij], ref JxF[pi, ij]);

                    // Eliminate the rows after the pivot.
                    double p = JxF[j, j];
                    for (int i = j + 1; i < M; ++i)
                    {
                        double s = JxF[i, j] / p;
                        for (int ij = j + 1; ij <= N; ++ij)
                            JxF[i, ij] -= JxF[j, ij] * s;
                    }
                }

                // JxF is now upper triangular, solve it.
                for (int j = N - 1; j >= 0; --j)
                {
                    double r = JxF[j, N];
                    for (int ji = j + 1; ji < N; ++ji)
                        r += JxF[j, ji] * dx[ji];

                    double dxj = -r / JxF[j, j];
                    dx[j] = dxj;
                    x[j] += dxj;
                }
            }

            // Failed to converge.
            throw new NotFiniteNumberException("NSolve failed to converge.");
        }

        private static void Swap(ref double a, ref double b) { double t = a; a = b; b = t; }

        // Use homotopy method with newton's method to find a solution for F(x) = 0.
        private static List<Arrow> NSolve(List<Expression> F, List<Arrow> x0, double Epsilon, int MaxIterations)
        {
            List<Expression> F0 = F.Select(i => i.Evaluate(x0)).ToList();

            // Remember where we last succeeded/failed.
            double s0 = 0.0;
            double s1 = 1.0;
            do
            {
                try
                {
                    Real s = s0;
                    // H(F, s) = F + s*F0
                    List<Expression> H = F.Zip(F0, (i, i0) => i - s * i0).ToList();
                    x0 = NewtonsMethod(H, x0, Epsilon, MaxIterations);

                    // Success at this s!
                    s1 = s0;
                    // Go near the goal.
                    s0 = Lerp(s0, 0.0, 0.9);
                }
                catch (NotFiniteNumberException)
                {
                    // Go near the last success.
                    s0 = Lerp(s0, s1, 0.9);
                }
            } while (s0 > 0.0 && s1 >= s0 + 1e-6);

            // Make sure the last solution is at F itself.
            if (s0 != 0.0)
                x0 = NewtonsMethod(F, x0, Epsilon, MaxIterations);

            return x0;
        }

        private static double Lerp(double a, double b, double t) { return a + (b - a) * t; }

        /// <summary>
        /// Numerically solve a system of equations implicitly equal to 0.
        /// </summary>
        /// <param name="f">System of equations to solve for 0.</param>
        /// <param name="x">List of variables to solve for, with an initial guess.</param>
        /// <param name="Epsilon">Threshold for convergence.</param>
        /// <returns></returns>
        public static List<Arrow> NSolve(this IEnumerable<Equal> f, IEnumerable<Arrow> x, double Epsilon, int MaxIterations)
        {
            return NSolve(f.Select(i => EqualToZero(i)).AsList(), x.AsList(), Epsilon, MaxIterations);
        }

        /// <summary>
        /// Numerically solve a system of equations implicitly equal to 0.
        /// </summary>
        /// <param name="f">System of equations to solve for 0.</param>
        /// <param name="x">List of variables to solve for, with an initial guess.</param>
        /// <returns></returns>
        public static List<Arrow> NSolve(this IEnumerable<Equal> f, IEnumerable<Arrow> x) { return NSolve(f, x, 1e-6, 64); }

        private static Expression EqualToZero(Expression i)
        {
            if (i is Equal)
                return ((Equal)i).Left - ((Equal)i).Right;
            else
                return i;
        }
    }
}
