using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ComputerAlgebra
{
    public class Power : Binary
    {
        protected Power(Expression L, Expression R) : base(Operator.Power, L, R) { }
        public static Expression New(Expression L, Expression R) { return new Power(L, R); }

        /// <summary>
        /// If x is of the form a^b, return b and replace x with a.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static Expression ExponentOf(Expression x)
        {
            if (x is Power)
                return ((Power)x).Right;
            return 1;
        }

        /// <summary>
        /// If x is of the form a^n where n is an integer, return n and replace x with a.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static int IntegralExponentOf(Expression x)
        {
            Expression n = ExponentOf(x);
            if (n is Constant && ((Real)n) % 1 == 0)
                return (int)(Real)n;
            return 1;
        }

        public override bool Matches(Expression E, MatchContext Matched)
        {
            Expression matched;
            if (Matched.TryGetValue(Right, out matched))
            {
                if (Left.Matches(E ^ Binary.Divide(1, matched), Matched))
                    return true;
            }

            // x^0 = 1.
            if (E.EqualsOne() && Right.Matches(0, Matched))
                return true;
            // 0^x = 0.
            if (E.EqualsZero() && Left.Matches(0, Matched))
                return true;

            Binary PE = E as Power;
            if (!ReferenceEquals(PE, null) && Matched.TryMatch(() => Left.Matches(PE.Left, Matched) && Right.Matches(PE.Right, Matched)))
                return true;

            // If the exponent matches 1, E can match left.
            if (Matched.TryMatch(() => Right.Matches(1, Matched) && Left.Matches(E, Matched)))
                return true;

            if (Left.Matches(ComputerAlgebra.Power.New(E, Binary.Divide(1, Right)).Evaluate(), Matched))
                return true;

            return false;
        }

        public override int CompareTo(Expression R)
        {
            Expression RL = R;
            Expression RR = 1;
            Power RP = R as Power;
            if (!ReferenceEquals(RP, null))
            {
                RL = RP.Left;
                RR = RP.Right;
            }
            return LexicalCompareTo(
                () => Left.CompareTo(RL),
                () => Right.CompareTo(RR));
        }
    }
}
