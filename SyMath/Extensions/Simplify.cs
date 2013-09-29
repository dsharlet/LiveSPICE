using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyMath
{
    /// <summary>
    /// Find the algebraic equivalent with minimum complexity.
    /// </summary>
    class SimplifyVisitor : EvaluateVisitor
    {
        public SimplifyVisitor() { }

        // In the case of revisiting an expression, just return it to avoid stack overflow.
        protected override Expression Revisit(Expression E) { return E; }

        public override Expression Visit(Expression E)
        {
            E = base.Visit(E);

            Expression S = E.AlgebraicEquivalents().ToList().ArgMin(i => i.EstimateComplexity());
            if (!ReferenceEquals(S, E))
                S = Visit(S);
            return S;
        }
    }

    public static class SimplifyExtension
    {
        /// <summary>
        /// Simplify expression x.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static Expression Simplify(this Expression x) { return new SimplifyVisitor().Visit(x); }
    }
}
