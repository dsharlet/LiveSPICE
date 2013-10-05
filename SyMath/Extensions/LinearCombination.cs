using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyMath
{    
    /// <summary>
    /// Decompose an expression into a linear combination of basis variables.
    /// </summary>
    public class LinearCombination
    {
        private class Term
        {
            public Expression b, A;
            public Expression Ab { get { return A * b; } }

            public Term(Expression b) { this.b = b; this.A = Constant.Zero; }

            public override string ToString()
            {
                return Ab.ToString();
            }
        };
        private List<Term> terms;

        private void AddTerm(IEnumerable<Expression> B, Expression t)
        {
            if (t.IsFunctionOf(B))
            {
                foreach (Expression b in B)
                {
                    Expression Tb = t / b;
                    if (!Tb.IsFunctionOf(B))
                    {
                        this[b] = this[b] + Tb;
                        return;
                    }
                }
            }
            this[Constant.One] = this[Constant.One] + t;
        }

        /// <summary>
        /// Basis variables of this linear combination.
        /// </summary>
        public IEnumerable<Expression> Basis { get { return terms.Select(i => i.b); } }

        /// <summary>
        /// Coefficients of this linear combination.
        /// </summary>
        /// <param name="b">Basis variable to access the coefficient for.</param>
        /// <returns></returns>
        public Expression this[Expression b]
        {
            get { return terms.Single(i => i.b.Equals(b)).A; }
            set { terms.SingleOrDefault(i => i.b.Equals(b)).A = value; }
        }

        /// <summary>
        /// Create a new empty linear combination.
        /// </summary>
        /// <param name="B">Basis variables.</param>
        public LinearCombination(IEnumerable<Expression> B)
        {
            // Add terms for the basis.
            terms = B.Select(i => new Term(i)).ToList();
            terms.Add(new Term(Constant.One));
        }

        /// <summary>
        /// Create a new linear combination.
        /// </summary>
        /// <param name="B">Basis variables.</param>
        /// <param name="E">Expression to get coefficients from.</param>
        public LinearCombination(IEnumerable<Expression> B, Expression E) : this(B)
        {
            foreach (Expression t in Add.TermsOf(E.Expand()))
                AddTerm(B, t);
        }

        /// <summary>
        /// Create an Expression equal to this linear combination.
        /// </summary>
        public Expression ToExpression() { return Add.New(terms.Select(i => i.Ab).Except(Constant.Zero)); }
        
        /// <summary>
        /// The pivot is the first term in this expression.
        /// </summary>
        private Term Pivot { get { return terms.FirstOrDefault(i => !i.A.IsZero() && !i.b.Equals(Constant.One)); } }
        public Expression PivotCoefficient { get { return Pivot != null ? Pivot.A : null; } }
        public Expression PivotVariable { get { return Pivot != null ? Pivot.b : null; } }

        /// <summary>
        /// Column index of the pivot in the linear combination.
        /// </summary>
        public int PivotPosition { get { return terms.FindIndex(i => !i.A.IsZero()); } }

        /// <summary>
        /// Solve the expression for the variable v.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public Expression Solve(Expression v)
        {
            // Subtract independent terms.
            Expression R = Add.New(terms.Where(x => !x.b.Equals(v)).Select(x => Unary.Negate(x.Ab)));
            // Divide coefficient from dependent term.
            return R / this[v];
        }
        public Expression SolveForPivot() { return Solve(PivotVariable); }

        public void Scale(Expression R)
        {
            foreach (Term i in terms)
                i.A = i.A * R;
        }

        public void AddScaled(Expression M, LinearCombination S)
        {
            foreach (Expression i in Basis)
                this[i] = this[i] + Binary.Multiply(S[i], M);
        }

        public override string ToString() { return terms.Select(i => i.Ab).Except(Constant.Zero).UnSplit(" + "); }
    }
}
