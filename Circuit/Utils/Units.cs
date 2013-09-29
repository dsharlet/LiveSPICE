using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using SyMath;

namespace Circuit
{
    public class Units : IEquatable<Units>
    {
        private int length;
        private int mass;
        private int time;
        private int current;
        
        public Units(int m, int kg, int s, int A)
        {
            length = m;
            mass = kg;
            time = s;
            current = A;
        }
        
        public static Units Parse(ref string s)
        {
            foreach (KeyValuePair<Units, string> i in names)
            {
                if (s.EndsWith(i.Value))
                {
                    s = s.Substring(0, s.Length - i.Value.Length);
                    return i.Key;
                }
            }
            // Some backups for hard to type symbols.
            if (s.ToUpperInvariant().EndsWith("Ohm"))
            {
                s = s.Substring(0, s.Length - 3);
                return Ohm;
            }
            return None;
        }

        public static readonly Units None = new Units(0, 0, 0, 0);

        public static readonly Units m = new Units(1, 0, 0, 0);
        public static readonly Units kg = new Units(0, 1, 0, 0);
        public static readonly Units s = new Units(0, 0, 1, 0);
        public static readonly Units A = new Units(0, 0, 0, 1);

        public static readonly Units Hz = s ^ -1;

        public static readonly Units N = kg * m / s ^ 2;

        public static readonly Units J = N * m;
        public static readonly Units W = J / s;
        public static readonly Units C = A * s;

        public static readonly Units V = W / A;
        public static readonly Units F = C / V;
        public static readonly Units Ohm = V / A;

        public static readonly Units Wb = V * s;
        public static readonly Units H = Wb / A;

        private static Dictionary<Units, string> names = new Dictionary<Units, string>()
        {
            { m, "m" }, { kg, "kg" }, { s, "s" }, { A, "A" },
            { Hz, "Hz" }, 
            { N, "N" }, 
            { J, "J" }, { W, "W" }, 
            { C, "C" }, { V, "V" }, { F, "F" }, { Ohm, "\u2126" },
            { Wb, "Wb" }, { H, "H" },
        };

        public static Units operator ^(Units L, int R)
        {
            return new Units(
                L.length * R,
                L.mass * R,
                L.time * R,
                L.current * R);
        }
        public static Units operator *(Units L, Units R)
        {
            return new Units(
                L.length        + R.length,
                L.mass          + R.mass,
                L.time          + R.time,
                L.current       + R.current);
        }
        public static Units operator /(Units L, Units R) { return L * (R^-1); }
        public static bool operator ==(Units L, Units R)
        {
            return 
                L.length == R.length &&
                L.mass == R.mass &&
                L.time == R.time &&
                L.current == R.current;
        }
        public static bool operator !=(Units L, Units R) { return !(L == R); }

        // IEquatable interface.
        public bool Equals(Units obj) { return this == obj; }

        // object interface.
        public override bool Equals(object obj)
        {
            if (obj is Units)
                return Equals((Units)obj);
            return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return ((length + 8) << 0)
                | ((mass + 8) << 4)
                | ((time + 8) << 8)
                | ((current + 8) << 12);
        }

        public override string ToString()
        {
            if (names.ContainsKey(this))
            {
                return names[this];
            }
            else
            {
                StringBuilder SB = new StringBuilder();

                if (mass == 1) SB.Append("kg");
                else if (mass != 0) SB.Append("kg^" + mass);
                if (length == 1) SB.Append("m");
                else if (length != 0) SB.Append("m^" + length);
                if (time == 1) SB.Append("s");
                else if (time != 0) SB.Append("s^" + time);
                if (current == 1) SB.Append("A");
                else if (current != 0) SB.Append("A^" + current);

                return SB.ToString();
            }
        }
    }
}
