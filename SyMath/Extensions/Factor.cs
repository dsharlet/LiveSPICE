using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyMath
{
    public static class FactorExtension
    {
        /// <summary>
        /// Distribute products across sums.
        /// </summary>
        /// <param name="f"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        public static Expression Factor(this Expression f, Expression x) 
        {
            if (f is Multiply)
                return Multiply.New(((Multiply)f).Terms.Select(i => i.Factor(x)));
            if (f is Power)
                return Power.New(((Power)f).Left.Factor(x), ((Power)f).Right);

            if (f is Add)
            {
                // If f is a polynomial of x, factor it.
                if (!ReferenceEquals(x, null))
                {
                    Polynomial P = Polynomial.New(f, x);
                    return P.Factor(x);
                }
                
                // Just try to find common sub-expressions.
                if (!ReferenceEquals(x, null))
                {
                    Expression fx = ((Add)f).Terms.FirstOrDefault(i => i.IsFunctionOf(x));
                    if (!ReferenceEquals(fx, null))
                    {
                        Expression A = Multiply.TermsOf(fx).FirstOrDefault(i => i is Constant);
                        if (!ReferenceEquals(A, null))
                            return Multiply.New(A, Add.New(((Add)f).Terms.Select(i => i / A)));
                    }
                }
            }

            return f;
        }

        public static Expression Factor(this Expression f) { return Factor(f, null); }
    }
}
