using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace ComputerAlgebra
{
    /// <summary>
    /// Trivial implementation of Product.
    /// </summary>
    class Multiply : Product
    {
        protected List<Expression> terms;
        public override IEnumerable<Expression> Terms { get { return terms; } }
        
        private Multiply(List<Expression> Terms) { terms = Terms; }

        private static IEnumerable<Expression> FlattenTerms(IEnumerable<Expression> Terms)
        {
            foreach (Expression i in Terms)
            {
                if (i is Product)
                    foreach (Expression j in FlattenTerms(((Product)i).Terms))
                        yield return j;
                else if (!i.EqualsOne())
                    yield return i;
            }
        }

        /// <summary>
        /// Create a new product expression in canonical form.
        /// </summary>
        /// <param name="Terms">The list of terms in the product expression.</param>
        /// <returns></returns>
        public static new Expression New(IEnumerable<Expression> Terms)
        {
            Debug.Assert(!Terms.Contains(null));

            // Canonicalize the terms.
            List<Expression> terms = FlattenTerms(Terms).OrderBy(i => i).ToList();

            switch (terms.Count)
            {
                case 0: return 1;
                case 1: return terms.First();
                default: return new Multiply(terms);
            }
        }
    }
}
