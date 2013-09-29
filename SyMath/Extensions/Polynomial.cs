using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyMath
{
    /// <summary>
    /// Represents a polynomial of one variable.
    /// </summary>
    public class Polynomial : IEquatable<Polynomial>
    {
        protected Dictionary<int, Expression> coefficients = new Dictionary<int, Expression>();

        /// <summary>
        /// Access the coefficients of this polynomial.
        /// </summary>
        /// <param name="d">Degree of the term to access.</param>
        /// <returns></returns>
        public Expression this[int d]
        {
            get 
            {
                if (coefficients.ContainsKey(d))
                    return coefficients[d];
                else
                    return Constant.Zero;
            }
            set 
            { 
                if (value.IsZero())
                    coefficients.Remove(d); 
                else
                    coefficients[d] = value; 
            }
        }

        /// <summary>
        /// Get the coefficients of this Polynomial.
        /// </summary>
        public IEnumerable<Expression> Coefficients { get { return Enumerable.Range(0, Degree + 1).Select(i => coefficients.ContainsKey(i) ? coefficients[i] : Constant.Zero); } }

        /// <summary>
        /// Convert this Polynomial to an equivalent expression.
        /// </summary>
        public Expression ToExpression(Expression x)
        {
            Expression E = Constant.Zero;
            foreach (KeyValuePair<int, Expression> i in coefficients)
                E = E + i.Value * Power.New(x, Constant.New(i.Key));
            return E;
        }

        /// <summary>
        /// Find the degree of this Polynomial.
        /// </summary>
        public int Degree { get { return coefficients.Any() ? coefficients.Max(i => i.Key) : 0; } }
        
        protected Polynomial(IEnumerable<Expression> Coefficients)
        {
            int n = 0;
            foreach (Expression i in Coefficients)
                this[n++] = i;
        }
        protected Polynomial() { }

        /// <summary>
        /// Construct a polynomial of x from f(x).
        /// </summary>
        /// <param name="f"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        public static Polynomial New(Expression f, Expression x)
        {
            // Match each term to A*x^N where A is constant with respect to x, and N is an integer.
            Variable A = PatternVariable.New("A", i => !i.IsFunctionOf(x));
            Variable N = PatternVariable.New("N", i => (i is Constant) && ((Real)i % 1 == 0));
            Expression TermPattern = Multiply.New(A, Power.New(x, N));

            Polynomial P = new Polynomial();

            foreach (Expression i in Add.TermsOf(f))
            {
                MatchContext Matched = TermPattern.Matches(i, Arrow.New(x, x));
                if (Matched == null)
                    throw new AlgebraException("f is not a polynomial of x.");

                int n = (int)(Real)Matched[N];
                P[n] = P[n] + Matched[A];
            }

            return P;
        }

        public Polynomial Clone() { return new Polynomial(Coefficients); }

        public Expression Factor(Expression x)
        {
            // Check if there is a simple factor of x.
            if (this[0].IsZero())
                return x * new Polynomial(Coefficients.Skip(1)).Factor(x);

            DefaultDictionary<Expression, int> factors = new DefaultDictionary<Expression, int>(0);
            switch (Degree)
            {
                //case 2:
                //    Expression a = this[2];
                //    Expression b = this[1];
                //    Expression c = this[0];

                //    // D = b^2 - 4*a*c
                //    Expression D = Add.New(Multiply.New(b, b), Multiply.New(-4, a, c));
                //    factors[Binary.Divide(Add.New(Unary.Negate(b), Call.Sqrt(D)), Multiply.New(2, a))] += 1;
                //    factors[Binary.Divide(Add.New(Unary.Negate(b), Call.Sqrt(D)), Multiply.New(2, a))] += 1;
                //    break;
                default:
                    return ToExpression(x);
            }

            // Assemble expression from factors.
            return Multiply.New(factors.Select(i => Power.New(Binary.Subtract(x, i.Key), i.Value)));
        }

        /// <summary>
        /// Test if this polynomial is approximately equal to P.
        /// </summary>
        /// <param name="P"></param>
        /// <returns></returns>
        public bool Equals(Polynomial P)
        {
            int d = Degree;
            if (d != P.Degree)
                return false;
            for (int i = 0; i <= d; ++i)
                if (!this[i].Equals(P[i]))
                    return false;
            return true;
        }

        public override bool Equals(object obj)
        {
            if (obj is Polynomial)
                return Equals((Polynomial)obj);
            return base.Equals(obj);
        }
        public override int GetHashCode() { return coefficients.OrderedHashCode(); }
        public override string ToString() { return "(" + coefficients.Select(i => i.Value * (Variable.New("x") ^ i.Key)).UnSplit(" + ") + ")"; }

        public static bool operator ==(Polynomial L, Polynomial R) { return L.Equals(R); }
        public static bool operator !=(Polynomial L, Polynomial R) { return !L.Equals(R); }

        public static Polynomial operator *(Polynomial L, Polynomial R)
        {
            Polynomial P = new Polynomial();
            int D = L.Degree + R.Degree;
            for (int i = 0; i <= D; ++i)
                for (int j = 0; j <= i; ++j)
                    P[i] += L[j] * R[i - j];
            return P;
        }
        public static RationalFunction operator /(Polynomial L, Polynomial R) { return RationalFunction.New(L, R); }
        public static Polynomial operator +(Polynomial L, Polynomial R)
        {
            Polynomial P = new Polynomial();
            int D = Math.Max(L.Degree, R.Degree);
            for (int i = 0; i <= D; ++i)
                P[i] = L[i] + R[i];
            return P;
        }
        public static Polynomial operator -(Polynomial L, Polynomial R)
        {
            Polynomial P = new Polynomial();
            int D = Math.Max(L.Degree, R.Degree);
            for (int i = 0; i <= D; ++i)
                P[i] = L[i] - R[i];
            return P;
        }
        
        public static Polynomial LongDivision(Polynomial n, Polynomial d, out Polynomial r)
        {
            Polynomial q = new Polynomial();
            r = n.Clone();

            while (r.coefficients.Any() && !r[0].Equals(Constant.Zero) && r.Degree >= d.Degree)
            {
                int rd = r.Degree;
                int dd = d.Degree;
                Expression t = r[rd] / d[dd];
                int td = rd - dd;

                // Compute q += t
                q[td] += t;

                // Compute r -= d * t
                for (int i = 0; i <= dd; ++i)
                    r[i + td] -= - d[i] * t;
            }
            return q;
        }
    }
}
