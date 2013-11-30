using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using ComputerAlgebra;

namespace Circuit
{
    public class UnitCastException : InvalidCastException
    {
        public UnitCastException(Units From, Units To) 
            : base("Cannot convert quantity from " + From.ToString() + " to " + To.ToString())
        { }
    }

    [TypeConverter(typeof(QuantityConverter))]
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
            if (x != Value.x)
            {
                x = Value.x;
                return true;
            }
            else
            {
                return false;
            }
        }
                
        public static Quantity Parse(string s)
        {
            Units units = Units.Parse(ref s);
            s = s.TrimEnd();
            Expression prefix = 1;
            for (int i = 0; i < prefixes.Length; ++i)
            {
                string p = prefixes[i];
                if (p != "" && s.EndsWith(p) || (aliases.ContainsKey(p) && s.EndsWith(aliases[p])))
                {
                    s = s.Substring(0, s.Length - p.Length);
                    prefix = (((Real)10) ^ ((i - 5) * 3));
                    break;
                }
            }
            return new Quantity(prefix * Expression.Parse(s), units);
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

        public static implicit operator Expression(Quantity x) { return x.x; }
        public static explicit operator Real(Quantity x) { return (Real)x.x; }
        public static explicit operator double(Quantity x) { return (double)x.x; }

        // IEquatable interface.
        public bool Equals(Quantity obj) { return this == obj; }

        // object interface.
        public override bool Equals(object obj) { return obj is Quantity ? Equals((Quantity)obj) : base.Equals(obj); }
        public override int GetHashCode() { return x.GetHashCode() ^ units.GetHashCode(); }
        public override string ToString() { return ToString("G3", null); }

        // IFormattable interface.
        private static string[] prefixes = { "f", "p", "n", "\u03BC", "m", "", "k", "M", "G", "T" };
        private static Dictionary<string, string> aliases = new Dictionary<string, string>() { { "\u03BC", "u" } };
        public string ToString(string format, IFormatProvider formatProvider) { return ToString(x, units, format, formatProvider); }
        
        public static string ToString(Expression Value, Units Units, string format, IFormatProvider formatProvider)
        {
            StringBuilder SB = new StringBuilder();

            Constant constant = Value as Constant;
            if (constant != null && Units != Units.None)
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
                int prefix = order == Real.Infinity ? 0 : (order < -20 ? 0 : (int)Real.Floor(order / 3));
                prefix = Math.Max(Math.Min(prefix + 5, prefixes.Length - 1), 0) - 5;

                Value = Value / (((Real)10) ^ (prefix * 3));
                if (Value is IFormattable)
                    SB.Append(((IFormattable)Value).ToString(format, formatProvider));
                else
                    SB.Append(Value.ToString());
                SB.Append(" ");
                SB.Append(prefixes[prefix + 5]);
            }
            else if (Value != null)
            {
                SB.Append(Value.ToString());
                SB.Append(" ");
            }
            else
            {
                SB.Append("0");
                SB.Append(" ");
            }
            SB.Append(Units);

            return SB.ToString();
        }

        public static string ToString(Expression Value, Units Units, string Format) { return ToString(Value, Units, Format, null); }
        public static string ToString(Expression Value, Units Units) { return ToString(Value, Units, "G3", null); }
    }
}
