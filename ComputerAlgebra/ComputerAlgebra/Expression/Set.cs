using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace ComputerAlgebra
{
    /// <summary>
    /// Represents an unordered collection of elements.
    /// </summary>
    public class Set : Expression
    {
        protected List<Expression> members;
        /// <summary>
        /// Elements contained in this set.
        /// </summary>
        public IEnumerable<Expression> Members { get { return members; } }

        protected Set(IEnumerable<Expression> Members) { members = Members.OrderBy(i => i).ToList(); }

        public static Set New(IEnumerable<Expression> Members) { return new Set(Members); }
        public static Set New(params Expression[] Members) { return new Set(Members); }

        public override bool Matches(Expression Expr, MatchContext Matched)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<Atom> Atoms
        {
            get
            {
                foreach (Expression i in members)
                    foreach (Atom j in i.Atoms)
                        yield return j;
            }
        }
        public override int CompareTo(Expression R)
        {
            Set RS = R as Set;
            if (!ReferenceEquals(RS, null))
                return members.LexicalCompareTo(RS.Members);

            return base.CompareTo(R);
        }

        public override bool Equals(Expression E)
        {
            Set S = E as Set;
            if (ReferenceEquals(S, null)) return false;

            return Members.SequenceEqual(S.Members);
        }
        public override string ToString() { return "{" + members.UnSplit(", ") + "}"; }
        public override int GetHashCode() { return Members.UnorderedHashCode(); }
        
        public static IEnumerable<Expression> MembersOf(Expression E)
        {
            if (E is Set)
                return (E as Set).Members;
            else
                return new Expression[] { E };
        }

        public static Set Union(Set A, Set B) { return Set.New(A.Members.Union(B.Members)); }
        public static Set Intersection(Set A, Set B) { return Set.New(A.Members.Intersect(B.Members)); }
    }
}
