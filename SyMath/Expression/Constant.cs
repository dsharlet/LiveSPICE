using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyMath
{
    public class Constant : Atom, IFormattable
    {
        protected Real x;
        public Real Value { get { return x; } }

        protected Constant(Real x) { this.x = x; }

        private static readonly Constant One = new Constant(1);
        private static readonly Constant Zero = new Constant(0);
        private static readonly Constant NegativeOne = new Constant(-1);

        public static Constant New(int x) 
        {
            //switch (x)
            //{
            //    case -1: return NegativeOne;
            //    case 0: return Zero;
            //    case 1: return One;
            //}
            return new Constant(new Real(x));
        }
        public static Constant New(double x)
        {
            //if (x == -1.0d) return NegativeOne;
            //if (x == 0.0d) return Zero;
            //if (x == 1.0d) return One;
            return new Constant(new Real(x));
        }
        public static Constant New(decimal x)
        {
            //if (x == -1m) return NegativeOne;
            //if (x == 0m) return Zero;
            //if (x == 1m) return One;
            return new Constant(new Real(x));
        }
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

        public override bool IsZero() { return x == 0; }
        public override bool IsOne() { return x == 1; }
        public override bool IsFalse() { return x == 0; }
        public override bool IsTrue() { return x != 0; }

        public static implicit operator Real(Constant x) { return x.x; }

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
        public override bool Equals(Expression R)
        {
            if (ReferenceEquals(this, R))
                return true;

            Constant RC = R as Constant;
            if (!ReferenceEquals(RC, null))
                return Value == RC.Value;

            return base.Equals(R);
        }
    }
}
