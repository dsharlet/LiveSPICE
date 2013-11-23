using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace ComputerAlgebra
{
    /// <summary>
    /// Trivial implementation of Sum.
    /// </summary>
    class Add : Sum
    {
        protected List<Expression> terms;
        public override IEnumerable<Expression> Terms { get { return terms; } }
        
        private Add(List<Expression> Terms) { terms = Terms; }

        private static IEnumerable<Expression> FlattenTerms(IEnumerable<Expression> Terms)
        {
            foreach (Expression i in Terms)
            {
                if (i is Sum)
                    foreach (Expression j in FlattenTerms(((Sum)i).Terms))
                        yield return j;
                else if (!i.EqualsZero())
                    yield return i;
            }
        }

        /// <summary>
        /// Create a new sum expression in canonical form.
        /// </summary>
        /// <param name="Terms">The list of terms in the sum expression.</param>
        /// <returns></returns>
        public static new Expression New(IEnumerable<Expression> Terms)
        {
            Debug.Assert(!Terms.Contains(null));

            // Canonicalize the terms.
            List<Expression> terms = FlattenTerms(Terms).OrderBy(i => i).ToList();

            switch (terms.Count)
            {
                case 0: return 0;
                case 1: return terms.First();
                default: return new Add(terms);
            }
        }
    }
}
