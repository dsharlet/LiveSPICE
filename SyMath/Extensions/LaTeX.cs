using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyMath
{
    class LaTeXVisitor : ExpressionVisitor<string>
    {
        public override string Visit(Expression E)
        {
            return base.Visit(E);
        }

        private string VisitDivide(Expression N, Expression D)
        {
            string n = Visit(N);
            string d = Visit(D);
            if (n.Length <= 2 && d.Length <= 2)
                return @"^{n}/_{d}";
            else
                return @"\frac{" + n + "}{" + d + "}";
        }

        protected override string VisitMultiply(Multiply M)
        {
            Expression N = Multiply.Numerator(M);
            Expression D = Multiply.Denominator(M);

            bool negative = false;
            if (N is Multiply && IsNegative(N))
            {
                negative = !negative;
                N = -N;
            }
            if (D is Multiply && IsNegative(D))
            {
                negative = !negative;
                D = -D;
            }
            string minus = negative ? "-" : "";

            if (!D.IsOne())
                return minus + VisitDivide(N, D);
            else
                return minus + Multiply.TermsOf(N).Select(i => Visit(i)).UnSplit(' ');
        }

        protected override string VisitAdd(Add A)
        {
            return A.Terms.Select(i => Visit(i)).UnSplit(" + ");
        }

        protected override string VisitBinary(Binary B)
        {
            return Visit(B.Left) + ToString(B.Operator) + Visit(B.Right);
        }

        protected override string VisitSet(Set S)
        {
            return @"\{" + S.Members.Select(i => Visit(i)).UnSplit(", ") + @"\}";
        }

        protected override string VisitUnary(Unary U)
        {
            return ToString(U.Operator) + Visit(U.Operand);
        }

        protected override string VisitCall(Call F)
        {
            // Special case for differentiate.
            if (F.Target.Name == "D" && F.Arguments.Count == 2)
                return @"\frac{d}{d" + Visit(F.Arguments[1]) + "}[" + Visit(F.Arguments[0]) + "]";

            return Visit(F.Target) + @"(" + F.Arguments.Select(i => Visit(i)).UnSplit(", ") + @")";
        }

        protected override string VisitPower(Power P)
        {
            return Visit(P.Left) + "^{" + Visit(P.Right) + "}";
        }

        protected override string VisitConstant(Constant C)
        {
            return C.Value.ToLaTeX();
        }

        protected override string VisitUnknown(Expression E)
        {
            return Escape(E.ToString());
        }

        private static string ToString(Operator Op)
        {
            switch (Op)
            {
                case Operator.NotEqual: return @"\neq";
                case Operator.GreaterEqual: return @"\geq";
                case Operator.LessEqual: return @"\leq";
                case Operator.Arrow: return @"\to";
                default: return Binary.ToString(Op);
            }
        }

        private static string Escape(string x)
        {
            return x;
        }

        private static bool IsNegative(Expression x)
        {
            Constant C = Multiply.TermsOf(x).FirstOrDefault(i => i is Constant) as Constant;
            if (C != null)
                return C.Value < 0;
            return false;
        }
    }

    public static class LaTeXExtension
    {
        /// <summary>
        /// Write x as a LaTeX string.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static string ToLaTeX(this Expression x)
        {
            return new LaTeXVisitor().Visit(x);
        }
    }
}
