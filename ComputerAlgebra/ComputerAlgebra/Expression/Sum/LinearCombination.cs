using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ComputerAlgebra
{    
    /// <summary>
    /// Decompose an expression into a linear combination of basis variables.
    /// </summary>
    public class LinearCombination
    {
        private class Term
        {
            public Expression b, A;
            public int bh;
            public Expression Ab 
            { 
                get 
                {
                    if (b.EqualsOne())
                        return A;
                    if (A.EqualsZero())
                        return 0;
                    return Product.New(A, b);
                } 
            }

            public Term(Expression b) { this.b = b; this.A = 0; bh = b.GetHashCode(); }

            public override string ToString()
            {
                return Ab.ToString();
            }
        };
        private List<Term> terms;

        private void AddTerm(IEnumerable<Expression> B, Expression t)
        {
            if (t.DependsOn(B))
            {
                foreach (Expression b in B)
                {
                    Expression Tb = t / b;
                    if (!Tb.DependsOn(B))
                    {
                        this[b] += Tb;
                        return;
                    }
                }
            }
            this[1] += t;
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
            get { int bh = b.GetHashCode(); return terms.Single(i => i.bh == bh && i.b.Equals(b)).A; }
            set { int bh = b.GetHashCode(); terms.SingleOrDefault(i => i.bh == bh && i.b.Equals(b)).A = value; }
        }

        private object tag;
        public object Tag { get { return tag; } set { tag = value; } }

        /// <summary>
        /// Create a new empty linear combination.
        /// </summary>
        /// <param name="B">Basis variables.</param>
        public LinearCombination(IEnumerable<Expression> B)
        {
            // Add terms for the basis.
            terms = B.Select(i => new Term(i)).ToList();
            terms.Add(new Term(1));
        }

        /// <summary>
        /// Create a new linear combination.
        /// </summary>
        /// <param name="B">Basis variables.</param>
        /// <param name="E">Expression to get coefficients from.</param>
        public LinearCombination(IEnumerable<Expression> B, Expression E) : this(B)
        {
            foreach (Expression t in Sum.TermsOf(E.Expand()))
                AddTerm(B, t);
        }

        public void SwapColumns(IEnumerable<Expression> NewBasis)
        {
            List<Term> old = terms;
            terms = new List<Term>();
            foreach (Expression i in NewBasis)
                terms.Add(old.Single(j => j.b.Equals(i)));
            terms.Add(old.Single(i => i.b.EqualsOne()));
        }

        /// <summary>
        /// Create an Expression equal to this linear combination.
        /// </summary>
        public Expression ToExpression() { return Sum.New(terms.Select(i => i.Ab).Where(i => !i.EqualsZero())); }

        public bool DependsOn(IEnumerable<Expression> x) { return ToExpression().DependsOn(x); }
        public bool DependsOn(params Expression[] x) { return DependsOn(x.AsEnumerable()); }

        /// <summary>
        /// The pivot is the first term in this expression.
        /// </summary>
        private Term Pivot { get { return terms.FirstOrDefault(i => !i.A.EqualsZero() && !i.b.EqualsOne()); } }
        public Expression PivotCoefficient { get { return Pivot != null ? Pivot.A : null; } }
        public Expression PivotVariable { get { return Pivot != null ? Pivot.b : null; } }

        /// <summary>
        /// Column index of the pivot in the linear combination.
        /// </summary>
        public int PivotPosition { get { return terms.FindIndex(i => !i.A.EqualsZero()); } }

        /// <summary>
        /// Solve the expression for the variable v.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public Expression Solve(Expression v)
        {
            int vh = v.GetHashCode();
            // Subtract independent terms.
            Expression R = Sum.New(terms.Where(x => vh != x.bh || !x.b.Equals(v)).Select(x => x.Ab));
            // Divide coefficient from dependent term.
            return R / Unary.Negate(this[v]);
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

        public override string ToString() { return terms.Select(i => i.Ab).Where(i => !i.EqualsZero()).UnSplit(" + "); }
    }
}
