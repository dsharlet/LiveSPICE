using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ComputerAlgebra
{
    /// <summary>
    /// PatternTransform that replaces the matched pattern with a result expression.
    /// </summary>
    public class SubstituteTransform : PatternTransform
    {
        private Expression result;

        protected override Expression ApplyTransform(Expression x, MatchContext Matched)
        {
            return result.Substitute(Matched, true).Evaluate();
        }

        /// <summary>
        /// Construct a new PatternTransform.
        /// </summary>
        /// <param name="Pattern">Pattern to match.</param>
        /// <param name="Result">Result to substitute.</param>
        /// <param name="Conditions">Conditions to check for using this transform.</param>
        public SubstituteTransform(Expression Pattern, Expression Result, IEnumerable<Expression> PreConditions)
            : base(Pattern, PreConditions)
        {
            result = Result;
        }

        /// <summary>
        /// Construct a new PatternTransform.
        /// </summary>
        /// <param name="Pattern">Pattern to match.</param>
        /// <param name="Result">Result to substitute.</param>
        /// <param name="PreConditions">Conditions to check for using this transform.</param>
        public SubstituteTransform(Expression Pattern, Expression Result, params Expression[] PreConditions) : this(Pattern, Result, PreConditions.AsEnumerable()) { }
    }
}
