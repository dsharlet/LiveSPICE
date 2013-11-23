using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ComputerAlgebra
{
    /// <summary>
    /// Interface for a Transform operation.
    /// </summary>
    public interface ITransform
    {
        /// <summary>
        /// Transform Expression E to a new Expression.
        /// </summary>
        /// <param name="E"></param>
        /// <returns></returns>
        Expression Transform(Expression E);
    }

    /// <summary>
    /// Transform operation utilizing ExpressionVisitor.
    /// </summary>
    public abstract class VisitorTransform : ExpressionVisitor<Expression>, ITransform
    {
        protected override Expression VisitUnknown(Expression E) { return E; }

        public Expression Transform(Expression E) { return Visit(E); }
    }

    /// <summary>
    /// Transform operation utilizing RecursiveExpressionVisitor.
    /// </summary>
    public class RecursiveVisitorTransform : RecursiveExpressionVisitor, ITransform
    {
        public Expression Transform(Expression E) { return Visit(E); }
    }
}
