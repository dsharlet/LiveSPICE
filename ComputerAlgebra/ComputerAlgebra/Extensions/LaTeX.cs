using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ComputerAlgebra
{
    class LaTeXVisitor : ExpressionVisitor<string>
    {
        private string Visit(Expression E, int Precedence)
        {
            string v = Visit(E);
            if (Parser.Precedence(E) < Precedence)
                v = "(" + v + ")";
            return v;
        }

        public override string Visit(Expression E)
        {
            return base.Visit(E);
        }
        
        protected override string VisitProduct(Product M)
        {
            int pr = Parser.Precedence(Operator.Multiply);

            Expression N = Product.Numerator(M);
            string minus = "";
            if (IsNegative(N))
            {
                minus = "-";
                N = -N;
            }

            string n = Product.TermsOf(N).Select(i => Visit(i, pr)).UnSplit(' ');
            string d = Product.TermsOf(Product.Denominator(M)).Select(i => Visit(i, pr)).UnSplit(' ');

            if (d != "1")
                return minus + Frac(n, d);
            else
                return minus + n;
        }

        protected override string VisitSum(Sum A)
        {
            int pr = Parser.Precedence(Operator.Add);

            StringBuilder s = new StringBuilder();
            s.Append(Visit(A.Terms.First(), pr));
            foreach (Expression i in A.Terms.Skip(1))
            {
                string si = Visit(i, pr);

                if (si[0] != '-')
                    s.Append("+");
                s.Append(si);
            }
            return s.ToString();
        }

        protected override string VisitBinary(Binary B)
        {
            int pr = Parser.Precedence(B.Operator);
            return Visit(B.Left, pr) + ToString(B.Operator) + Visit(B.Right, pr);
        }

        protected override string VisitSet(Set S)
        {
            return @"\{" + S.Members.Select(i => Visit(i)).UnSplit(", ") + @"\}";
        }

        protected override string VisitUnary(Unary U)
        {
            return ToString(U.Operator) + Visit(U.Operand, Parser.Precedence(U.Operator));
        }

        protected override string VisitCall(Call F)
        {
            // Special case for differentiate.
            if (F.Target.Name == "D" && F.Arguments.Count() == 2)
                return @"\frac{d}{d" + Visit(F.Arguments.ElementAt(1)) + "}[" + Visit(F.Arguments.ElementAt(0)) + "]";

            return Visit(F.Target) + @"(" + F.Arguments.Select(i => Visit(i)).UnSplit(", ") + @")";
        }

        protected override string VisitPower(Power P)
        {
            int pr = Parser.Precedence(Operator.Power);
            return Visit(P.Left, pr) + "^{" + Visit(P.Right, pr) + "}";
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
                case Operator.NotEqual: return @"\neq ";
                case Operator.GreaterEqual: return @"\geq ";
                case Operator.LessEqual: return @"\leq ";
                case Operator.ApproxEqual: return @"\approx ";
                case Operator.Arrow: return @"\to ";
                default: return Binary.ToString(Op);
            }
        }

        private static string Frac(string n, string d)
        {
            if (n.Length <= 2 && d.Length <= 2)
                return @"^{n}/_{d}";
            else
                return @"\frac{" + n + "}{" + d + "}";
        }

        private static readonly Dictionary<char, string> EscapeMap = new Dictionary<char,string>()
        {
            { '&', @"\&" },
            { '%', @"\%" },
            { '$', @"\$" },
            { '#', @"\#" },
            { '_', @"\_" },
            { '{', @"\{" },
            { '}', @"\}" },
            { '~', @"\textasciitilde" },
            { '^', @"\textasciicircum" },
            { '\\', @"\textbackslash" },
        };

        private static string Escape(string x)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char i in x)
            {
                string e;
                if (EscapeMap.TryGetValue(i, out e))
                    sb.Append(e);
                else
                    sb.Append(i);
            }
            return sb.ToString();
        }

        private static bool IsNegative(Expression x)
        {
            Constant C = Product.TermsOf(x).FirstOrDefault(i => i is Constant) as Constant;
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
