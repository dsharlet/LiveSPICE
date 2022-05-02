using ComputerAlgebra;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Circuit
{
    public class UnitCastException : InvalidCastException
    {
        public UnitCastException(Units From, Units To)
            : base("Cannot convert quantity from '" + From.ToString() + "' to '" + To.ToString() + "'")
        { }
    }

    [TypeConverter("Circuit.QuantityConverter")]
    public class Quantity : IEquatable<Quantity>, IFormattable
    {
        private Expression x = 0;

        private Units units;
        public Units Units { get { return units; } set { units = value; } }

        public Quantity(Expression x, Units Units)
        {
            this.x = x;
            this.units = Units;
        }
        public Quantity() { units = null; }

        // Set the value of this quantity from another quantity, validating the units.
        public bool Set(Quantity Value)
        {
            if (Value.Units != units && Value.Units != Units.None)
                throw new UnitCastException(Value.Units, units);
            if (!Equals(x, Value.x))
            {
                x = Value.x;
                return true;
            }
            else
            {
                return false;
            }
        }

        private static Expression ParsePrefix(ref string s)
        {
            s = s.TrimEnd();
            if (s.Length < 2)
                return 1;

            if (Prefixes.TryGetValue(DeAliasPrefix(s[s.Length - 1].ToString()), out int prefix))
            {
                // Make sure this isn't just part of the word before the prefix.
                if (char.IsDigit(s[s.Length - 2]) || char.IsWhiteSpace(s[s.Length - 2]))
                {
                    s = s.Substring(0, s.Length - 1);
                    return ((Real)10) ^ prefix;
                }
            }
            return 1;
        }

        public static Quantity Parse(string s, CultureInfo culture)
        {
            Expression prefix = ParsePrefix(ref s);
            Units units = Units.None;
            if (prefix.EqualsOne())
            {
                units = Units.Parse(ref s);
                s = s.TrimEnd();
                prefix = ParsePrefix(ref s);
            }
            return new Quantity(prefix * Expression.Parse(s, culture), units);
        }

        public static Quantity Parse(string s, Units ExpectedUnits)
        {
            Quantity m = Parse(s);
            if (m.Units == Units.None)
                m.Units = ExpectedUnits;
            else if (m.Units != ExpectedUnits)
                throw new UnitCastException(m.Units, ExpectedUnits);
            return m;
        }

        public static Quantity Parse(string s) { return Parse(s, CultureInfo.InstalledUICulture); }

        public static implicit operator Expression(Quantity x) { return x.x; }
        public static implicit operator LazyExpression(Quantity x) { return new LazyExpression(x.x); }
        public static explicit operator Real(Quantity x) { return (Real)x.x; }
        public static explicit operator double(Quantity x) { return (double)x.x; }

        // IEquatable interface.
        public bool Equals(Quantity obj) { return this == obj; }

        // object interface.
        public override bool Equals(object obj) { return obj is Quantity quantity ? Equals(quantity) : base.Equals(obj); }
        public override int GetHashCode() { return x.GetHashCode() ^ units.GetHashCode(); }
        public override string ToString() { return ToString("G3", null); }

        // IFormattable interface.
        private static Dictionary<string, int> Prefixes = new Dictionary<string, int>()
        {
            { "f", -15 },
            { "p", -12 },
            { "n", -9 },
            { "\u03BC", -6 },
            { "m", -3 },
            { "", 0 },
            { "k", 3 },
            { "M", 6 },
            { "G", 9 },
            { "T", 12 },
        };
        private static int MinPrefix = Prefixes.Values.Min();
        private static int MaxPrefix = Prefixes.Values.Max();
        private static string DeAliasPrefix(string Prefix)
        {
            switch (Prefix)
            {
                case "u": return "\u03BC";
                default: return Prefix;
            }
        }

        public string ToString(string format, IFormatProvider formatProvider) { return ToString(x, units, format, formatProvider); }

        public static string ToString(Expression Value, Units Units, string format, IFormatProvider formatProvider)
        {
            StringBuilder SB = new StringBuilder();

            Constant constant = Value as Constant;
            if (constant != null)
            {
                if (format != null && format.StartsWith("+"))
                {
                    if (constant.Value >= 0)
                        SB.Append("+");
                    format = format.Remove(0, 1);
                }

                // Find out how many digits the format has.
                double round = 1.0;
                for (int significant = 12; significant > 0; --significant)
                {
                    round = 1.0 + 5 * Math.Pow(10.0, -significant);

                    if (double.Parse(round.ToString(format)) != 1.0)
                        break;
                }

                // Find the order of magnitude of the value.
                Real order = Real.Log10(Real.Abs(constant.Value.IsNaN() ? 0 : constant.Value) * round);
                if (order < -20) order = 0;
                else if (order == Real.Infinity) order = 0;

                int prefix = Math.Max(Math.Min((int)Real.Floor(order / 3) * 3, MaxPrefix), MinPrefix);

                Value /= (((Real)10) ^ prefix);
                SB.Append(Value.ToString(format, formatProvider));
                SB.Append(" ");
                SB.Append(Prefixes.Single(i => i.Value == prefix).Key);
            }
            else if (Value != null)
            {
                SB.Append(Convert.ToString((object)Value, formatProvider));
                SB.Append(" ");
            }
            else
            {
                SB.Append("0");
                SB.Append(" ");
            }
            SB.Append(Units);

            return SB.ToString().Trim();
        }

        public static string ToString(Expression Value, Units Units, string Format) { return ToString(Value, Units, Format, null); }
        public static string ToString(Expression Value, Units Units) { return ToString(Value, Units, "G3", null); }
    }
}
