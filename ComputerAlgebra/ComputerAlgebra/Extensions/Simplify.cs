using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ComputerAlgebra
{
    /// <summary>
    /// Visitor to estimate the cost of the visited expression for simplification/optimization purposes.
    /// </summary>
    public class CostVisitor : ExpressionVisitor<int>
    {
        protected override int VisitUnknown(Expression E) { return 1; }

        protected virtual int Cost(Function F) { return 5; }

        protected override int VisitSum(Sum A) { return A.Terms.Sum(i => Visit(i) + 1) - 1; }
        protected override int VisitProduct(Product M) { return M.Terms.Sum(i => Visit(i) + 1) - 1; }
        protected override int VisitCall(Call F) { return F.Arguments.Sum(i => Visit(i)) + Cost(F.Target); }
        protected override int VisitSet(Set S) { return S.Members.Sum(i => Visit(i)); }

        protected override int VisitBinary(Binary B) { return Visit(B.Left) + Visit(B.Right) + 1; }
        protected override int VisitUnary(Unary U) { return Visit(U.Operand) + 1; }
    }

    /// <summary>
    /// Find the algebraic equivalent with minimum complexity.
    /// </summary>
    class SimplifyVisitor : EvaluateVisitor
    {
        protected CostVisitor cost;

        public SimplifyVisitor(CostVisitor Cost) { cost = Cost; }

        // In the case of revisiting an expression, just return it to avoid stack overflow.
        protected override Expression Revisit(Expression E) { return E; }

        public override Expression Visit(Expression E)
        {
            E = base.Visit(E);

            Expression S = E.AlgebraicEquivalents().ToList().ArgMin(i => cost.Visit(i));
            if (!ReferenceEquals(S, E))
                S = Visit(S);
            return S;
        }
    }

    public static class SimplifyExtension
    {
        private static CostVisitor complexity = new CostVisitor();

        /// <summary>
        /// Simplify expression x.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static Expression Simplify(this Expression x) { return new SimplifyVisitor(complexity).Visit(x); }

        /// <summary>
        /// Simplify expression x.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static Expression Simplify(this Expression x, CostVisitor Cost) { return new SimplifyVisitor(Cost).Visit(x); }
    }
}
