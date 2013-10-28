using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyMath
{
    /// <summary>
    /// Implements differentiation.
    /// </summary>
    class DifferentiateTransform : VisitorTransform
    {
        // Rule database.
        private static TransformSet rules = new TransformSet()
        {
            new SubstituteTransform("D[Sin[u], x]", "Cos[u]*D[u, x]"),
            new SubstituteTransform("D[Cos[u], x]", "-Sin[u]*D[u, x]"),
            new SubstituteTransform("D[Tan[u], x]", "Sec[u]^2*D[u, x]"),
            new SubstituteTransform("D[Abs[u], x]", "Sign[u]*D[u, x]"),
            new SubstituteTransform("D[Sign[u], x]", "0"),
            new SubstituteTransform("D[Exp[u], x]", "Exp[u]*D[u, x]"),
            new SubstituteTransform("D[Ln[u], x]", "D[u, x]/u"),
            new SubstituteTransform("D[I[u, x], x]", "u"),
            new SubstituteTransform("D[If[c, t, f], x]", "If[c, D[t, x], D[f, x]]"),
            new SubstituteTransform("D[Max[a, b], x]", "If[a > b, D[a, x], D[b, x]]"),
            new SubstituteTransform("D[Min[a, b], x]", "If[a < b, D[a, x], D[b, x]]")
        };

        // Differentation variable.
        protected Expression x;

        private DifferentiateTransform(Expression x) { this.x = x; }

        public static Expression Transform(Expression f, Expression x) { return new DifferentiateTransform(x).Visit(f); }

        public override Expression Visit(Expression E) 
        {
            if (x.Equals(E))
                return Constant.One;
            else if (!E.DependsOn(x))
                return Constant.Zero;
            else
                return base.Visit(E);
        }

        protected override Expression VisitAdd(Add A) { return Add.New(A.Terms.Select(i => Visit(i)).Where(i => !i.IsZero())); }

        protected Expression ProductRule(Expression L, IEnumerable<Expression> R)
        {
            if (R.Empty())
                return Visit(L);

            if (L.DependsOn(x))
            {
                // Product rule.
                return Add.New(
                    Multiply.New(new Expression[] { Visit(L) }.Concat(R)),
                    Multiply.New(L, ProductRule(R.First(), R.Skip(1)))).Evaluate();
            }
            else
            {
                // L is constant w.r.t. x.
                return Multiply.New(L, ProductRule(R.First(), R.Skip(1))).Evaluate();
            }
        }

        protected override Expression VisitMultiply(Multiply M)
        {
            return ProductRule(M.Terms.First(), M.Terms.Skip(1));
        }

        protected override Expression VisitPower(Power P)
        {
            Expression f = P.Left;
            Expression g = P.Right;
            if (g.DependsOn(x))
            {
                // f(x)^g(x)
                return Multiply.New(P,
                    Add.New(
                        Multiply.New(Visit(f), Binary.Divide(g, f)),
                        Multiply.New(Visit(g), Call.Ln(f)))).Evaluate();
            }
            else
            {
                // f(x)^g
                return Multiply.New(
                    g,
                    Power.New(f, Binary.Subtract(g, Constant.One)),
                    Visit(f)).Evaluate();
            }
        }

        protected override Expression VisitUnknown(Expression E) 
        {
            Expression DE = Call.D(E, x);
            Expression TDE = rules.Transform(DE);
            if (!ReferenceEquals(TDE, DE))
                return TDE;
            return TDE;
        }
    }

    public static class DifferentiateExtension
    {
        /// <summary>
        /// Differentiate expression with respect to x.
        /// </summary>
        /// <param name="f"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        public static Expression Differentiate(this Expression f, Expression x) { return DifferentiateTransform.Transform(f, x); }
    }
}
