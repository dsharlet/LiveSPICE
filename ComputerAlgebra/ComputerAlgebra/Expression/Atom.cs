using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ComputerAlgebra
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
        protected virtual int TypeRank { get { return 3; } }

        public override sealed IEnumerable<Atom> Atoms { get { yield return this; } }

        // object
        public override abstract int GetHashCode();
        public override int CompareTo(Expression R)
        {
            Atom RA = R as Atom;
            if (!ReferenceEquals(RA, null))
                return TypeRank.CompareTo(RA.TypeRank);

            return base.CompareTo(R);
        }
    }

    /// <summary>
    /// Atom with a name.
    /// </summary>
    public class NamedAtom : Atom
    {
        private ulong cmp;

        private string name;
        public string Name { get { return name; } }

        protected NamedAtom(string Name) 
        { 
            name = Name; 

            cmp = 0;
            int max = Math.Min(name.Length, 4);
            for (int i = 0; i < max; ++i)
                cmp |= ((ulong)(byte)name[i]) << ((7 - i) << 4);
        }

        public override string ToString() { return Name; }
        public override int GetHashCode() { return name.GetHashCode(); }
        public override bool Equals(Expression E)
        {
            if (ReferenceEquals(E, null) || GetType() != E.GetType()) return false;
            return name.Equals(((NamedAtom)E).name);
        }
        public override int CompareTo(Expression R)
        {
            NamedAtom RA = R as NamedAtom;
            if (!ReferenceEquals(RA, null))
            {
                int c = TypeRank.CompareTo(RA.TypeRank);
                if (c != 0) return c;

                // Try comparing the first 4 chars of the name.
                c = cmp.CompareTo(RA.cmp);
                if (c != 0) return c;

                // First 4 chars match, need to use the full compare.
                return name.CompareTo(RA.name);
            }

            return base.CompareTo(R);
        }
    }
}
