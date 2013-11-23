using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ComputerAlgebra
{

            //// Integrals.
            //Simplify.AddRange(
            //    new LinearTransform("I[f, x]", "f", "x"),

            //    new SubstituteTransform("I[u*D[u, x], x]", "u^2/2"),
            //    new SubstituteTransform("I[Exp[u]*D[u, x], x]", "Exp[u]"),
            //    new SubstituteTransform("I[u^N*D[u, x], x]", "u^(N + 1)/(N + 1)", "IsInteger[N]", "N != -1"),
            //    new SubstituteTransform("I[u*D[v, x], x]", "u*v - I[v*D[u, x], x]"),
            //    new SubstituteTransform("I[D[u, x], x]", "u")
            //    );

    public static class IntegrateExtension
    {
        /// <summary>
        /// Integrate expression with respect to x.
        /// </summary>
        /// <param name="f"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        public static Expression Integrate(this Expression f, Expression x)
        {
            return Call.I(f, x);
        }
    }
}
