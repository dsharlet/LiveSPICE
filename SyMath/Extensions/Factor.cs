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
                    catch (AlgebraException) { }
                }

                List<Expression> terms = s.Terms.Select(i => i.Factor()).ToList();
                
                // All of the distinct factors, excluding constants.
                List<Expression> factors = terms.SelectMany(i => Product.TermsOf(i).Where(j => !(j is Constant))).Distinct().ToList();
                // Choose the most common factor to use.
                Expression factor = factors.ArgMax(i => terms.Count(j => Product.TermsOf(j).Contains(i)));
                List<Expression> contains = terms.Where(i => Product.TermsOf(i).Contains(factor)).ToList();
                if (contains.Count > 1)
                    return Sum.New(
                        Product.New(factor, Sum.New(contains.Select(i => Product.New(Product.TermsOf(i).Except(factor))))),
                        Sum.New(terms.ExceptUnique(contains)).Factor()).Factor();

                // Find the largest constant.
                Real A = 1;
                foreach (Expression i in terms)
                {
                    Real Ai = Product.TermsOf(i).OfType<Constant>().Select(j => j.Value).Aggregate((Real)1, (a, j) => a * j);
                    if (Real.Abs(Real.Max(Ai, 1 / Ai)) > Real.Abs(Real.Max(A, 1 / A)))
                        A = Ai;
                    else if (Ai == -1 && A == 1)
                        A = Ai;
                }

                if (A != 1)
                    return Product.New(Constant.New(A), Sum.New(terms.Select(i => i / A)));
            }

            return f;
        }

        public static Expression Factor(this Expression f) { return Factor(f, null); }
    }
}
