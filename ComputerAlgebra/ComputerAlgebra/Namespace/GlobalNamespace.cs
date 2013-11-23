using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Numerics;

namespace ComputerAlgebra
{
    class GlobalNamespace : Namespace
    {
        private static IfFunction If = IfFunction.New();

        public GlobalNamespace() : base(typeof(GlobalNamespace)) { }

        public override IEnumerable<Expression> LookupName(string Name)
        {
            if (Name == "If")
                return new Expression[] { If };
            return base.LookupName(Name);
        }

        // Some useful constants.
        public static readonly Expression Pi = Constant.New(Math.PI);
        public static readonly Expression e = Constant.New(Math.E);

        public static readonly Expression False = 0;
        public static readonly Expression True = 1;

        public static Expression Abs(Constant x) { return Constant.New(Real.Abs(x)); }
        public static Expression Sign(Constant x) { return Constant.New(Real.Sign(x)); }

        public static Expression Min(Constant x, Constant y) { return Real.Min((Real)x, (Real)y); }
        public static Expression Max(Constant x, Constant y) { return Real.Max((Real)x, (Real)y); }

        public static Expression Sin(Constant x) { return Constant.New(Real.Sin(x)); }
        public static Expression Cos(Constant x) { return Constant.New(Real.Cos(x)); }
        public static Expression Tan(Constant x) { return Constant.New(Real.Tan(x)); }
        public static Expression Sec(Constant x) { return Constant.New(Real.Sec(x)); }
        public static Expression Csc(Constant x) { return Constant.New(Real.Csc(x)); }
        public static Expression Cot(Constant x) { return Constant.New(Real.Cot(x)); }

        public static Expression ArcSin(Constant x) { return Constant.New(Real.ArcSin(x)); }
        public static Expression ArcCos(Constant x) { return Constant.New(Real.ArcCos(x)); }
        public static Expression ArcTan(Constant x) { return Constant.New(Real.ArcTan(x)); }
        public static Expression ArcSec(Constant x) { return Constant.New(Real.ArcSec(x)); }
        public static Expression ArcCsc(Constant x) { return Constant.New(Real.ArcCsc(x)); }
        public static Expression ArcCot(Constant x) { return Constant.New(Real.ArcCot(x)); }

        public static Expression Sinh(Constant x) { return Constant.New(Real.Sinh(x)); }
        public static Expression Cosh(Constant x) { return Constant.New(Real.Cosh(x)); }
        public static Expression Tanh(Constant x) { return Constant.New(Real.Tanh(x)); }
        public static Expression Sech(Constant x) { return Constant.New(Real.Sech(x)); }
        public static Expression Csch(Constant x) { return Constant.New(Real.Csch(x)); }
        public static Expression Coth(Constant x) { return Constant.New(Real.Coth(x)); }

        public static Expression ArcSinh(Constant x) { return Constant.New(Real.ArcSinh(x)); }
        public static Expression ArcCosh(Constant x) { return Constant.New(Real.ArcCosh(x)); }
        public static Expression ArcTanh(Constant x) { return Constant.New(Real.ArcTanh(x)); }
        public static Expression ArcSech(Constant x) { return Constant.New(Real.ArcSech(x)); }
        public static Expression ArcCsch(Constant x) { return Constant.New(Real.ArcCsch(x)); }
        public static Expression ArcCoth(Constant x) { return Constant.New(Real.ArcCoth(x)); }

        public static Expression Sqrt(Constant x) { return Constant.New(Real.Sqrt(x)); }
        public static Expression Exp(Constant x) { return Constant.New(Real.Exp(x)); }
        public static Expression Ln(Constant x) { return Constant.New(Real.Ln(x)); }
        public static Expression Log(Constant x, Constant b) { return Constant.New(Real.Log(x, b)); }

        public static Expression Floor(Constant x) { return Constant.New(Real.Floor(x)); }
        public static Expression Ceiling(Constant x) { return Constant.New(Real.Ceiling(x)); }
        public static Expression Round(Constant x) { return Constant.New(Real.Round(x)); }

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

        public static Expression IsFunctionOf(Expression f, Expression x) { return Constant.New(f.DependsOn(x)); }
        
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
        /// <param name="e">Number of iterations to use for numerical solutions.</param>
        /// <returns>Expression or set of expressions for x.</returns>
        public static Expression NSolve(Expression f, Expression x, Expression e, Expression n)
        {
            IEnumerable<Expression> result = Set.MembersOf(f).Cast<Equal>().NSolve(Set.MembersOf(x).Cast<Arrow>(), (double)e, (int)n);
            return (f is Set || result.Count() != 1) ? Set.New(result) : result.Single();
        }
        public static Expression NSolve(Expression f, Expression x)
        {
            IEnumerable<Expression> result = Set.MembersOf(f).Cast<Equal>().NSolve(Set.MembersOf(x).Cast<Arrow>());
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


        private static BigInteger Factorial(BigInteger x)
        {
            BigInteger F = 1;
            while (x > 1)
                F = F * x--;
            return F;
        }
    };
}
