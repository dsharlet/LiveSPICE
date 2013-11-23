using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ComputerAlgebra
{
    /// <summary>
    /// Base class for a linear transform operation.
    /// </summary>
    public abstract class LinearTransform : VisitorTransform
    {
        /// <summary>
        /// Check if x is a constant for this transform.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        protected abstract bool IsConstant(Expression x);

        // V(x + y) = V(x) + V(y)
        protected override Expression VisitSum(Sum A) { return Sum.New(A.Terms.Select(i => Visit(i))); }

        // V(A*x) = A*V(x)
        protected override Expression VisitProduct(Product M)
        {
            IEnumerable<Expression> A = M.Terms.Where(i => IsConstant(i));
            if (A.Any())
                return Product.New(A.Append(Visit(Product.New(M.Terms.Where(i => !IsConstant(i))))));
            return base.VisitProduct(M);
        }

        // V((A*x)^n) = A^(1/n)*V(x^n)
        protected override Expression VisitPower(Power P)
        {
            if (!IsConstant(P.Right))
                return base.VisitPower(P);

            Expression L = P.Left.Factor();

            IEnumerable<Expression> A = Product.TermsOf(L).Where(i => IsConstant(i));
            if (A.Any())
                return Product.New(Power.New(Product.New(A), 1 / P.Right), Visit(Product.New(Product.TermsOf(L).Where(i => !IsConstant(i)))));
            return base.VisitPower(P);
        }
    }
}
