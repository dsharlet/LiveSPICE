using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ComputerAlgebra
{
    public class Constant : Atom, IFormattable
    {
        protected Real x;
        public Real Value { get { return x; } }

        protected Constant(Real x) { this.x = x; }

        private static readonly Constant One = new Constant(1);
        private static readonly Constant Zero = new Constant(0);

        public static Constant New(int x) { return new Constant(x); }
        public static Constant New(double x) { return new Constant(x); }
        public static Constant New(decimal x) { return new Constant(x); }
        public static Constant New(Real x) { return new Constant(x); }
        public static Constant New(bool x) { return x ? One : Zero; }
        public static Expression New(object x) 
        {
            if (x.GetType() == typeof(int)) return New((int)x);
            if (x.GetType() == typeof(double)) return New((double)x);
            if (x.GetType() == typeof(decimal)) return New((decimal)x);
            if (x.GetType() == typeof(bool)) return New((bool)x);
            if (x.GetType() == typeof(Real)) return New((Real)x);
            throw new InvalidCastException();
        }

        public override bool EqualsZero() { return x.EqualsZero(); }
        public override bool EqualsOne() { return x.EqualsOne(); }
        public override bool IsFalse() { return x.EqualsZero(); }
        public override bool IsTrue() { return !x.EqualsZero(); }

        public static implicit operator Real(Constant x) { return x.x; }

        protected override int TypeRank { get { return 0; } }

        // object interface.
        public override int GetHashCode() { return x.GetHashCode(); }
        public override string ToString() { return x.ToString("G6"); }
        public string ToString(string format, IFormatProvider formatProvider) { return x.ToString(format, formatProvider); }

        // Note that this is *not* an arithmetic comparison, it is a canonicalization ordering.
        public override int CompareTo(Expression R)
        {
            Constant RC = R as Constant;
            if (!ReferenceEquals(RC, null))
                return Real.Abs(RC.Value).CompareTo(Real.Abs(Value));

            return base.CompareTo(R);
        }
        public override bool Equals(Expression E)
        {
            Constant C = E as Constant;
            if (ReferenceEquals(C, null)) return false;

            return Value.Equals(C.Value);
        }
    }
}
