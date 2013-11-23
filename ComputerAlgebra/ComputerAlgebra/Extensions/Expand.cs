using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ComputerAlgebra
{
    public static class ExpandExtension
    {
        // Evaluate x*A, distributing x if A is Add.
        public static Expression Distribute(Expression x, Expression A)
        {
            if (A is Sum || x is Sum)
                return EvaluateVisitor.EvaluateSum(Sum.TermsOf(A).SelectMany(i => Sum.TermsOf(Distribute(i, x))));
            else
                return Product.New(A, x).Evaluate();
        }

        // Expand N(x)/D(x) using partial fractions.
        private static Expression ExpandPartialFractions(Expression N, Expression D, Expression x)
        {
            List<Expression> terms = new List<Expression>();
            List<Variable> unknowns = new List<Variable>();
            List<Expression> basis = new List<Expression>();
            foreach (Expression i in Product.TermsOf(D))
            {
                // Get the multiplicity of this basis term.
                Expression e = i;
                int n = Power.IntegralExponentOf(e);
                if (n != 1)
                    e = ((Power)i).Left;

                // Convert to a polynomial.
                Polynomial Pi = Polynomial.New(e, x);

                // Add new terms for each multiplicity n.
                for (int j = 1; j <= n; ++j)
                {
                    // Expression for the unknown numerator of this term.
                    Expression unknown = 0;
                    for (int k = 0; k < Pi.Degree; ++k)
                    {
                        Variable Ai = Variable.New("_A" + unknowns.Count.ToString());
                        unknown += Ai * (x ^ k);
                        unknowns.Add(Ai);
                    }

                    terms.Add(Product.New(unknown, Power.New(e, -j)));
                }
                basis.Add(i);
            }

            // Equate the original expression with the decomposed expressions.
            D = Sum.New(terms.Select(j => D * j)).Expand();
            Polynomial l = Polynomial.New(N, x);
            Polynomial r = Polynomial.New(D, x);

            // Equate terms of equal degree and solve for the unknowns.
            int degree = Math.Max(l.Degree, r.Degree);
            List<Equal> eqs = new List<Equal>(degree + 1);
            for (int i = 0; i <= degree; ++i)
                eqs.Add(Equal.New(l[i], r[i]));
            List<Arrow> A = eqs.Solve(unknowns);

            // Substitute the now knowns.
            return Sum.New(terms.Select(i => i.Evaluate(A)));
        }

        /// <summary>
        /// Expand a power expression.
        /// </summary>
        /// <param name="f"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        private static Expression ExpandPower(Power f, Expression x)
        {
            // Get integral exponent of f.
            int n = Power.IntegralExponentOf(f);

            // If this is an an integral constant negative exponent, attempt to use partial fractions.
            if (n < 0 && !ReferenceEquals(x, null))
            {
                Expression b = f.Left.Factor(x);
                if (n != -1)
                    b = Power.New(b, Math.Abs(n));
                return ExpandPartialFractions(1, b, x);
            }

            // If f is an add expression, expand it as if it were multiplication.
            if (n > 1 && f.Left is Sum)
            {
                Expression e = f.Left;
                for (int i = 1; i < n; ++i)
                    e = Distribute(f.Left, e);
                return e;
            }

            return f;
        }

        /// <summary>
        /// Expand a multiplication expression.
        /// </summary>
        /// <param name="f"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        private static Expression ExpandMultiply(Expression f, Expression x)
        {
            // If the denominator is multiplication, expand partial fractions.
            if (!ReferenceEquals(x, null))
            {
                Expression d = Product.Denominator(f).Factor(x);
                if (d is Product)
                    return ExpandPartialFractions(Product.Numerator(f), (Product)d, x);
            }

            // If f contains an add expression, distribute it.
            if (Product.TermsOf(f).Any(i => i is Sum))
            {
                Expression e = 1;
                foreach (Expression i in Product.TermsOf(f))
                    e = Distribute(i.Expand(x), e);
                return e;
            }

            return f;
        }

        /// <summary>
        /// Distribute products across sums, using partial fractions if necessary.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static Expression Expand(this Expression f, Expression x) 
        {
            if (f is Product)
                return ExpandMultiply(f, x);
            else if (f is Sum)
                return Sum.New(((Sum)f).Terms.Select(i => i.Expand(x)));
            else if (f is Power)
                return ExpandPower((Power)f, x);
            else if (f is Binary)
                return Binary.New(((Binary)f).Operator, ((Binary)f).Left.Expand(), ((Binary)f).Right.Expand());
            else if (f is Unary)
                return Unary.New(((Unary)f).Operator, ((Unary)f).Operand.Expand());

            return f;
        }

        /// <summary>
        /// Distribute products across sums.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static Expression Expand(this Expression f) { return Expand(f, null); }
    }
}
