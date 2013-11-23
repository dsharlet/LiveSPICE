using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ComputerAlgebra
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
            if (f is Product)
                return Product.New(((Product)f).Terms.Select(i => i.Factor(x)));

            if (f is Power)
            {
                Expression l = ((Power)f).Left.Factor(x);
                Expression r = ((Power)f).Right;
                return Product.New(Product.TermsOf(l).Select(i => Power.New(i, r)));
            }

            if (f is Sum)
            {
                Sum s = (Sum)f;

                // If f is a polynomial of x, factor it.
                if (!ReferenceEquals(x, null))
                {
                    try
                    {
                        return Polynomial.New(f, x).Factor(x);
                    }
                    catch (Exception) { }
                }

                List<Expression> terms = s.Terms.Select(i => i.Factor()).ToList();
                
                // All of the distinct factors.
                List<Expression> factors = terms.SelectMany(i => Product.TermsOf(i)).Distinct().ToList();
                // Choose the most common factor to use.
                Expression factor = factors.ArgMax(i => terms.Count(j => Product.TermsOf(j).Contains(i)));
                // Find the terms that contain the factor.
                List<Expression> contains = terms.Where(i => Product.TermsOf(i).Contains(factor)).ToList();
                // If more than one term contains the factor, pull it out and factor the resulting expression (again).
                if (contains.Count > 1)
                    return Sum.New(
                        Product.New(factor, Sum.New(contains.Select(i => Product.New(Product.TermsOf(i).Except(factor))))),
                        Sum.New(terms.Except(contains, Expression.RefComparer))).Factor();
            }

            return f;
        }

        public static Expression Factor(this Expression f) { return Factor(f, null); }
    }
}
