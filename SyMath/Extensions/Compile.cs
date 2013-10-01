using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using LinqExpressions = System.Linq.Expressions;
using LinqExpression = System.Linq.Expressions.Expression;

namespace SyMath
{
    /// <summary>
    /// Visitor to translate Expression to System.Linq.Expressions.Expression.
    /// </summary>
    class CompileVisitor : ExpressionVisitor<LinqExpression>
    {
        protected Dictionary<Expression, LinqExpression> compiled = new Dictionary<Expression,LinqExpression>();

        public CompileVisitor(IDictionary<Expression, LinqExpression> Map)
        {
            if (Map != null)
                foreach (KeyValuePair<Expression, LinqExpression> i in Map)
                    compiled[i.Key] = i.Value;
        }

        public override LinqExpression Visit(Expression E)
        {
            // If this expression has already been compiled, re-use it.
            LinqExpression e = null;
            if (compiled.TryGetValue(E, out e))
                return e;

            // Compile and store the expression.
            e = base.Visit(E);
            compiled[E] = e;
            return e;
        }

        protected override LinqExpression VisitConstant(Constant C) 
        { 
            return LinqExpression.Constant((double)C); 
        }

        protected override LinqExpression VisitAdd(Add A)
        {
            LinqExpression add = Visit(A.Terms.First());
            foreach (Expression i in A.Terms.Skip(1))
                add = LinqExpression.Add(add, Visit(i));
            return add;
        }

        protected override LinqExpression VisitMultiply(Multiply M)
        {
            LinqExpression multiply = Visit(M.Terms.First());
            foreach (Expression i in M.Terms.Skip(1))
                multiply = LinqExpression.Multiply(multiply, Visit(i));
            return multiply;
        }

        protected override LinqExpression VisitUnary(Unary U)
        {
            LinqExpression o = Visit(U.Operand);
            switch (U.Operator)
            {
                case Operator.Negate: return LinqExpression.Negate(o);
                case Operator.Inverse: return LinqExpression.Divide(LinqExpression.Constant(1.0), o);
                case Operator.Not: return LinqExpression.Not(o);

                default: throw new NotImplementedException("Unsupported unary operator");
            }
        }

        protected override LinqExpression VisitBinary(Binary B)
        {
            LinqExpression l = Visit(B.Left);
            LinqExpression r = Visit(B.Right);
            switch (B.Operator)
            {
                case Operator.Add: return LinqExpression.Add(l, r);
                case Operator.Subtract: return LinqExpression.Subtract(l, r);
                case Operator.Multiply: return LinqExpression.Multiply(l, r);
                case Operator.Divide: return LinqExpression.Divide(l, r);
                case Operator.Power: return LinqExpression.Power(l, r);

                case Operator.And: return LinqExpression.And(l, r);
                case Operator.Or: return LinqExpression.Or(l, r);

                case Operator.Equal: return LinqExpression.Equal(l, r);
                case Operator.NotEqual: return LinqExpression.NotEqual(l, r);
                case Operator.Greater: return LinqExpression.GreaterThan(l, r);
                case Operator.GreaterEqual: return LinqExpression.GreaterThanOrEqual(l, r);
                case Operator.Less: return LinqExpression.LessThan(l, r);
                case Operator.LessEqual: return LinqExpression.LessThanOrEqual(l, r);

                default: throw new NotImplementedException("Unsupported binary operator.");
            }
        }

        protected override LinqExpression VisitPower(Power P)
        {
            LinqExpression l = Visit(P.Left);
            if (P.Right.Equals(Constant.New(2)))
                return LinqExpression.Multiply(l, l);

            LinqExpression r = Visit(P.Right);
            return LinqExpression.Power(l, r);
        }

        protected override LinqExpression VisitCall(Call F)
        {
            if (F.Target.CanCall(F.Arguments))
                return F.Target.Compile(F.Arguments.Select(i => Visit(i)));
            else
                throw new InvalidOperationException("Call cannot be resolved.");
        }

        protected override LinqExpression VisitVariable(Variable V)
        {
            throw new InvalidOperationException("Unmapped variable in compiled expression.");
        }

        protected override LinqExpression VisitUnknown(Expression E)
        {
            throw new NotImplementedException("Unsupported expression type.");
        }
    }

    public static class CompileExtension
    {
        /// <summary>
        /// Compile an expression to a LinqExpression.
        /// </summary>
        /// <param name="This"></param>
        /// <param name="Map"></param>
        /// <returns></returns>
        public static LinqExpression Compile(this Expression This, IDictionary<Expression, LinqExpression> Map)
        {
            return new CompileVisitor(Map).Visit(This);
        }

        /// <summary>
        /// Compile an expression to a Lambda.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="This"></param>
        /// <param name="ParameterMap"></param>
        /// <param name="Map"></param>
        /// <returns></returns>
        public static LinqExpressions.Expression<T> Compile<T>(this Expression This, IEnumerable<Expression> ParameterMap, IDictionary<Expression, LinqExpression> Map)
        {
            List<LinqExpressions.ParameterExpression> parameters = new List<LinqExpressions.ParameterExpression>();
            Dictionary<Expression, LinqExpression> map = Map != null ? new Dictionary<Expression,LinqExpression>(Map) : new Dictionary<Expression, LinqExpression>();
            foreach (Expression i in ParameterMap)
            {
                LinqExpressions.ParameterExpression p = LinqExpression.Parameter(typeof(double), i.ToString());
                parameters.Add(p);
                map.Add(i, p);
            }

            return LinqExpression.Lambda<T>(
                Compile(This, map), 
                parameters);
        }

        /// <summary>
        /// Compile an expression to a Lambda.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="This"></param>
        /// <param name="ParameterMap"></param>
        /// <returns></returns>
        public static LinqExpressions.Expression<T> Compile<T>(this Expression This, IEnumerable<Expression> ParameterMap)
        {
            return Compile<T>(This, ParameterMap, null);
        }

        /// <summary>
        /// Compile an expression to a Lambda.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="This"></param>
        /// <param name="ParameterMap"></param>
        /// <returns></returns>
        public static LinqExpressions.Expression<T> Compile<T>(this Expression This, params Expression[] ParameterMap)
        {
            return Compile<T>(This, ParameterMap, null);
        }
    }
}
