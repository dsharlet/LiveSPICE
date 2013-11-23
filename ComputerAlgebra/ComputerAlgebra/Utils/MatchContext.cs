using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ComputerAlgebra
{
    /// <summary>
    /// Store matched variables with the ability to revert to a previous match state upon matching failure.
    /// </summary>
    public class MatchContext : Dictionary<Expression, Expression>
    {
        private List<Expression> history = new List<Expression>();

        private Expression matched;
        public Expression Matched { get { return matched; } }

        /// <summary>
        /// Create a match context with conditions.
        /// </summary>
        /// <param name="Conditions"></param>
        public MatchContext(Expression Matching, IEnumerable<Arrow> PreMatch)
        {
            matched = Matching;
            foreach (Arrow i in PreMatch)
                if (!Matches(i.Left, i.Right))
                    throw new InvalidOperationException("Duplicate prematch failed.");
        }
        public MatchContext(Expression Matching, params Arrow[] PreMatch) : this(Matching, PreMatch.AsEnumerable()) { }

        /// <summary>
        /// Check if Key has already been matched to Value. If not, store it as the match.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Value"></param>
        /// <returns>false if Key was already matched to an expression other than Value, true otherwise.</returns>
        public bool Matches(Expression Key, Expression Value)
        {
            Expression Matched;
            if (TryGetValue(Key, out Matched))
                return Matched.Equals(Value);

            this[Key] = Value;
            history.Add(Key);
            return true;
        }

        /// <summary>
        /// Store the state of the matching context and attempt to execute a function F. If the function returns false, the saved state is restored.
        /// </summary>
        /// <param name="F">The function to execute.</param>
        /// <returns>The result of F.</returns>
        public bool TryMatch(Func<bool> F)
        {
            // Remember where this context begins.
            int at = history.Count;
            if (F())
                return true;

            // Match failed, remove any matches since the beginning of this context.
            for (int i = at; i < history.Count; ++i)
                Remove(history[i]);
            history.RemoveRange(at, history.Count - at);
            return false;
        }
    }
}
