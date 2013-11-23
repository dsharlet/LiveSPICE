using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ComputerAlgebra
{
    class PrettyString
    {
        private List<string> lines;
        int zero;

        public PrettyString(int ZeroLine, IEnumerable<string> Lines) { lines = Lines.ToList(); zero = ZeroLine; }
        public PrettyString(int ZeroLine, params string[] Lines) { lines = Lines.ToList(); zero = ZeroLine; }

        public IEnumerable<string> Lines { get { return lines; } }
        public int LineCount { get { return lines.Count; } }
        public int ColumnCount { get { return lines.Max(i => i.Length); } }
        public int ZeroRow { get { return zero; } }

        public static PrettyString ConcatLines(int Zero, PrettyString A, PrettyString B)
        {
            int cols = Math.Max(A.ColumnCount, B.ColumnCount);
            string padA = new string(' ', (cols - A.ColumnCount) / 2);
            string padB = new string(' ', (cols - B.ColumnCount) / 2);
            return new PrettyString(Zero, A.Lines.Select(i => padA + i).Concat(B.Lines.Select(i => padB + i)));
        }

        public static PrettyString ConcatColumns(PrettyString L, PrettyString R)
        {
            int l0 = Math.Min(-L.ZeroRow, -R.ZeroRow);
            int l1 = Math.Max(L.LineCount - L.ZeroRow, R.LineCount - R.ZeroRow);

            IEnumerable<string> linesL = Enumerable.Repeat("", -L.ZeroRow - l0).Concat(L.Lines).Concat(Enumerable.Repeat("", l1 - (L.LineCount - L.ZeroRow)));
            IEnumerable<string> linesR = Enumerable.Repeat("", -R.ZeroRow - l0).Concat(R.Lines).Concat(Enumerable.Repeat("", l1 - (R.LineCount - R.ZeroRow)));

            int cols = L.ColumnCount;

            return new PrettyString(-l0, linesL.Zip(linesR, (i, j) => i + new string(' ', cols - i.Length) + j));
        }

        public static PrettyString ConcatColumns(PrettyString L, PrettyString M, PrettyString R)
        {
            return ConcatColumns(L, ConcatColumns(M, R));
        }

        public static implicit operator PrettyString(string l) { return new PrettyString(0, l); }

        public override string ToString() { return lines.UnSplit("\r\n"); }
    }

    class PrettyStringVisitor : ExpressionVisitor<PrettyString>
    {
        private static PrettyString MakeLParen(int Lines)
        {
            if (Lines == 1)
                return new PrettyString(0, "(");
            if (Lines == 2)
                return new PrettyString(1,
                    "/",
                    "\\");

            return new PrettyString(Lines / 2,
                Enumerable.Repeat(@"/", 1).Concat(
                Enumerable.Repeat(@"|", Lines - 2)).Concat(
                Enumerable.Repeat(@"\", 1)));
        }

        private static PrettyString MakeLBrace(int Lines)
        {
            if (Lines == 1)
                return new PrettyString(0, "{");
            if (Lines <= 3) 
                return new PrettyString(1,
                    @"(",
                    @"<",
                    @"(");
            if (Lines <= 5)
                return new PrettyString(Lines / 2,
                    @"/",
                    @"\",
                    @"<",
                    @"/",
                    @"\");

            return new PrettyString(Lines / 2,
                Enumerable.Repeat(@"/", 1).Concat(
                Enumerable.Repeat(@"\", 1)).Concat(
                Enumerable.Repeat(@"|", (Lines - 5) / 2)).Concat(
                Enumerable.Repeat(@"<", 1)).Concat(
                Enumerable.Repeat(@"|", (Lines - 4) / 2)).Concat(
                Enumerable.Repeat(@"/", 1)).Concat(
                Enumerable.Repeat(@"\", 1)));
        }

        private static PrettyString MakeLBracket(int Lines)
        {
            return new PrettyString(Lines / 2, Enumerable.Repeat("[", Lines));
        }

        private static Dictionary<string, string> FlipMap = new Dictionary<string, string>() { { "(", ")" }, { "{", "}" }, { "[", "]" }, { "|", "|" }, { "<", ">" }, { "/", "\\" }, { "\\", "/" } };
        private static PrettyString FlipParen(PrettyString x) { return new PrettyString(x.ZeroRow, x.Lines.Select(i => FlipMap[i])); }
        
        private static PrettyString MakeRParen(int Lines) { return FlipParen(MakeLParen(Lines)); }
        private static PrettyString MakeRBrace(int Lines) { return FlipParen(MakeLBrace(Lines)); }
        private static PrettyString MakeRBracket(int Lines) { return FlipParen(MakeLBracket(Lines)); }

        private static PrettyString ConcatParens(PrettyString x) { return PrettyString.ConcatColumns(MakeLParen(x.LineCount), x, MakeRParen(x.LineCount)); }
        private static PrettyString ConcatBraces(PrettyString x) { return PrettyString.ConcatColumns(MakeLBrace(x.LineCount), x, MakeRBrace(x.LineCount)); }
        private static PrettyString ConcatBrackets(PrettyString x) { return PrettyString.ConcatColumns(MakeLBracket(x.LineCount), x, MakeRBracket(x.LineCount)); }

        private static int Precedence(Expression x)
        {
            if (x is Sum)
                return Parser.Precedence(Operator.Add);
            else if (x is Product)
                return Parser.Precedence(Operator.Multiply);
            else if (x is Binary)
                return Parser.Precedence(((Binary)x).Operator);
            else if (x is Unary)
                return Parser.Precedence(((Unary)x).Operator);
            else if (x is Atom)
                return 100;
            return Parser.Precedence(Operator.Equal);
        }

        private static bool IsNegative(Expression x)
        {
            Constant C = Product.TermsOf(x).FirstOrDefault(i => i is Constant) as Constant;
            if (C != null)
                return C.Value < 0;
            return false;
        }

        private PrettyString UnSplit(IEnumerable<Expression> x, string Delim)
        {
            if (x.Any())
            {
                PrettyString unsplit = Visit(x.First());
                foreach (Expression i in x.Skip(1))
                    unsplit = PrettyString.ConcatColumns(unsplit, Delim, Visit(i));
                return unsplit;
            }
            return "";
        }

        private Stack<int> precedence = new Stack<int>();
        public override PrettyString Visit(Expression E)
        {
            if (precedence.Empty())
                precedence.Push(0);

            int p = Precedence(E);
            bool parens = p < precedence.Peek();
            precedence.Push(p);
            PrettyString V = base.Visit(E);
            precedence.Pop();
            if (parens)
                V = ConcatParens(V);
            return V;
        }

        private PrettyString VisitDivide(Expression N, Expression D)
        {
            precedence.Push(0);
            PrettyString NS = Visit(N);
            PrettyString DS = Visit(D);
            precedence.Pop();

            if (DS.ColumnCount <= 2 && DS.ColumnCount <= 2)
                return PrettyString.ConcatColumns(NS, "/", DS);

            int Cols = Math.Max(NS.ColumnCount, DS.ColumnCount);
            return PrettyString.ConcatLines(NS.LineCount, NS, PrettyString.ConcatLines(0, new string('-', Cols), DS));
        }

        protected override PrettyString VisitProduct(Product M)
        {
            Expression N = Product.Numerator(M);
            Expression D = Product.Denominator(M);

            bool negative = false;
            if (N is Product && IsNegative(N))
            {
                negative = !negative;
                N = -N;
            }
            if (D is Product && IsNegative(D))
            {
                negative = !negative;
                D = -D;
            }

            if (!D.Equals(1))
                return PrettyString.ConcatColumns(negative ? "- " : "", VisitDivide(N, D));
            else if (N is Product)
                return PrettyString.ConcatColumns(negative ? "-" : "", UnSplit(Product.TermsOf(N), "*"));
            else
                return PrettyString.ConcatColumns(negative ? "-" : "", Visit(N));
        }

        protected override PrettyString VisitSum(Sum A)
        {
            PrettyString s = Visit(A.Terms.First());

            foreach (Expression i in A.Terms.Skip(1))
            {
                if (IsNegative(i))
                    s = PrettyString.ConcatColumns(s, " - ", Visit(-i));
                else
                    s = PrettyString.ConcatColumns(s, " + ", Visit(i));
            }
            return s;
        }

        protected override PrettyString VisitBinary(Binary B)
        {
            return PrettyString.ConcatColumns(Visit(B.Left), Binary.ToString(B.Operator), Visit(B.Right));
        }

        protected override PrettyString VisitSet(Set S)
        {
            precedence.Push(0);
            PrettyString s = ConcatBraces(UnSplit(S.Members, ", "));
            precedence.Pop();
            return s;
        }

        protected override PrettyString VisitUnary(Unary U)
        {
            return PrettyString.ConcatColumns(Unary.ToString(U.Operator), Visit(U.Operand));
        }

        protected override PrettyString VisitCall(Call F)
        {
            precedence.Push(0);
            PrettyString s = PrettyString.ConcatColumns(F.Target.Name, ConcatBrackets(UnSplit(F.Arguments, ", ")));
            precedence.Pop();
            return s;
        }

        protected override PrettyString VisitPower(Power P)
        {
            if (IsNegative(P.Right))
                return VisitDivide(1, P ^ -1);

            PrettyString l = Visit(P.Left);
            PrettyString r = Visit(P.Right);
            r = new PrettyString(r.ZeroRow + 1, r.Lines);
            return PrettyString.ConcatColumns(l, r);
        }

        protected override PrettyString VisitUnknown(Expression E)
        {
            return E.ToString();
        }
    }

    public static class PrettyStringExtension
    {
        private static PrettyStringVisitor formatter = new PrettyStringVisitor();

        public static string ToPrettyString(this Expression x)
        {
            return formatter.Visit(x).ToString();
        }
    }
}
