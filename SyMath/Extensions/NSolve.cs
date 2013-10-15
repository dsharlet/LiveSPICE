using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        public static Matrix Jacobian(List<Expression> F, List<Expression> x)
        {
            Matrix J = new Matrix(F.Count, x.Count);
            for (int i = 0; i < F.Count; ++i)
                for (int j = 0; j < x.Count; ++j)
                    J[i, j] = F[i].Differentiate(x[j]);
            return J;
        }

        private static List<Equal> NewtonRhapson(List<Expression> F, List<Arrow> x0)
        {
            List<Expression> x = x0.Select(i => i.Left).ToList();

            Matrix J = Jacobian(F, x);

            // Compute J * (x - x0)
            Matrix X = new Matrix(x0.Count, 1);
            for (int i = 0; i < x0.Count; ++i)
                X[i] = x0[i].Left - x0[i].Right;
            Matrix JX = J.Evaluate(x0) * X;

            // Solve for x.
            List<Equal> newton = new List<Equal>();
            for (int i = 0; i < F.Count; ++i)
                newton.Add(Equal.New(JX[i, 0], -F[i].Evaluate(x0)));
            
            return newton;
        }

        private static List<Arrow> NSolve(List<Equal> f, List<Arrow> x0, int N)
        {
            List<Expression> x = x0.Select(i => i.Left).ToList();
            
            // Numerically approximate the result with Newton's method, 
            // i.e. solve JF(x0)*(x - x0) = -F(x0) for x.
            List<Expression> F = f.Select(i => i.Left - i.Right).ToList();
            Matrix J = Jacobian(F, x);
            Equal[] newton = new Equal[F.Count];
            for (int n = 0; n < N; ++n)
            {
                // Compute J * (x - x0)
                Matrix X = new Matrix(x0.Count, 1);
                for (int i = 0; i < x0.Count; ++i)
                    X[i] = x0[i].Left - x0[i].Right;
                Matrix JX = J.Evaluate(x0) * X;

                // Solve for x.
                for (int i = 0; i < F.Count; ++i)
                    newton[i] = Equal.New(JX[i, 0], -F[i].Evaluate(x0));
                x0 = newton.Solve(x);
            }

            return x0.AsList();
        }

        /// <summary>
        /// Compute a single Newton-Rhapson iteration for finding the roots of f(x), but does not solve the resulting system.
        /// </summary>
        /// <param name="f"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        public static List<Equal> NewtonRhapson(this IEnumerable<Expression> f, IEnumerable<Arrow> x)
        {
            return NewtonRhapson(f.AsList(), x.AsList());
        }

        /// <summary>
        /// Solve a system of equations numerically.
        /// </summary>
        /// <param name="f">System of equations to solve.</param>
        /// <param name="x">List of variables to solve for, with an initial guess.</param>
        /// <param name="N">Number of iterations to use in finding the solution.</param>
        /// <returns></returns>
        public static List<Arrow> NSolve(this IEnumerable<Equal> f, IEnumerable<Arrow> x, int N)
        {
            return NSolve(f.AsList(), x.AsList(), N);
        }

        
    }
}
