using ComputerAlgebra;
using System;
using System.ComponentModel;

namespace Circuit
{
    [TypeConverter("Circuit.RatioConverter")]
    public class Ratio : IEquatable<Ratio>
    {
        private int n, d;

        public int N { get { return n; } }
        public int D { get { return d; } }

        public Ratio(int N, int D)
        {
            n = N;
            d = D;
        }

        public static Ratio Parse(string s)
        {
            string[] nd = s.Split(':');
            if (nd.Length >= 2)
                return new Ratio(int.Parse(nd[0]), int.Parse(nd[1]));
            else
                throw new ParseException("'" + s + "' is not a ratio.");
        }

        public static bool operator ==(Ratio L, Ratio R) { return L.n * R.d == L.d * R.n; }
        public static bool operator !=(Ratio L, Ratio R) { return !(L == R); }

        public static implicit operator Expression(Ratio x) { return Constant.New(x.n) / Constant.New(x.d); }

        // IEquatable interface.
        public bool Equals(Ratio obj) { return this == obj; }

        // object interface.
        public override bool Equals(object obj) { return obj is Ratio ratio ? Equals(ratio) : base.Equals(obj); }
        public override int GetHashCode() { return n.GetHashCode() * 33 + d.GetHashCode(); }
        public override string ToString() { return n.ToString() + ":" + d.ToString(); }
    }
}
