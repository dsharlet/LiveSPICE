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

        private static string Frac(string n, string d)
        {
            if (n.Length <= 2 && d.Length <= 2)
                return @"^{n}/_{d}";
            else
                return @"\frac{" + n + "}{" + d + "}";
        }

        protected override string VisitMultiply(Multiply M)
        {
            Expression N = Multiply.Numerator(M);
            Expression D = Multiply.Denominator(M);

            string n = Multiply.TermsOf(N).Select(i => Visit(i)).UnSplit(' ');
            string mn = Multiply.TermsOf(-N).Select(i => Visit(i)).UnSplit(' ');

            string d = Multiply.TermsOf(D).Select(i => Visit(i)).UnSplit(' ');
            string md = Multiply.TermsOf(-D).Select(i => Visit(i)).UnSplit(' ');

            bool m = false;
            if (mn.Length < n.Length)
            {
                m = !m;
                n = mn;
            }
            if (md.Length < d.Length)
            {
                m = !m;
                d = md;
            }
            string minus = m ? "-" : "";

            if (d != "1")
                return minus + Frac(n, d);
            else
                return minus + n;
        }

        protected override string VisitAdd(Add A)
        {
            StringBuilder s = new StringBuilder();
            s.Append("(");
            s.Append(Visit(A.Terms.First()));
            foreach (Expression i in A.Terms.Skip(1))
            {
                if (IsNegative(i))
                {
                    s.Append("-");
                    s.Append(Visit(-i));
                }
                else
                {
                    s.Append("+");
                    s.Append(Visit(i));
                }
            }
            s.Append(")");
            return s.ToString();
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
                case Operator.Equal: return "=";
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
