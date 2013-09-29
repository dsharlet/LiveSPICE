using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;

namespace SyMath
{
    /// <summary>
    /// Arbitrary precision real number. Represents numbers as close to exactly as possible.
    /// </summary>
    public struct Real : IComparable<Real>, IEquatable<Real>
    {
        private BigRational? rational;
        private double? real;

        public Real(int x) { this.rational = new BigRational(x); this.real = null; }
        public Real(decimal x) { this.rational = new BigRational(x); this.real = null; }
        public Real(BigInteger x) { this.rational = x; this.real = null; }
        public Real(BigRational x) { this.rational = x; this.real = null; }
        public Real(double x) 
        {
            if (x % 1 == 0)
            {
                this.rational = new BigRational(x);
                this.real = null;
            }
            else
            {
                this.real = x;
                this.rational = null;
            }
        }

        public static explicit operator int(Real x)
        {
            if (x.rational != null) return (int)x.rational.Value;
            else if (x.real != null) return (int)x.real.Value;
            else throw new InvalidCastException();
        }
        public static explicit operator double(Real x)
        {
            if (x.rational != null) return (double)x.rational.Value;
            else if (x.real != null) return x.real.Value;
            else throw new InvalidCastException();
        }
        public static explicit operator decimal(Real x)
        {
            if (x.rational != null) return (decimal)x.rational.Value;
            else if (x.real != null) return (decimal)x.real.Value;
            else throw new InvalidCastException();
        }
        public static explicit operator BigInteger(Real x)
        {
            if (x.rational != null) return (BigInteger)x.rational.Value;
            else if (x.real != null) return (BigInteger)x.real.Value;
            else throw new InvalidCastException();
        }
        public static explicit operator BigRational(Real x)
        {
            if (x.rational != null) return x.rational.Value;
            else if (x.real != null) return (BigRational)x.real.Value;
            else throw new InvalidCastException();
        }

        public static implicit operator Real(int x) { return new Real(x); }
        public static implicit operator Real(double x) { return new Real(x); }
        public static implicit operator Real(decimal x) { return new Real(x); }
        public static implicit operator Real(BigRational x) { return new Real(x); }
               
        // Relational operators.
        public static bool operator ==(Real a, Real b) { return a.CompareTo(b) == 0; }
        public static bool operator !=(Real a, Real b) { return a.CompareTo(b) != 0; }
        public static bool operator <(Real a, Real b) { return a.CompareTo(b) < 0; }
        public static bool operator <=(Real a, Real b) { return a.CompareTo(b) <= 0; }
        public static bool operator >(Real a, Real b) { return a.CompareTo(b) > 0; }
        public static bool operator >=(Real a, Real b) { return a.CompareTo(b) >= 0; }
        
        // Arithmetic operators.
        public static Real operator +(Real a, Real b) 
        {
            if (a.rational != null && b.rational != null)
                return new Real(a.rational.Value + b.rational.Value);
            return new Real((double)a + (double)b); 
        }
        public static Real operator -(Real a, Real b)
        {
            if (a.rational != null && b.rational != null)
                return new Real(a.rational.Value - b.rational.Value);
            return new Real((double)a - (double)b);
        }
        public static Real operator *(Real a, Real b)
        {
            if (a.rational != null && b.rational != null)
                return new Real(a.rational.Value * b.rational.Value);
            return new Real((double)a * (double)b);
        }
        public static Real operator %(Real a, Real b)
        {
            if (a.rational != null && b.rational != null)
                return new Real(a.rational.Value % b.rational.Value);
            return new Real((double)a % (double)b);
        }
        public static Real operator /(Real a, Real b)
        {
            if (a.rational != null && b.rational != null)
                return new Real(a.rational.Value / b.rational.Value);
            return new Real((double)a / (double)b);
        }
        public static Real operator ^(Real a, Real b)
        {
            if (a.rational != null && b.rational != null)
                return new Real(a.rational.Value ^ b.rational.Value);
            return new Real(Math.Pow((double)a, (double)b));
        }
        public static Real operator -(Real x) 
        { 
            if (x.rational != null)
                return new Real(-x.rational.Value);
            return new Real(-(double)x);
        }
        
        // Math functions
        public static Real Abs(Real x) { return x > 0 ? x : -x; }
        public static Real Sign(Real x) { return new Real(x < 0 ? -1 : 1); }

        public static Real Min(Real a, Real b) { return a < b ? a : b; }
        public static Real Max(Real a, Real b) { return a > b ? a : b; }

        public static Real Sin(Real x) { return new Real(Math.Sin((double)x)); }
        public static Real Cos(Real x) { return new Real(Math.Cos((double)x)); }
        public static Real Tan(Real x) { return new Real(Math.Tan((double)x)); }
        public static Real Sec(Real x) { return 1 / Cos(x); }
        public static Real Csc(Real x) { return 1 / Sin(x); }
        public static Real Cot(Real x) { return 1 / Tan(x); }

        public static Real ArcSin(Real x) { return new Real(Math.Asin((double)x)); }
        public static Real ArcCos(Real x) { return new Real(Math.Acos((double)x)); }
        public static Real ArcTan(Real x) { return new Real(Math.Atan((double)x)); }
        public static Real ArcSec(Real x) { return ArcCos(1 / x); }
        public static Real ArcCsc(Real x) { return ArcSin(1 / x); }
        public static Real ArcCot(Real x) { return ArcTan(1 / x); }

        public static Real Sinh(Real x) { return new Real(Math.Sinh((double)x)); }
        public static Real Cosh(Real x) { return new Real(Math.Cosh((double)x)); }
        public static Real Tanh(Real x) { return new Real(Math.Tanh((double)x)); }
        public static Real Sech(Real x) { return 1 / Cosh(x); }
        public static Real Csch(Real x) { return 1 / Sinh(x); }
        public static Real Coth(Real x) { return 1 / Tanh(x); }

        public static Real ArcSinh(Real x) { throw new NotImplementedException(); }
        public static Real ArcCosh(Real x) { throw new NotImplementedException(); }
        public static Real ArcTanh(Real x) { throw new NotImplementedException(); }
        public static Real ArcSech(Real x) { return ArcCosh(1 / x); }
        public static Real ArcCsch(Real x) { return ArcSinh(1 / x); }
        public static Real ArcCoth(Real x) { return ArcTanh(1 / x); }

        public static Real Sqrt(Real x) { return new Real(Math.Sqrt((double)x)); }
        public static Real Exp(Real x) { return new Real(Math.Exp((double)x)); }
        public static Real Ln(Real x) { return new Real(Math.Log((double)x)); }
        public static Real Log(Real x, Real b) { return new Real(Math.Log((double)x, (double)b)); }
        public static Real Log10(Real x) { return new Real(Math.Log((double)x, 10.0)); }
        
        public static Real Floor(Real x) 
        {
            if (x.rational != null)
                return new Real(BigRational.Floor(x.rational.Value));
            return new Real(Math.Floor((double)x)); 
        }
        public static Real Ceiling(Real x)
        {
            if (x.rational != null)
                return new Real(BigRational.Ceiling(x.rational.Value));
            return new Real(Math.Ceiling((double)x)); 
        }
        public static Real Round(Real x)
        {
            if (x.rational != null)
                return new Real(BigRational.Round(x.rational.Value));
            return new Real(Math.Round((double)x)); 
        }
        
        // IComparable interface.
        public int CompareTo(Real x)
        {
            if (rational != null && x.rational != null)
                return rational.Value.CompareTo(x.rational.Value);
            return ((double)this).CompareTo((double)x);
        }

        // IEquatable interface.
        public bool Equals(Real x)
        {
            if (rational != null && x.rational != null)
                return rational.Equals(x.rational);
            else if (real != null && x.real != null)
                return real.Equals(x.real);
            else
                return CompareTo(x) == 0;
        }

        // IFormattable interface.
        public string ToString(string format, IFormatProvider formatProvider)
        {
            return rational != null ? rational.Value.ToString(format, formatProvider) : real.Value.ToString(format);
        }
        public string ToString(string format) { return ToString(format, null); }
         
        // object interface.
        public override bool Equals(object obj) { return obj is Real ? Equals((Real)obj) : base.Equals(obj); }
        public override int GetHashCode() { return rational != null ? rational.GetHashCode() : real.GetHashCode(); }
        public override string ToString() { return rational != null ? rational.ToString() : real.ToString(); }
    }
}
