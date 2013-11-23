using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ComputerAlgebra
{
    /// <summary>
    /// Transform using a pattern expression.
    /// </summary>
    public abstract class PatternTransform : ITransform
    {
        protected Expression pattern;
        private List<Expression> conditions = null;

        protected abstract Expression ApplyTransform(Expression x, MatchContext Matched);

        protected PatternTransform(Expression Pattern, IEnumerable<Expression> PreConditions)
        {
            pattern = Pattern;
            conditions = PreConditions.ToList();
        }

        protected PatternTransform(Expression Pattern, params Expression[] PreConditions) 
            : this(Pattern, PreConditions.AsEnumerable()) 
        { 
        }

        /// <summary>
        /// Get the pattern for this transform.
        /// </summary>
        public Expression Pattern { get { return pattern; } }

        /// <summary>
        /// Transform an expression by matching to the pattern and substituting the result if successful.
        /// </summary>
        /// <param name="E">Expression to transform.</param>
        /// <returns>The transformed expression.</returns>
        public Expression Transform(Expression x)
        {
            MatchContext matched = pattern.Matches(x);
            if (matched != null && conditions.All(i => i.Evaluate(matched).IsTrue()))
                return ApplyTransform(x, matched);
            else
                return x;
        }
    }
}
