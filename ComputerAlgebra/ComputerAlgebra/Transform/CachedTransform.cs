using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ComputerAlgebra
{
    /// <summary>
    /// Cache the results of a transform.
    /// </summary>
    public class CachedTransform : ITransform
    {
        private Dictionary<Expression, Expression> cache = new Dictionary<Expression, Expression>();
        private ITransform transform;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="T">Transform to cache the results of.</param>
        public CachedTransform(ITransform T) { transform = T; }

        public Expression Transform(Expression E)
        {
            Expression TE;
            if (cache.TryGetValue(E, out TE))
                return TE;
            TE = transform.Transform(E);
            cache[E] = TE;
            return TE;
        }
    }
}
