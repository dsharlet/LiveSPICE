using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using LinqExprs = System.Linq.Expressions;
using LinqExpr = System.Linq.Expressions.Expression;

namespace ComputerAlgebra.LinqCompiler
{
    /// <summary>
    /// Exception thrown when the compiler encounters an undefined variable.
    /// </summary>
    public class UndefinedVariable : UnresolvedName
    {
        public UndefinedVariable(string Name) : base(Name) { }
    }

    /// <summary>
    /// Visitor to generate LINQ expressions for our Expressions.
    /// </summary>
    class CompileVisitor : ExpressionVisitor<LinqExpr>
    {
        private CodeGen target;
        
        public CompileVisitor(CodeGen Target) { target = Target; }
        
        public override LinqExpr Visit(Expression E)
        {
            // Check if this expression has already been compiled to an intermediate expression or otherwise.
            LinqExpr ret = target.LookUp(E);
            if (ret != null)
                return ret;

            return base.Visit(E);
        }

        protected override LinqExpr VisitSum(Sum A)
        {
            LinqExpr _int = Visit(A.Terms.First());
            foreach (Expression i in A.Terms.Skip(1))
            {
                if (IsNegative(i))
                    _int = LinqExpr.Subtract(_int, Visit(-i));
                else
                    _int = LinqExpr.Add(_int, Visit(i));
            }
            return Int(A, _int);
        }

        protected override LinqExpr VisitProduct(Product M)
        {
            LinqExpr _int = Visit(M.Terms.First());
            foreach (Expression i in M.Terms.Skip(1))
                _int = LinqExpr.Multiply(_int, Visit(i));
            return Int(M, _int);
        }

        protected override LinqExpr VisitUnary(Unary U)
        {
            LinqExpr o = Visit(U.Operand);
            switch (U.Operator)
            {
                case Operator.Negate: return Int(U, LinqExpr.Negate(o));
                case Operator.Inverse: return Int(U, LinqExpr.Divide(LinqExpr.Constant(1.0), o));
                case Operator.Not: return Int(U, LinqExpr.Not(o));

                default: throw new NotSupportedException("Unsupported unary operator '" + U.Operator.ToString() + "'.");
            }
        }

        protected override LinqExpr VisitBinary(Binary B)
        {
            LinqExpr l = Visit(B.Left);
            LinqExpr r = Visit(B.Right);
            switch (B.Operator)
            {
                case Operator.Add: return Int(B, LinqExpr.Add(l, r));
                case Operator.Subtract: return Int(B, LinqExpr.Subtract(l, r));
                case Operator.Multiply: return Int(B, LinqExpr.Multiply(l, r));
                case Operator.Divide: return Int(B, LinqExpr.Divide(l, r));
                case Operator.Power: return Int(B, LinqExpr.Power(l, r));

                case Operator.And: return Int(B, LinqExpr.And(l, r));
                case Operator.Or: return Int(B, LinqExpr.Or(l, r));

                case Operator.Equal: return Int(B, LinqExpr.Equal(l, r));
                case Operator.NotEqual: return Int(B, LinqExpr.NotEqual(l, r));
                case Operator.Greater: return Int(B, LinqExpr.GreaterThan(l, r));
                case Operator.GreaterEqual: return Int(B, LinqExpr.GreaterThanOrEqual(l, r));
                case Operator.Less: return Int(B, LinqExpr.LessThan(l, r));
                case Operator.LessEqual: return Int(B, LinqExpr.LessThanOrEqual(l, r));

                default: throw new NotSupportedException("Unsupported binary operator '" + B.Operator.ToString() + "'.");
            }
        }

        protected override LinqExpr VisitPower(Power P)
        {
            LinqExpr l = Visit(P.Left);
            // Handle some special cases.
            if (P.Right.Equals(2))
                return Int(P, LinqExpr.Multiply(l, l));
            else if (P.Right.Equals(-1))
                return Int(P, LinqExpr.Divide(LinqExpr.Constant(1.0), l));
            else if (P.Right.Equals(-2))
                return Int(P, LinqExpr.Divide(LinqExpr.Constant(1.0), LinqExpr.Multiply(l, l)));

            LinqExpr r = Visit(P.Right);
            return Int(P, LinqExpr.Power(l, r));
        }

        protected override LinqExpr VisitCall(Call C)
        {
            LinqExpr[] args = C.Arguments.Select(i => Visit(i)).ToArray();
            return Int(C, LinqExpr.Call(
                target.Module.Compile(C.Target, args.Select(i => i.Type).ToArray()), 
                args));
        }

        protected override LinqExpr VisitConstant(Constant C) { return LinqExpr.Constant((double)C); }

        protected override LinqExpr VisitVariable(Variable V) { throw new UndefinedVariable("Undefined variable '" + V.Name + "'."); }
        protected override LinqExpr VisitUnknown(Expression E) { throw new NotSupportedException("Unsupported expression type '" + E.GetType().FullName + "'."); }

        // Generate an intermediate expression.
        private LinqExpr Int(Expression For, LinqExpr x)
        {
            LinqExpr _int = target.Decl(Scope.Intermediate, x.Type);
            target.Map(Scope.Intermediate, For, _int);
            target.Add(LinqExpr.Assign(_int, x));
            return _int;
        }

        private static bool IsNegative(Expression x)
        {
            Constant C = Product.TermsOf(x).FirstOrDefault(i => i is Constant) as Constant;
            if (C != null)
                return C.Value < 0;
            return false;
        }
    }

}