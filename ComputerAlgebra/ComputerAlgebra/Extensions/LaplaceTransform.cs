using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ComputerAlgebra
{
    class LaplaceTransform : LinearTransform
    {
        // Rule database.
        private static TransformSet rules = new TransformSet()
        {
            new SubstituteTransform("L[D[f, t], t, s]", "s*L[f, t, s] - (f:t->0)"),
            new SubstituteTransform("L[I[f, t], t, s]", "L[f, t, s]/s"),
            new SubstituteTransform("L[IL[f, s, t], t, s]", "f"),
            new SubstituteTransform("L[t^N, t, s]", "Factorial[N]/s^(N + 1)", "IsNatural[N]"),
            new SubstituteTransform("L[Exp[a*t], t, s]", "1/(s - a)"),
            new SubstituteTransform("L[Sin[a*t], t, s]", "a/(s^2 + a^2)"),
            new SubstituteTransform("L[Cos[a*t], t, s]", "s/(s^2 + a^2)")
        };

        protected Expression t, s;
        
        private LaplaceTransform(Expression t, Expression s) { this.t = t; this.s = s; }

        protected override bool IsConstant(Expression x) { return !x.DependsOn(t); }

        public override Expression Visit(Expression E) 
        {
            if (!E.DependsOn(t))
                return E / s;
            
            return base.Visit(E);
        }
                
        protected override Expression VisitUnknown(Expression E) 
        {
            Expression LE = Call.L(E, t, s);

            // Try applying a rule to E.
            Expression TLE = rules.Transform(LE);
            if (!ReferenceEquals(TLE, LE))
                return TLE;

            // Try expanding E.
            Expression Ex = E.Expand(s);
            if (!ReferenceEquals(E, Ex))
                return Visit(Ex);

            // Give up.
            return LE;
        }

        public static Expression Transform(Expression f, Expression t, Expression s) { return new LaplaceTransform(t, s).Visit(f); }
    }

    class InverseLaplaceTransform : LinearTransform
    {
        // Rule database.
        private static TransformSet rules = new TransformSet()
        {
            new SubstituteTransform("IL[L[f, t, s], s, t]", "f"),
            new SubstituteTransform("IL[s*L[f, t, s] - (f:t->0), s, t]", "D[f, t]"),
            new SubstituteTransform("IL[L[f, t, s]/s, s, t]", "I[f, t]"),
            new SubstituteTransform("IL[1/s^N, s, t]", "t^(N - 1)/Factorial[N - 1]", "IsNatural[N]"),
            new SubstituteTransform("IL[1/(s + a), s, t]", "Exp[-a*t]"),
            new SubstituteTransform("IL[a/(s^2 + a^2), s, t]", "Sin[a*t]"),
            new SubstituteTransform("IL[s/(s^2 + a^2), s, t]", "Cos[a*t]")
        };

        protected Expression s, t;

        private InverseLaplaceTransform(Expression s, Expression t) { this.s = s; this.t = t; }

        protected override bool IsConstant(Expression x) { return !x.DependsOn(s); }
        
        protected override Expression VisitUnknown(Expression E)
        {
            Expression LE = Call.IL(E, s, t);

            // Try applying a known rule to E.
            Expression TLE = rules.Transform(LE, x => !x.DependsOn(s));
            if (!ReferenceEquals(TLE, LE))
                return TLE;

            // Try expanding E.
            Expression Ex = E.Expand(s);
            if (!ReferenceEquals(E, Ex))
                return Visit(Ex);

            // Give up.
            return LE;
        }

        public static Expression Transform(Expression f, Expression s, Expression t) { return new InverseLaplaceTransform(s, t).Visit(f); }
    }

    public static class LaplaceTransformExtension
    {
        /// <summary>
        /// Compute F(s) = L[f(t)].
        /// </summary>
        /// <param name="f"></param>
        /// <param name="t"></param>
        /// <param name="s"></param>
        /// <returns></returns>
        public static Expression LaplaceTransform(this Expression f, Expression t, Expression s)
        {
            return ComputerAlgebra.LaplaceTransform.Transform(f, t, s);
        }

        /// <summary>
        /// Compute f(t) = L^-1[F(s)]
        /// </summary>
        /// <param name="f"></param>
        /// <param name="s"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Expression InverseLaplaceTransform(this Expression f, Expression s, Expression t)
        {
            return ComputerAlgebra.InverseLaplaceTransform.Transform(f, s, t);
        }
    }
}