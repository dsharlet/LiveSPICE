using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ComputerAlgebra
{
    /// <summary>
    /// Expression visitor for substiting expressions in for other expressions.
    /// </summary>
    class SubstituteVisitor : RecursiveExpressionVisitor
    {
        protected IDictionary<Expression, Expression> x0;
        protected bool transform;

        public SubstituteVisitor(IDictionary<Expression, Expression> x0, bool IsTransform) { this.x0 = x0; transform = IsTransform; }

        public override Expression Visit(Expression E)
        {
            Expression xE;
            if (x0.TryGetValue(E, out xE))
                return xE;
            else
                return base.Visit(E);
        }

        protected override Expression VisitCall(Call F)
        {
            return F.Target.Substitute(F, x0, transform);
        }
    }

    public static class SubstituteExtension
    {
        /// <summary>
        /// Substitute variables x0 into f.
        /// </summary>
        /// <param name="f"></param>
        /// <param name="x0"></param>
        /// <returns></returns>
        public static Expression Substitute(this Expression f, IDictionary<Expression, Expression> x0, bool IsTransform = false) 
        {
            if (x0.Empty())
                return f;
            else
                return new SubstituteVisitor(x0, IsTransform).Visit(f); 
        }

        /// <summary>
        /// Evaluate an expression at x = x0.
        /// </summary>
        /// <param name="f">Expression to evaluate.</param>
        /// <param name="x">Arrow expressions representing substitutions to evaluate.</param>
        /// <returns>The evaluated expression.</returns>
        public static Expression Substitute(this Expression f, IEnumerable<Arrow> x) { return f.Substitute(x.ToDictionary(i => i.Left, i => i.Right)); }
        public static Expression Substitute(this Expression f, params Arrow[] x) { return f.Substitute(x.AsEnumerable()); }

        /// <summary>
        /// Evaluate an expression at x = x0.
        /// </summary>
        /// <param name="f">Expression to evaluate.</param>
        /// <param name="x">Variable to evaluate at.</param>
        /// <param name="x0">Value to evaluate for.</param>
        /// <returns>The evaluated expression.</returns>
        public static Expression Substitute(this Expression f, Expression x, Expression x0) { return f.Substitute(new Dictionary<Expression, Expression> { { x, x0 } }); }
        public static Expression Substitute(this Expression f, IEnumerable<Expression> x, IEnumerable<Expression> x0) { return f.Substitute(x.Zip(x0, (i, j) => Arrow.New(i, j))); }
    }
}
