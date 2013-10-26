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

        public static Constant New(int x) { return new Constant(new Real(x)); }
        public static Constant New(double x) { return new Constant(new Real(x)); }
        public static Constant New(decimal x) { return new Constant(new Real(x)); }
        public static Constant New(bool x) { return new Constant(new Real(x ? 1 : 0)); }
        public static Constant New(Real x) { return new Constant(x); }
        public static Expression New(object x) 
        {
            if (x.GetType() == typeof(int)) return New((int)x);
            if (x.GetType() == typeof(double)) return New((double)x);
            if (x.GetType() == typeof(decimal)) return New((decimal)x);
            if (x.GetType() == typeof(bool)) return New((bool)x);
            if (x.GetType() == typeof(Real)) return New((Real)x);
            throw new InvalidOperationException();
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

        public override int CompareTo(Expression R)
        {
            Constant RC = R as Constant;
            if (!ReferenceEquals(RC, null))
                return Real.Abs(RC.Value).CompareTo(Real.Abs(Value));
                //return RC.Value.CompareTo(Value);

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
        
        public static readonly Expression Zero = Constant.New(0);
        public static readonly Expression One = Constant.New(1);
        public static readonly Expression NegativeOne = Constant.New(-1);
        public static readonly Expression Infinity = Constant.New(Real.Infinity);
    }
}
