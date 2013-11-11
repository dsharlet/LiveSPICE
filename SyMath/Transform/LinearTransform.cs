using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyMath
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
    }
}
