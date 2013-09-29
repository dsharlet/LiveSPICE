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

        /// <summary>
        /// V(x + y) = V(x) + V(y)
        /// </summary>
        /// <param name="A"></param>
        /// <returns></returns>
        protected override Expression VisitAdd(Add A) { return Add.New(A.Terms.Select(i => Visit(i))); }

        /// <summary>
        /// V(A*x) = A*V(x)
        /// </summary>
        /// <param name="M"></param>
        /// <returns></returns>
        protected override Expression VisitMultiply(Multiply M)
        {
            IEnumerable<Expression> f = M.Terms.Where(i => !IsConstant(i));
            if (f.Count() == 1)
                return Multiply.New(M.Terms.ExceptUnique(f.First()).Append(Visit(f.First())));
            return base.VisitMultiply(M);
        }
    }
}
