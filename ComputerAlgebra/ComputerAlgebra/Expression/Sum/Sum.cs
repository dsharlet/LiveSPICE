using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace ComputerAlgebra
{
    /// <summary>
    /// Base class for expressions to sum other expressions.
    /// </summary>
    public abstract class Sum : Expression
    {
        public abstract IEnumerable<Expression> Terms { get; }

        /// <summary>
        /// Create a new sum expression in canonical form.
        /// </summary>
        /// <param name="Terms">The list of terms in the sum expression.</param>
        /// <returns></returns>
        public static Expression New(IEnumerable<Expression> Terms) { return Add.New(Terms); }
        public static Expression New(params Expression[] Terms) { return Add.New(Terms); }
        
        public override bool Matches(Expression E, MatchContext Matched)
        {
            // Move the constants in this pattern to E.
            IEnumerable<Expression> PTerms = Terms;
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
                        if (Matched.TryMatch(() => p.Matches(0, Matched) && P.Matches(E, Matched)))
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
            Sum S = E as Sum;
            if (!ReferenceEquals(S, null))
                return S.Terms;
            else
                return new Expression[] { E };
        }

        // object interface.
        private static int Precedence = Parser.Precedence(Operator.Add);
        public override string ToString() 
        {
            StringBuilder s = new StringBuilder();
            s.Append(Terms.First().ToString(Precedence));
            foreach (Expression i in Terms.Skip(1))
            {
                string si = i.ToString(Precedence);
                string nsi = (-i).ToString(Precedence);
                if (si.Length < nsi.Length)
                    s.Append(" + " + si);
                else
                    s.Append(" - " + nsi);
            }
            return s.ToString();
        }
        public override int GetHashCode() { return Terms.OrderedHashCode(); }
        public override bool Equals(Expression E)
        {
            Sum S = E as Sum;
            if (ReferenceEquals(S, null)) return false;

            return Terms.SequenceEqual(S.Terms);
        }

        public override IEnumerable<Atom> Atoms 
        {
            get
            {
                foreach (Expression i in Terms)
                    foreach (Atom j in i.Atoms)
                        yield return j;
            }
        }
        public override int CompareTo(Expression R) { return Terms.LexicalCompareTo(TermsOf(R)); }
    }
}
