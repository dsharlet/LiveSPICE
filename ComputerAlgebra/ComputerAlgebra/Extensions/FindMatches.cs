using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ComputerAlgebra
{
    /// <summary>
    /// Visitor for finding subexpressions in expressions. Returns null if any of the expressions is found.
    /// </summary>
    class FindMatchesVisitor : RecursiveExpressionVisitor
    {
        protected List<MatchContext> matches = new List<MatchContext>();
        protected IEnumerable<Expression> patterns;

        public FindMatchesVisitor(IEnumerable<Expression> Patterns) { patterns = Patterns; }

        public IEnumerable<MatchContext> Matches { get { return matches; } }

        public override Expression Visit(Expression E)
        {
            foreach (Expression i in patterns)
            {
                MatchContext m = i.Matches(E);
                if (m != null)
                {
                    matches.Add(m);
                    return E;
                }
            }
            return base.Visit(E);
        }
    }

    public static class FindMatchesExtension
    {
        /// <summary>
        /// Find any subexpressions of x that match any of the patterns.
        /// </summary>
        /// <param name="f"></param>
        /// <param name="Patterns"></param>
        /// <returns>Enumerable of MatchContext for the successful matches.</returns>
        public static IEnumerable<MatchContext> FindMatches(this Expression f, IEnumerable<Expression> Patterns)
        {
            FindMatchesVisitor V = new FindMatchesVisitor(Patterns);
            V.Visit(f);
            return V.Matches;
        }

        /// <summary>
        /// Find any subexpressions of x that match any of the patterns.
        /// </summary>
        /// <param name="f"></param>
        /// <param name="Patterns"></param>
        /// <returns>Enumerable of MatchContext for the successful matches.</returns>
        public static IEnumerable<MatchContext> FindMatches(this Expression f, params Expression[] Patterns) { return FindMatches(f, Patterns.AsEnumerable()); }
    }
}
