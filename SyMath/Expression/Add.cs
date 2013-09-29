using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace SyMath
{
    /// <summary>
    /// List of expressions to add.
    /// </summary>
    public class Add : Expression
    {
        protected List<Expression> terms;
        public ReadOnlyCollection<Expression> Terms { get { return new ReadOnlyCollection<Expression>(terms); } }
        
        protected Add(IEnumerable<Expression> Terms) { terms = Terms.ToList(); }

        private static IEnumerable<Expression> FlattenTerms(IEnumerable<Expression> Terms)
        {
            foreach (Expression i in Terms)
            {
                if (i is Add)
                    foreach (Expression j in FlattenTerms(((Add)i).Terms))
                        yield return j;
                else
                    yield return i;
            }
        }
        private static IEnumerable<Expression> CanonicalForm(IEnumerable<Expression> Terms)
        {
            return FlattenTerms(Terms).OrderBy(i => i);
        }

        /// <summary>
        /// Create a new sum expression in canonical form.
        /// </summary>
        /// <param name="Terms">The list of terms in the sum expression.</param>
        /// <returns></returns>
        public static Expression New(IEnumerable<Expression> Terms)
        {
            Debug.Assert(!Terms.Contains(null));

            // Canonicalize the terms.
            Terms = CanonicalForm(Terms);

            switch (Terms.Count())
            {
                case 0: return Constant.Zero;
                case 1: return Terms.First();
                default: return new Add(Terms);
            }
        }
        public static Expression New(params Expression[] Terms) { return New(Terms.AsEnumerable()); }
        
        public override bool Matches(Expression E, MatchContext Matched)
        {
            // Move the constants in this pattern to E.
            IEnumerable<Expression> PTerms = terms;
            IEnumerable<Expression> Constants = PTerms.OfType<Constant>();
            if (Constants.Any())
            {
                E = Binary.Subtract(E, New(Constants)).Evaluate();
                PTerms = PTerms.ExceptUnique(Constants, RefComparer);
            }
            
            IEnumerable<Expression> ETerms = TermsOf(E);

            // Try starting the match at each term of the pattern.
            foreach (Expression p in PTerms)
            {
                // Remaining terms of the pattern.
                Expression P = New(PTerms.ExceptUnique(p, RefComparer));

                // If p is a variable, we have to handle the possibility that more than one term of E might match this term.
                if (p is Variable)
                {
                    // Check if p has already been matched. If it has, treat it as a constant and match the rest of the terms.
                    Expression matched;
                    if (Matched.TryGetValue(p, out matched))
                    {
                        // p has already been matched. Remove it out of E and match the remainder of the pattern.
                        if (P.Matches(Binary.Subtract(E, matched).Evaluate(), Matched))
                            return true;
                    }
                    else
                    {
                        // Try matching p to the various combinations of the terms of E.
                        for (int i = 1; i <= ETerms.Count(); ++i)
                        {
                            foreach (IEnumerable<Expression> e in ETerms.Combinations(i))
                                if (Matched.TryMatch(() =>
                                    p.Matches(New(e), Matched) &&
                                    P.Matches(New(ETerms.ExceptUnique(e, RefComparer)), Matched)))
                                    return true;
                        }

                        // Try matching p to identity.
                        if (Matched.TryMatch(() => p.Matches(Constant.Zero, Matched) && P.Matches(E, Matched)))
                            return true;
                    }
                }
                else
                {
                    // If p is not a variable, try matching it to any of the terms of E.
                    foreach (Expression e in ETerms)
                        if (Matched.TryMatch(() =>
                            p.Matches(e, Matched) &&
                            P.Matches(New(ETerms.ExceptUnique(e, RefComparer)), Matched)))
                            return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Enumerate the addition terms of E.
        /// </summary>
        /// <param name="E"></param>
        /// <returns></returns>
        public static IEnumerable<Expression> TermsOf(Expression E)
        {
            Add A = E as Add;
            if (!ReferenceEquals(A, null))
                return A.terms;
            else
                return new Expression[] { E };
        }

        // object interface.
        public override string ToString() { return "(" + terms.UnSplit(" + ") + ")"; }
        public override bool Equals(Expression E) { return ReferenceEquals(this, E) || terms.SequenceEqual(TermsOf(E)); }
        public override int GetHashCode() { return terms.OrderedHashCode(); }

        public override IEnumerable<Atom> Atoms 
        {
            get
            {
                foreach (Expression i in terms)
                    foreach (Atom j in i.Atoms)
                        yield return j;
            }
        }
        public override int CompareTo(Expression R) { return terms.LexicalCompareTo(TermsOf(R)); }
    }
}
