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
        public static List<LinearCombination> Jacobian(this IEnumerable<Expression> F, IEnumerable<Expression> x)
        {
            List<LinearCombination> J = new List<LinearCombination>();
            foreach (Expression i in F)
            {
                LinearCombination Ji = new LinearCombination(x);
                Ji.Tag = i;
                foreach (Expression j in x)
                    Ji[j] = i.Differentiate(j);
                J.Add(Ji);
            }
            return J;
        }

        private static List<Arrow> NSolve(List<Equal> f, List<Arrow> x0, int N)
        {
            List<Expression> x = x0.Select(i => i.Left).ToList();
            
            // Numerically approximate the result with Newton's method, 
            // i.e. solve JF(x0)*(x - x0) = -F(x0) for x.
            List<Expression> F = f.Select(i => i.Left - i.Right).ToList();
            List<LinearCombination> J = Jacobian(F, x);
            Equal[] newton = new Equal[F.Count];
            for (int n = 0; n < N; ++n)
            {
                for (int i = 0; i < F.Count; ++i)
                {
                    // Compute J * (x - x0)
                    Expression Jx = Add.New(x0.Select(j => J[i][j.Left].Evaluate(x0) * (j.Left - j.Right)));
                    // Solve for x.
                    newton[i] = Equal.New(Jx, -F[i].Evaluate(x0));
                }
                x0 = newton.Solve(x);
            }

            return x0.AsList();
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
