using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ComputerAlgebra
{
    public class Unary : Expression
    {
        protected Operator op;
        protected Expression o;
        public Operator Operator { get { return op; } }
        public Expression Operand { get { return o; } }

        protected Unary(Operator Op, Expression Operand) { op = Op; o = Operand; }

        public static Expression Prime(Expression Operand) 
        { 
            Call f = Operand as Call;
            if (f != null && f.Arguments.Count() == 1)
                return Call.D(f, f.Arguments.First());
            return new Unary(Operator.Prime, Operand); 
        }
        public static Expression Negate(Expression Operand) { return Product.New(-1, Operand); }
        public static Expression Inverse(Expression Operand) { return Binary.Power(Operand, -1); }
        public static Expression Not(Expression Operand) { return new Unary(Operator.Not, Operand); }
        public static Expression New(Operator Op, Expression Operand)
        {
            switch (Op)
            {
                case Operator.Prime: return Prime(Operand);
                case Operator.Negate: return Negate(Operand);
                case Operator.Inverse: return Inverse(Operand);
                case Operator.Not: return Not(Operand);
                default: return new Unary(Op, Operand);
            }
        }
        
        public static string ToString(Operator o)
        {
            switch (o)
            {
                case Operator.Negate: return "-";
                case Operator.Not: return "!";
                default: return "<unknown>";
            }
        }

        // object.
        public override string ToString() { return ToString(Operator) + Operand.ToString(Parser.Precedence(Operator)); }
        public override int GetHashCode() { return Operator.GetHashCode() ^ Operand.GetHashCode(); }
        public override bool Equals(Expression E)
        {
            Unary U = E as Unary;
            if (ReferenceEquals(U, null)) return false;
            
            return Operator.Equals(U.Operator) && Operand.Equals(U.Operand);
        }

        public override IEnumerable<Atom> Atoms { get { return Operand.Atoms; } }
        public override int CompareTo(Expression R)
        {
            Unary RU = R as Unary;
            if (!ReferenceEquals(RU, null))
                return LexicalCompareTo(
                    () => Operator.CompareTo(RU.Operator),
                    () => Operand.CompareTo(RU.Operand));

            return base.CompareTo(R);
        }
    }
}
