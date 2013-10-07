using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace SyMath
{
    /// <summary>
    /// List of expressions to multiply.
    /// </summary>
    public class Multiply : Expression
    {
        protected List<Expression> terms;
        public ReadOnlyCollection<Expression> Terms { get { return new ReadOnlyCollection<Expression>(terms); } }

        protected Multiply(IEnumerable<Expression> Terms) { terms = Terms.ToList(); }

        private static IEnumerable<Expression> FlattenTerms(IEnumerable<Expression> Terms)
        {
            foreach (Expression i in Terms)
            {
                if (i is Multiply)
                    foreach (Expression j in FlattenTerms(((Multiply)i).Terms))
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
        /// Create a new product expression in canonical form.
        /// </summary>
        /// <param name="Terms">The list of terms in the product expression.</param>
        /// <returns></returns>
        public static Expression New(IEnumerable<Expression> Terms)
        {
            Debug.Assert(!Terms.Contains(null));

            // Canonicalize the terms.
            Terms = CanonicalForm(Terms);
            
            switch (Terms.Count())
            {
                case 0: return Constant.One;
                case 1: return Terms.First();
                default: return new Multiply(Terms);
            }
        }
        public static Expression New(params Expression[] Terms) { return New(Terms.AsEnumerable()); }

        public override bool Matches(Expression E, MatchContext Matched)
        {
            // if E is zero, any term can match to zero to succeed.
            if (E.IsZero())
                return terms.Any(i => i.Matches(Constant.Zero, Matched));

            // Move the constants in this pattern to E.
            IEnumerable<Expression> PTerms = terms;
            IEnumerable<Expression> Constants = PTerms.OfType<Constant>();
            if (Constants.Any())
            {
                E = Binary.Divide(E, New(Constants)).Evaluate();
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
                        if (P.Matches(E / matched, Matched))
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
                        if (Matched.TryMatch(() => p.Matches(Constant.One, Matched) && P.Matches(E, Matched)))
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
        /// Enumerate the multiplication terms of E.
        /// </summary>
        /// <param name="E"></param>
        /// <returns></returns>
        public static IEnumerable<Expression> TermsOf(Expression E)
        {
            Multiply M = E as Multiply;
            if (!ReferenceEquals(M, null))
                return M.terms;
            else
                return new Expression[] { E };
        }

        private static bool IsNegative(Expression x)
        {
            Constant C = Multiply.TermsOf(x).First() as Constant;
            if (C != null)
                return C.Value < 0;
            return false;
        }
        private static bool IsInDenominator(Expression x)
        {
            if (x is Power)
                return IsNegative(((Power)x).Right);
            return false;
        }
        public static Expression Numerator(Expression x) { return Multiply.New(Multiply.TermsOf(x).Where(i => !IsInDenominator(i))); }
        public static Expression Denominator(Expression x) { return Multiply.New(Multiply.TermsOf(x).Where(i => IsInDenominator(i)).Select(i => i ^ -1)); }

        private static int Precedence = Parser.Precedence(Operator.Multiply);
        private static Constant NegativeOne = Constant.New(-1);
        public override string ToString()
        {
            Expression n = -this;

            string s = terms.Select(i => i.ToString(Precedence)).UnSplit("*");
            string ns = Multiply.TermsOf(n).Select(i => i.ToString(Precedence)).UnSplit("*");

            IEnumerable<Expression> t;
            if (s.Length < ns.Length)
                t = terms;
            else
                t = Multiply.TermsOf(n);

            StringBuilder sb = new StringBuilder();

            sb.Append(s.Length < ns.Length ? s : "-" + ns);

            return sb.ToString();
        }
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
