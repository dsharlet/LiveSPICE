using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinqExprs = System.Linq.Expressions;
using LinqExpr = System.Linq.Expressions.Expression;
using ParamExpr = System.Linq.Expressions.ParameterExpression;

namespace SyMath
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

            // Compile the Jacobian and F to delegates so we can evaluate them quickly during iteration.
            Delegate[][] _J = new Delegate[M][];
            for (int i = 0; i < M; ++i)
            {
                _J[i] = new Delegate[N + 1];

                List<ParamExpr> args = x0.Select(j => LinqExpr.Parameter(typeof(double), j.Left.ToString())).ToList();
                Dictionary<Expression, LinqExpr> map = Enumerable.Range(0, N).ToDictionary(j => x0[j].Left, j => (LinqExpr)args[j]);
                
                for (int j = 0; j < N; ++j)
                    _J[i][j] = LinqExpr.Lambda(J[i][x0[j].Left].Compile(map), args).Compile();
                _J[i][N] = LinqExpr.Lambda(F[i].Compile(map), args).Compile();
            }
            
            double[][] JxF = new double[M][];
            for (int i = 0; i < M; ++i)
                JxF[i] = new double[N + 1];
            double[] x = new double[N];
            double[] dx = new double[N];
            for (int j = 0; j < N; ++j)
            {
                x[j] = (double)x0[j].Right;
                dx[j] = 0.0;
            }

            for (int n = 0; n < MaxIterations; ++n)
            {
                // Evaluate JxF and F.
                double error = 0.0;
                for (int i = 0; i < M; ++i)
                {
                    object[] _x = x.Cast<object>().ToArray();
                    for (int j = 0; j < N; ++j)
                        JxF[i][j] = (double)_J[i][j].DynamicInvoke(_x);
                    double e = (double)_J[i][N].DynamicInvoke(_x);
                    JxF[i][N] = e;
                    error += e * e;
                }
                if (error < Epsilon * Epsilon * N)
                    return Enumerable.Range(0, N).Select(i => Arrow.New(x0[i].Left, x[i])).ToList();

                // solve for dx.
                // For each variable in the system...
                for (int j = 0; j < N; ++j)
                {
                    int pi = j;
                    double max = Math.Abs(JxF[j][j]);

                    // Find a pivot row for this variable.
                    for (int i = j + 1; i < M; ++i)
                    {
                        // if(|JxF[i][j]| > max) { pi = i, max = |JxF[i][j]| }
                        double maxj = Math.Abs(JxF[i][j]);
                        if (maxj > max)
                        {
                            pi = i;
                            max = maxj;
                        }
                    }

                    // Swap pivot row with the current row.
                    if (pi != j)
                        for (int ij = j; ij <= N; ++ij)
                            Swap(ref JxF[j][ij], ref JxF[pi][ij]);

                    // Eliminate the rows after the pivot.
                    double p = JxF[j][j];
                    for (int i = j + 1; i < M; ++i)
                    {
                        double s = JxF[i][j] / p;
                        for (int ij = j + 1; ij <= N; ++ij)
                            JxF[i][ij] -= JxF[j][ij] * s;
                    }
                }

                // JxF is now upper triangular, solve it.
                for (int j = N - 1; j >= 0; --j)
                {
                    double r = JxF[j][N];
                    for (int ji = j + 1; ji < N; ++ji)
                        r += JxF[j][ji] * dx[ji];

                    double dxj = -r / JxF[j][j];
                    dx[j] = dxj;
                    x[j] += dxj;
                }
            }

            // Failed to converge.
            throw new AlgebraException("NSolve failed to converge.");
        }

        private static void Swap(ref double a, ref double b) { double t = a; a = b; b = t; }

        // Use homotopy method with newton's method to find a solution for F(x) = 0.
        private static List<Arrow> NSolve(List<Expression> F, List<Arrow> x0, double Epsilon, int MaxIterations)
        {
            List<Expression> F0 = F.Select(i => i - i.Evaluate(x0)).ToList();

            // Remember where we last succeeded/failed.
            double s0 = 0.0;
            double s1 = 1.0;
            do
            {
                // H(F, s) = s*F + (1 - s)*F0
                List<Expression> H = F.Zip(F0, (i, i0) => s1 * i + (1 - s1) * i0).ToList();

                try
                {
                    x0 = NewtonsMethod(H, x0, Epsilon, MaxIterations);
                    // Success at this s!
                    s0 = s1;
                    // Go near the goal.
                    s1 = Lerp(s1, 1.0, 0.9);
                }
                catch (AlgebraException)
                {
                    // Go near the last success.
                    s1 = Lerp(s0, s1, 0.1);
                }
            } while (s1 < 1.0 && s1 >= s0 + 1e-6);

            if (s1 != 1.0)
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
        public static List<Arrow> NSolve(this IEnumerable<Expression> f, IEnumerable<Arrow> x, double Epsilon, int MaxIterations)
        {
            return NSolve(f.Select(i => EqualToZero(i)).AsList(), x.AsList(), Epsilon, MaxIterations);
        }

        /// <summary>
        /// Numerically solve a system of equations implicitly equal to 0.
        /// </summary>
        /// <param name="f">System of equations to solve for 0.</param>
        /// <param name="x">List of variables to solve for, with an initial guess.</param>
        /// <returns></returns>
        public static List<Arrow> NSolve(this IEnumerable<Expression> f, IEnumerable<Arrow> x) { return NSolve(f, x, 1e-6, 64); }

        private static Expression EqualToZero(Expression i)
        {
            if (i is Equal)
                return ((Equal)i).Left - ((Equal)i).Right;
            else
                return i;
        }
    }
}
