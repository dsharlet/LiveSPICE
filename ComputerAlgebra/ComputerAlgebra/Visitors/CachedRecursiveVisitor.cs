using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ComputerAlgebra
{
    /// <summary>
    /// RecursiveExpressionVisitor that caches the results of Visit. 
    /// The cache enables the CachedRecursiveVisitor to detect and avoid stack overflow situations.
    /// </summary>
    public class CachedRecursiveVisitor : RecursiveExpressionVisitor
    {
        private Dictionary<Expression, Expression> cache = new Dictionary<Expression,Expression>();

        /// <summary>
        /// Called when visiting an Expression has tentatively been added to the cache, but Visit has not yet returned. This likely indicates infinite recursion.
        /// </summary>
        /// <param name="E"></param>
        /// <returns></returns>
        protected virtual Expression Revisit(Expression E) { throw new StackOverflowException("Infinite recursion detected."); }

        public override Expression Visit(Expression E)
        {
            Expression VE;
            if (cache.TryGetValue(E, out VE))
                return !ReferenceEquals(VE, null) ? VE : Revisit(E);

            // Tentatively cache this expression to detect revisits.
            cache[E] = null;

            VE = base.Visit(E);
            cache[E] = VE;
            return VE;
        }
    }
}
