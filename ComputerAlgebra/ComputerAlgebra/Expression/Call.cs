using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace ComputerAlgebra
{
    /// <summary>
    /// Function call expression.
    /// </summary>
    public class Call : Atom
    {
        protected Function target;
        public Function Target { get { return target; } }

        protected List<Expression> arguments;
        public IEnumerable<Expression> Arguments { get { return arguments; } }

        protected Call(Function Target, IEnumerable<Expression> Args) 
        {
            Debug.Assert(Target != null);
            target = Target; 
            arguments = Args.ToList(); 
        }

        public static Call New(Function Target, IEnumerable<Expression> Args) { return new Call(Target, Args); }
        public static Call New(Function Target, params Expression[] Args) { return new Call(Target, Args); }
        public static Call New(string Target, IEnumerable<Expression> Args) { return new Call(Namespace.Global.Resolve(Target, Args), Args); }
        public static Call New(string Target, params Expression[] Args) { return new Call(Namespace.Global.Resolve(Target, Args), Args); }

        public override bool Matches(Expression E, MatchContext Matched)
        {
            Call EF = E as Call;
            if (!ReferenceEquals(EF, null))
            {
                if (target != EF.Target || arguments.Count() != EF.Arguments.Count())
                    return false;

                return Matched.TryMatch(() => arguments.AsEnumerable().Reverse().Zip(EF.Arguments.Reverse(), (p, e) => p.Matches(e, Matched)).All());
            }

            return false;
        }

        // object interface.
        public override string ToString() { return target.ToString() + "[" + arguments.UnSplit(", ") + "]"; }
        public override bool Equals(Expression E)
        {
            Call C = E as Call;
            if (ReferenceEquals(C, null)) return false;
            
            return target.Equals(C.Target) && arguments.SequenceEqual(C.Arguments);
        }
        public override int GetHashCode() { return target.GetHashCode() ^ arguments.OrderedHashCode(); }

        protected override int TypeRank { get { return 2; } }
        //public override IEnumerable<Atom> Atoms
        //{
        //    get 
        //    {
        //        yield return ConstantString.New(Name);
        //        foreach (Expression i in arguments)
        //            foreach (Atom j in i.Atoms)
        //                yield return j;
        //    }
        //}
        public override int CompareTo(Expression R)
        {
            Call RF = R as Call;
            if (!ReferenceEquals(RF, null))
                return LexicalCompareTo(
                    () => target.CompareTo(RF.Target),
                    () => arguments.LexicalCompareTo(RF.Arguments));

            return base.CompareTo(R);
        }

        public static Expression If(Expression c, Expression t, Expression f) { return Call.New("If", c, t, f); }

        public static Expression IsFunctionOf(Expression f, Expression x) { return Call.New("IsFunctionOf", f, x); }
        public static Expression IsConstant(Expression x) { return Call.New("IsConstant", x); }
        public static Expression IsInteger(Expression x) { return Call.New("IsInteger", x); }
        public static Expression IsNatural(Expression x) { return Call.New("IsNatural", x); }

        public static Expression Abs(Expression x) { return Call.New("Abs", x); }
        public static Expression Sign(Expression x) { return Call.New("Sign", x); }

        public static Expression Min(Expression a, Expression b) { return Call.New("Min", a, b); }
        public static Expression Max(Expression a, Expression b) { return Call.New("Max", a, b); }

        public static Expression Sin(Expression x) { return Call.New("Sin", x); }
        public static Expression Cos(Expression x) { return Call.New("Cos", x); }
        public static Expression Tan(Expression x) { return Call.New("Tan", x); }
        public static Expression Sec(Expression x) { return Call.New("Sec", x); }
        public static Expression Csc(Expression x) { return Call.New("Csc", x); }
        public static Expression Cot(Expression x) { return Call.New("Cot", x); }
        public static Expression ArcSin(Expression x) { return Call.New("ArcSin", x); }
        public static Expression ArcCos(Expression x) { return Call.New("ArcCos", x); }
        public static Expression ArcTan(Expression x) { return Call.New("ArcTan", x); }
        public static Expression ArcSec(Expression x) { return Call.New("ArcSec", x); }
        public static Expression ArcCsc(Expression x) { return Call.New("ArcCsc", x); }
        public static Expression ArcCot(Expression x) { return Call.New("ArcCot", x); }

        public static Expression Sinh(Expression x) { return Call.New("Sinh", x); }
        public static Expression Cosh(Expression x) { return Call.New("Cosh", x); }
        public static Expression Tanh(Expression x) { return Call.New("Tanh", x); }
        public static Expression Sech(Expression x) { return Call.New("Sech", x); }
        public static Expression Csch(Expression x) { return Call.New("Csch", x); }
        public static Expression Coth(Expression x) { return Call.New("Coth", x); }
        public static Expression ArcSinh(Expression x) { return Call.New("ArcSinh", x); }
        public static Expression ArcCosh(Expression x) { return Call.New("ArcCosh", x); }
        public static Expression ArcTanh(Expression x) { return Call.New("ArcTanh", x); }
        public static Expression ArcSech(Expression x) { return Call.New("ArcSech", x); }
        public static Expression ArcCsch(Expression x) { return Call.New("ArcCsch", x); }
        public static Expression ArcCoth(Expression x) { return Call.New("ArcCoth", x); }

        public static Expression Sqrt(Expression x) { return Call.New("Sqrt", x); }
        public static Expression Exp(Expression x) { return Call.New("Exp", x); }
        public static Expression Ln(Expression x) { return Call.New("Ln", x); }
        public static Expression Log(Expression x, Expression b) { return Call.New("Log", x, b); }

        public static Expression Factorial(Expression x) { return Call.New("Factorial", x); }

        public static Expression Factor(Expression x) { return Call.New("Factor", x); }
        public static Expression Expand(Expression x) { return Call.New("Expand", x); }

        public static Expression D(Expression f, Expression x) { return Call.New("D", f, x); }
        public static Expression I(Expression f, Expression x) { return Call.New("I", f, x); }
        public static Expression L(Expression f, Expression t, Expression s) { return Call.New("L", f, t, s); }
        public static Expression IL(Expression f, Expression s, Expression t) { return Call.New("IL", f, s, t); }
    }
}