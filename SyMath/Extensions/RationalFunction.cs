using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyMath
{
    /// <summary>
    /// Represents a rational function of one variable.
    /// </summary>
    public class RationalFunction : IEquatable<RationalFunction>
    {
        protected Polynomial numerator, denominator;

        public Polynomial Numerator { get { return numerator; } set { numerator = value; } }
        public Polynomial Denominator { get { return denominator; } set { denominator = value; } }
        
        /// <summary>
        /// Convert this polynomial to an equivalent expression.
        /// </summary>
        public Expression ToExpression(Expression x) 
        { 
            return Numerator.ToExpression(x) / Denominator.ToExpression(x);
        }

        /// <summary>
        /// The degree of the RationalFunction, equivalent to Max(Numerator.Degree, Denominator.Degree).
        /// </summary>
        public int Degree { get { return Math.Max(Numerator.Degree, Denominator.Degree); } }

        protected RationalFunction(Polynomial n, Polynomial d) { numerator = n; denominator = d; }

        /// <summary>
        /// Create a new rational function from the given expression.
        /// </summary>
        /// <param name="f"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        public static RationalFunction New(Expression f, Expression x)
        {
            return new RationalFunction(
                Polynomial.New(Multiply.Numerator(f), x),
                Polynomial.New(Multiply.Denominator(f), x));
        }

        /// <summary>
        /// Create a new rational function from two polynomials.
        /// </summary>
        /// <param name="N"></param>
        /// <param name="D"></param>
        /// <returns></returns>
        public static RationalFunction New(Polynomial N, Polynomial D) { return new RationalFunction(N, D); }

        public bool Equals(RationalFunction R) { return Numerator.Equals(R.Numerator) && Denominator.Equals(R.Denominator); }

        public override bool Equals(object obj)
        {
            if (obj is RationalFunction)
                return Equals((RationalFunction)obj);
            return base.Equals(obj);
        }
        public override int GetHashCode() { return numerator.GetHashCode() ^ denominator.GetHashCode(); }
        public override string ToString() { return Numerator.ToString() + "/" + Denominator.ToString(); }

        public static bool operator ==(RationalFunction L, RationalFunction R) { return L.Equals(R); }
        public static bool operator !=(RationalFunction L, RationalFunction R) { return !L.Equals(R); }
    }
}
