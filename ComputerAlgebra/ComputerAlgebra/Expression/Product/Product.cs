using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace ComputerAlgebra
{
    /// <summary>
    /// List of expressions to multiply.
    /// </summary>
    public abstract class Product : Expression
    {
        public abstract IEnumerable<Expression> Terms { get; }
        
        /// <summary>
        /// Create a new product expression in canonical form.
        /// </summary>
        /// <param name="Terms">The list of terms in the product expression.</param>
        /// <returns></returns>
        public static Expression New(IEnumerable<Expression> Terms) { return Multiply.New(Terms); }
        public static Expression New(params Expression[] Terms) { return Multiply.New(Terms); }

        public override bool Matches(Expression E, MatchContext Matched)
        {
            // if E is zero, any term can match to zero to succeed.
            if (E.EqualsZero())
                return Terms.Any(i => i.Matches(0, Matched));

            // Move the constants in this pattern to E.
            IEnumerable<Expression> PTerms = Terms;
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
                        if (Matched.TryMatch(() => p.Matches(1, Matched) && P.Matches(E, Matched)))
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
            Product M = E as Product;
            if (!ReferenceEquals(M, null))
                return M.Terms;
            else
                return new Expression[] { E };
        }

        private static bool IsNegative(Expression x)
        {
            Constant C = Product.TermsOf(x).First() as Constant;
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
        public static Expression Numerator(Expression x) { return Product.New(Product.TermsOf(x).Where(i => !IsInDenominator(i))); }
        public static Expression Denominator(Expression x) { return Product.New(Product.TermsOf(x).Where(i => IsInDenominator(i)).Select(i => i ^ -1)); }

        private static int Precedence = Parser.Precedence(Operator.Multiply);
        public override string ToString() { return Terms.Select(i => i.ToString(Precedence)).UnSplit("*"); }
        public override int GetHashCode() { return Terms.OrderedHashCode(); }
        public override bool Equals(Expression E) 
        {
            Product P = E as Product;
            if (ReferenceEquals(P, null)) return false;
            
            return Terms.SequenceEqual(P.Terms);
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
