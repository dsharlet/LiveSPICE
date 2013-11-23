using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ComputerAlgebra
{
    /// <summary>
    /// ExpressionVisitor that caches the results of Visit.
    /// </summary>
    public abstract class CachedVisitor<T> : ExpressionVisitor<T>
    {
        private Dictionary<Expression, T> cache = new Dictionary<Expression,T>();

        public override T Visit(Expression E)
        {
            T VE;
            if (cache.TryGetValue(E, out VE))
                return VE;
            VE = base.Visit(E);
            cache[E] = base.Visit(E);
            return VE;
        }
    }
}
