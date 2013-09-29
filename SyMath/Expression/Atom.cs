using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyMath
{
    /// <summary>
    /// An Expression class that has Atoms = { this }. Atom classes will always compare to other atoms successfully.
    /// </summary>
    public abstract class Atom : Expression
    {
        private static List<Type> TypeOrder = new List<Type>()
        {
            typeof(Constant),
            typeof(Variable),
            typeof(Call),
            typeof(Atom),
        };
        protected int TypeRank()
        {
            int Rank = -1;
            Type T = GetType();
            do
            {
                Rank = TypeOrder.IndexOf(T);
                T = T.BaseType;
            } while (Rank < 0);
            return Rank;
        }

        public override sealed IEnumerable<Atom> Atoms { get { yield return this; } }
        public override abstract int GetHashCode();
        public override int CompareTo(Expression R)
        {
            Atom RA = R as Atom;
            if (!ReferenceEquals(RA, null))
                return LexicalCompareTo(() => TypeRank().CompareTo(RA.TypeRank()));

            return base.CompareTo(R);
        }
    }

    /// <summary>
    /// Atom with a name.
    /// </summary>
    public class NamedAtom : Atom
    {
        private string name;
        public string Name { get { return name; } }

        protected NamedAtom(string Name) { name = Name; }

        public override string ToString() { return Name; }
        public override int GetHashCode() { return name.GetHashCode(); }
        public override int CompareTo(Expression R)
        {
            NamedAtom RA = R as NamedAtom;
            if (!ReferenceEquals(RA, null))
                return LexicalCompareTo(
                    () => TypeRank().CompareTo(RA.TypeRank()),
                    () => Name.CompareTo(RA.Name));

            return base.CompareTo(R);
        }
    }
}
