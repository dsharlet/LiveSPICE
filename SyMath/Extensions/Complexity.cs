using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyMath
{
    /// <summary>
    /// Visitor to estimate the complexity of the visited expression for simplification purposes.
    /// </summary>
    class ComplexityVisitor : ExpressionVisitor<int>
    {
        protected override int VisitUnknown(Expression E) { return 1; }

        protected override int VisitAdd(Add A) { return A.Terms.Sum(i => Visit(i) + 1) - 1; }
        protected override int VisitMultiply(Multiply M) { return M.Terms.Sum(i => Visit(i) + 1) - 1; }
        protected override int VisitCall(Call F) { return F.Arguments.Sum(i => Visit(i)) + 5; }
        protected override int VisitSet(Set S) { return S.Members.Sum(i => Visit(i)); }

        protected override int VisitBinary(Binary B) { return Visit(B.Left) + Visit(B.Right) + 1; }
        protected override int VisitUnary(Unary U) { return Visit(U.Operand) + 1; }
    }

    public static class EstimateComplexityExtension
    {
        private static ComplexityVisitor estimator = new ComplexityVisitor();

        /// <summary>
        /// Estimate the complexity of an expression for simplification purposes.
        /// </summary>
        /// <param name="E"></param>
        /// <returns></returns>
        public static int EstimateComplexity(this Expression E)
        {
            return estimator.Visit(E);
        }
    }
}
