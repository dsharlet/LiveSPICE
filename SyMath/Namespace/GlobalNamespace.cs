using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Numerics;

namespace SyMath
{
    class GlobalNamespace : Namespace
    {
        // Some useful constants.
        public static readonly Expression Pi = Constant.New(Math.PI);
        public static readonly Expression e = Constant.New(Math.E);

        public static readonly Expression False = Constant.Zero;
        public static readonly Expression True = Constant.One;

        [CompileTarget(typeof(System.Math), "Abs")] public static Expression Abs(Constant x) { return Constant.New(Real.Abs(x)); }
        [CompileTarget(typeof(System.Math), "Sign")] public static Expression Sign(Constant x) { return Constant.New(Real.Sign(x)); }

        [CompileTarget(typeof(System.Math), "Sin")] public static Expression Sin(Constant x) { return Constant.New(Real.Sin(x)); }
        [CompileTarget(typeof(System.Math), "Cos")] public static Expression Cos(Constant x) { return Constant.New(Real.Cos(x)); }
        [CompileTarget(typeof(System.Math), "Tan")] public static Expression Tan(Constant x) { return Constant.New(Real.Tan(x)); }
        [CompileTarget(typeof(System.Math), "Sec")] public static Expression Sec(Constant x) { return Constant.New(Real.Sec(x)); }
        [CompileTarget(typeof(System.Math), "Csc")] public static Expression Csc(Constant x) { return Constant.New(Real.Csc(x)); }
        [CompileTarget(typeof(System.Math), "Cot")] public static Expression Cot(Constant x) { return Constant.New(Real.Cot(x)); }

        [CompileTarget(typeof(System.Math), "ArcSin")] public static Expression ArcSin(Constant x) { return Constant.New(Real.ArcSin(x)); }
        [CompileTarget(typeof(System.Math), "ArcCos")] public static Expression ArcCos(Constant x) { return Constant.New(Real.ArcCos(x)); }
        [CompileTarget(typeof(System.Math), "ArcTan")] public static Expression ArcTan(Constant x) { return Constant.New(Real.ArcTan(x)); }
        [CompileTarget(typeof(System.Math), "ArcSec")] public static Expression ArcSec(Constant x) { return Constant.New(Real.ArcSec(x)); }
        [CompileTarget(typeof(System.Math), "ArcCsc")] public static Expression ArcCsc(Constant x) { return Constant.New(Real.ArcCsc(x)); }
        [CompileTarget(typeof(System.Math), "ArcCot")] public static Expression ArcCot(Constant x) { return Constant.New(Real.ArcCot(x)); }

        [CompileTarget(typeof(System.Math), "Sinh")] public static Expression Sinh(Constant x) { return Constant.New(Real.Sinh(x)); }
        [CompileTarget(typeof(System.Math), "Cosh")] public static Expression Cosh(Constant x) { return Constant.New(Real.Cosh(x)); }
        [CompileTarget(typeof(System.Math), "Tanh")] public static Expression Tanh(Constant x) { return Constant.New(Real.Tanh(x)); }
        [CompileTarget(typeof(System.Math), "Sech")] public static Expression Sech(Constant x) { return Constant.New(Real.Sech(x)); }
        [CompileTarget(typeof(System.Math), "Csch")] public static Expression Csch(Constant x) { return Constant.New(Real.Csch(x)); }
        [CompileTarget(typeof(System.Math), "Coth")] public static Expression Coth(Constant x) { return Constant.New(Real.Coth(x)); }

        [CompileTarget(typeof(System.Math), "ArcSinh")] public static Expression ArcSinh(Constant x) { return Constant.New(Real.ArcSinh(x)); }
        [CompileTarget(typeof(System.Math), "ArcCosh")] public static Expression ArcCosh(Constant x) { return Constant.New(Real.ArcCosh(x)); }
        [CompileTarget(typeof(System.Math), "ArcTanh")] public static Expression ArcTanh(Constant x) { return Constant.New(Real.ArcTanh(x)); }
        [CompileTarget(typeof(System.Math), "ArcSech")] public static Expression ArcSech(Constant x) { return Constant.New(Real.ArcSech(x)); }
        [CompileTarget(typeof(System.Math), "ArcCsch")] public static Expression ArcCsch(Constant x) { return Constant.New(Real.ArcCsch(x)); }
        [CompileTarget(typeof(System.Math), "ArcCoth")] public static Expression ArcCoth(Constant x) { return Constant.New(Real.ArcCoth(x)); }

        [CompileTarget(typeof(System.Math), "Sqrt")] public static Expression Sqrt(Constant x) { return Constant.New(Real.Sqrt(x)); }
        [CompileTarget(typeof(System.Math), "Exp")] public static Expression Exp(Constant x) { return Constant.New(Real.Exp(x)); }
        [CompileTarget(typeof(System.Math), "Ln")] public static Expression Ln(Constant x) { return Constant.New(Real.Ln(x)); }
        [CompileTarget(typeof(System.Math), "Log")] public static Expression Log(Constant x, Constant b) { return Constant.New(Real.Log(x, b)); }

        [CompileTarget(typeof(System.Math), "Floor")] public static Expression Floor(Constant x) { return Constant.New(Real.Floor(x)); }
        [CompileTarget(typeof(System.Math), "Ceiling")] public static Expression Ceiling(Constant x) { return Constant.New(Real.Ceiling(x)); }
        [CompileTarget(typeof(System.Math), "Round")] public static Expression Round(Constant x) { return Constant.New(Real.Round(x)); }

        private static BigInteger Factorial(BigInteger x)
        {
            BigInteger F = 1;
            while (x > 1)
                F = F * x--;
            return F;
        }
        public static Expression Factorial(Constant x)
        {
            if ((Real)x % 1 == 0)
                return Constant.New(new Real(Factorial((BigInteger)(Real)x)));
            else
                throw new ArgumentException("Factorial cannot be called for non-integer value.");
        }

        public static Expression IsConstant(Constant x) { return Constant.New(true); }
        public static Expression IsInteger(Constant x) { return Constant.New((Real)x % 1 == 0); }
        public static Expression IsNatural(Constant x) { return Constant.New((Real)x % 1 == 0 && (Real)x > 0); }

        public static Expression If(Constant x, Expression t, Expression f) { return (Real)x == 0 ? f : t; }

        public static Expression IsFunctionOf(Expression f, Expression x) { return Constant.New(f.IsFunctionOf(x)); }

        public static Expression Simplify(Expression x) { return x.Simplify(); }

        public static Expression Factor(Expression f) { return f.Factor(); }
        public static Expression Factor(Expression f, Expression x) { return f.Factor(x); }
        public static Expression Expand(Expression f) { return f.Expand(); }
        public static Expression Expand(Expression f, Expression x) { return f.Expand(x); }

        /// <summary>
        /// Solve a linear equation or system of linear equations f for expressions x.
        /// </summary>
        /// <param name="f">Equation or set of equations to solve.</param>
        /// <param name="x">Expressions to solve for.</param>
        /// <returns>Expression or set of expressions for x.</returns>
        public static Expression Solve(Expression f, Expression x)
        {
            IEnumerable<Expression> result = Set.MembersOf(f).Cast<Equal>().Solve(Set.MembersOf(x));
            return (f is Set || result.Count() != 1) ? Set.New(result) : result.Single();
        }
        /// <summary>
        /// Solve an equation or system of equations f for expressions x. Uses numerical solutions the equations are not linear equations of x.
        /// </summary>
        /// <param name="f">Equation or set of equations to solve.</param>
        /// <param name="x">Expressions to solve for.</param>
        /// <param name="N">Number of iterations to use for numerical solutions.</param>
        /// <returns>Expression or set of expressions for x.</returns>
        public static Expression NSolve(Expression f, Expression x, Expression N)
        {
            IEnumerable<Expression> result = Set.MembersOf(f).Cast<Equal>().NSolve(Set.MembersOf(x).Cast<Arrow>(), (int)N);
            return (f is Set || result.Count() != 1) ? Set.New(result) : result.Single();
        }

        /// <summary>
        /// Solve a differential equation or system of differential equations f for functions y[t], with initial conditions y^(n)[0] = y0.
        /// </summary>
        /// <param name="f">Equation or set of equations to solve.</param>
        /// <param name="y">Functions to solve for.</param>
        /// <param name="y0">Values for y^(n)[0].</param>
        /// <param name="t">Independent variable.</param>
        /// <returns>Expression or set of expressions for y.</returns>
        public static Expression DSolve(Expression f, Expression y, Expression y0, Expression t)
        {
            IEnumerable<Expression> result = Set.MembersOf(f).Cast<Equal>().DSolve(Set.MembersOf(y), Set.MembersOf(y0).Cast<Arrow>(), t);
            return (f is Set || result.Count() != 1) ? Set.New(result) : result.Single();
        }

        /// <summary>
        /// Differentiate f with respect to x.
        /// </summary>
        /// <param name="f">Expression to differentiate.</param>
        /// <param name="x">Differentiation variable.</param>
        /// <returns>Derivative of f with respect to x.</returns>
        public static Expression D(Expression f, [NoSubstitute]Expression x) { return f.Differentiate(x); }
        /// <summary>
        /// Integrate f with respect to x.
        /// </summary>
        /// <param name="f">Expression to integrate.</param>
        /// <param name="x">Integration variable.</param>
        /// <returns>Antiderivative of f with respect to x.</returns>
        public static Expression I(Expression f, [NoSubstitute]Expression x) { return f.Integrate(x); }
        /// <summary>
        /// Find the Laplace transform of f[t].
        /// </summary>
        /// <param name="f"></param>
        /// <param name="t"></param>
        /// <param name="s"></param>
        /// <returns></returns>
        public static Expression L(Expression f, [NoSubstitute]Expression t, Expression s) { return f.LaplaceTransform(t, s); }
        /// <summary>
        /// Find the inverse Laplace transform of F[s].
        /// </summary>
        /// <param name="f"></param>
        /// <param name="s"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Expression IL(Expression f, [NoSubstitute]Expression s, Expression t) { return f.InverseLaplaceTransform(s, t); }
    };
}
