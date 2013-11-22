using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyMath
{    
    /// <summary>
    /// Decompose an expression into a linear combination of basis variables.
    /// </summary>
    public class LinearCombination : Sum
    {
        private DefaultDictionary<Expression, Expression> terms = new DefaultDictionary<Expression,Expression>(0);

        public override IEnumerable<Expression> Terms
        {
            get 
            {
                foreach (KeyValuePair<Expression, Expression> i in terms)
                {
                    if (i.Value.EqualsOne())
                        yield return i.Key;
                    else if (i.Key.EqualsOne())
                        yield return i.Value;
                    else
                        yield return i.Key * i.Value;
                }
            }
        }

        private void AddTerm(IEnumerable<Expression> B, Expression t)
        {
            if (t.DependsOn(B))
            {
                foreach (Expression b in B)
                {
                    Expression Tb = t / b;
                    if (!Tb.DependsOn(B))
                    {
                        terms[b] += Tb;
                        return;
                    }
                }
            }
            terms[1] += t;
        }

        /// <summary>
        /// Basis variables of this linear combination.
        /// </summary>
        public IEnumerable<Expression> Basis { get { return terms.Keys; } }

        /// <summary>
        /// Coefficients of this linear combination.
        /// </summary>
        /// <param name="b">Basis variable to access the coefficient for.</param>
        /// <returns></returns>
        public Expression this[Expression b] { get { return terms[b]; } }

        private LinearCombination() { }
        private LinearCombination(IEnumerable<KeyValuePair<Expression, Expression>> Terms)
        {
            foreach (KeyValuePair<Expression, Expression> i in Terms)
                terms[i.Key] = i.Value;
        }

        public static LinearCombination New(IEnumerable<Expression> A, Expression x)
        {
            LinearCombination ret = new LinearCombination();
            foreach (Expression t in Sum.TermsOf(x.Expand()))
                ret.AddTerm(A, t);
            return ret;
        }

        public static LinearCombination New(IEnumerable<KeyValuePair<Expression, Expression>> Terms) { return new LinearCombination(Terms); }

        /// <summary>
        /// Create an Expression equal to this linear combination.
        /// </summary>
        public Expression ToExpression() { return Sum.New(Terms); }

        public bool DependsOn(IEnumerable<Expression> x) { return ToExpression().DependsOn(x); }
        public bool DependsOn(params Expression[] x) { return DependsOn(x.AsEnumerable()); }

        /// <summary>
        /// The pivot is the first term in this expression.
        /// </summary>
        private KeyValuePair<Expression, Expression> Pivot { get { return terms.FirstOrDefault(i => !i.Value.EqualsZero() && !i.Key.EqualsOne()); } }
        public Expression PivotCoefficient { get { return Pivot.Value; } }
        public Expression PivotVariable { get { return Pivot.Key; } }

        /// <summary>
        /// Solve the expression for the variable v.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public Expression Solve(Expression v)
        {
            // Subtract independent terms.
            Expression R = Sum.New(terms.Where(x => !x.Key.Equals(v)).Select(x => x.Key * x.Value));
            // Divide coefficient from dependent term.
            return R / Unary.Negate(this[v]);
        }
        public Expression SolveForPivot() { return Solve(PivotVariable); }
    }
}
