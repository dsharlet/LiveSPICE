using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ComputerAlgebra
{
    /// <summary>
    /// Represents a polynomial of one variable.
    /// </summary>
    public class Polynomial : Sum
    {
        protected Dictionary<int, Expression> coefficients = new Dictionary<int, Expression>();
        protected Expression variable;

        public override IEnumerable<Expression> Terms
        {
            get
            {
                foreach (KeyValuePair<int, Expression> i in coefficients)
                    yield return i.Value * Power.New(variable, Constant.New(i.Key));
            }
        }

        public Expression Variable { get { return variable; } }

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
                    return 0;
            }
        }

        /// <summary>
        /// Get the coefficients of this Polynomial.
        /// </summary>
        public IEnumerable<KeyValuePair<int, Expression>> Coefficients { get { return coefficients; } }
        
        /// <summary>
        /// Find the degree of this Polynomial.
        /// </summary>
        public int Degree { get { return coefficients.Max(i => i.Key, 0); } }
        
        protected Polynomial(IEnumerable<Expression> Coefficients, Expression Variable)
        {
            int n = 0;
            foreach (Expression i in Coefficients)
                if (!i.EqualsZero())
                    coefficients[n++] = i;
            variable = Variable;
        }
        protected Polynomial(IEnumerable<KeyValuePair<int, Expression>> Coefficients, Expression Variable)
        {
            coefficients = new Dictionary<int, Expression>();
            foreach (KeyValuePair<int, Expression> i in Coefficients)
                coefficients.Add(i.Key, i.Value);
            variable = Variable;
        }
        protected Polynomial(Dictionary<int, Expression> Coefficients, Expression Variable)
        {
            coefficients = Coefficients;
            variable = Variable;
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
            Variable A = PatternVariable.New("A", i => !i.DependsOn(x));
            Variable N = PatternVariable.New("N", i => (i is Constant) && ((Real)i % 1 == 0));
            Expression TermPattern = Product.New(A, Power.New(x, N));

            DefaultDictionary<int, Expression> P = new DefaultDictionary<int, Expression>(0);

            foreach (Expression i in Sum.TermsOf(f))
            {
                MatchContext Matched = TermPattern.Matches(i, Arrow.New(x, x));
                if (Matched == null)
                    throw new ArgumentException("f is not a polynomial of x.");

                int n = (int)(Real)Matched[N];
                P[n] += Matched[A];
            }

            return new Polynomial(P, x);
        }
                
        public Expression Factor(Expression x)
        {
            // Check if there is a simple factor of x.
            if (this[0].EqualsZero())
                return x * new Polynomial(Coefficients.Where(i => i.Key != 0).ToDictionary(i => i.Key - 1, i => i.Value), x).Factor(x);

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
                    return this;
            }

            // Assemble expression from factors.
            //return Multiply.New(factors.Select(i => Power.New(Binary.Subtract(x, i.Key), i.Value)));
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

        public override bool Equals(Expression E)
        {
            Polynomial P = E as Polynomial;
            if (ReferenceEquals(P, null)) return false;
            
            return Equals(P);
        }
        public override int GetHashCode() { return coefficients.OrderedHashCode() ^ variable.GetHashCode(); }
        public override string ToString() { return "(" + coefficients.Select(i => i.Value * (ComputerAlgebra.Variable.New("x") ^ i.Key)).UnSplit(" + ") + ")"; }

        //public static Polynomial operator *(Polynomial L, Polynomial R)
        //{
        //    if (Equals(L.x, R.x))
        //    {
        //        Dictionary<int, Expression> P = new Dictionary<int, Expression>();
        //        int D = L.Degree + R.Degree;
        //        for (int i = 0; i <= D; ++i)
        //            for (int j = 0; j <= i; ++j)
        //                P[i] += L[j] * R[i - j];
        //        return new Polynomial(P, L.x);
        //    }
        //    else
        //    {
        //        return (Expression)L * (Expression)R;
        //    }
        //}
        //public static RationalFunction operator /(Polynomial L, Polynomial R) { return RationalFunction.New(L, R); }
        //public static Polynomial operator +(Polynomial L, Polynomial R)
        //{
        //    Polynomial P = new Polynomial();
        //    int D = Math.Max(L.Degree, R.Degree);
        //    for (int i = 0; i <= D; ++i)
        //        P[i] = L[i] + R[i];
        //    return P;
        //}
        //public static Polynomial operator -(Polynomial L, Polynomial R)
        //{
        //    Polynomial P = new Polynomial();
        //    int D = Math.Max(L.Degree, R.Degree);
        //    for (int i = 0; i <= D; ++i)
        //        P[i] = L[i] - R[i];
        //    return P;
        //}
        
        public static Polynomial LongDivision(Polynomial N, Polynomial D, out Polynomial R)
        {
            if (!Equals(N.Variable, D.Variable))
                throw new ArgumentException("Dividing polynomials of different variable");

            DefaultDictionary<int, Expression> q = new DefaultDictionary<int, Expression>();
            DefaultDictionary<int, Expression> r = new DefaultDictionary<int, Expression>();
            foreach (KeyValuePair<int, Expression> i in N.Coefficients)
                r.Add(i.Key, i.Value);

            while (r.Any() && !r[0].Equals(0) && r.Keys.Max() + 1 >= D.Degree)
            {
                int rd = r.Keys.Max() + 1;
                int dd = D.Degree;
                Expression t = r[rd] / D[dd];
                int td = rd - dd;

                // Compute q += t
                q[td] += t;

                // Compute r -= d * t
                for (int i = 0; i <= dd; ++i)
                    r[i + td] -= - D[i] * t;
            }
            R = new Polynomial(r, N.Variable);
            return new Polynomial(q, N.Variable);
        }
    }
}
