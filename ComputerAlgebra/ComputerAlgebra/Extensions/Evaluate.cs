using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace ComputerAlgebra
{
    /// <summary>
    /// Implements constant evaluation.
    /// </summary>
    class EvaluateVisitor : CachedRecursiveVisitor
    {
        private List<Exception> exceptions = new List<Exception>();
        public IEnumerable<Exception> Exceptions { get { return exceptions; } }

        public EvaluateVisitor() { }

        // In the case of revisiting an expression, just return it to avoid stack overflow.
        protected override Expression Revisit(Expression E) { return E; }

        public static Expression EvaluateSum(IEnumerable<Expression> Terms)
        {
            // Map terms to their coefficients.
            DefaultDictionary<Expression, Real> terms = new DefaultDictionary<Expression, Real>(0);

            // Accumulate constants and sum coefficient of each term.
            Real C = 0;
            foreach (Expression i in Terms)
            {
                if (i is Constant)
                {
                    C += (Real)i;
                }
                else
                {
                    // Find constant term.
                    Constant coeff = Product.TermsOf(i).OfType<Constant>().FirstOrDefault();
                    if (!ReferenceEquals(coeff, null))
                        terms[Product.New(Product.TermsOf(i).ExceptUnique(coeff, Expression.RefComparer))] += (Real)coeff;
                    else
                        terms[i] += 1;
                }
            }

            // Build a new expression with the accumulated terms.
            if (!C.EqualsZero())
                terms.Add(Constant.New(C), (Real)1);
            return Sum.New(terms
                .Where(i => !i.Value.EqualsZero())
                .Select(i => !i.Value.EqualsOne() ? Product.New(i.Key, Constant.New(i.Value)) : i.Key));
        }

        protected override Expression VisitSum(Sum A)
        {
            return EvaluateSum(A.Terms.SelectMany(i => Sum.TermsOf(Visit(i))));
        }
        
        // Combine like terms and multiply constants.
        protected override Expression VisitProduct(Product M)
        {
            // Map terms to exponents.
            DefaultDictionary<Expression, Real> terms = new DefaultDictionary<Expression, Real>(0);

            // Accumulate constants and sum exponent of each term.
            Real C = 1;
            foreach (Expression i in M.Terms.SelectMany(i => Product.TermsOf(Visit(i))))
            {
                if (i is Constant)
                {
                    C *= (Real)i;
                }
                else
                {
                    Power Pi = i as Power;
                    if (!ReferenceEquals(Pi, null) && Pi.Right is Constant)
                        terms[Pi.Left] += (Real)Pi.Right;
                    else
                        terms[i] += 1;
                }
            }

            // Build a new expression with the accumulated terms.
            if (C.EqualsZero())
            {
                return 0;
            }
            else if (!C.EqualsOne())
            {
                // Find a sum term that has a constant term to distribute into.
                KeyValuePair<Expression, Real> A = terms.FirstOrDefault(i => Real.Abs(i.Value).EqualsOne() && i.Key is Sum);
                if (!ReferenceEquals(A.Key, null))
                {
                    terms.Remove(A.Key);
                    terms[ExpandExtension.Distribute(C ^ A.Value, A.Key)] += A.Value;
                }
                else
                {
                    terms.Add(C, 1);
                }
            }
            return Product.New(terms
                .Where(i => !i.Value.EqualsZero())
                .Select(i => !i.Value.EqualsOne() ? Power.New(i.Key, Constant.New(i.Value)) : i.Key));
        }

        protected override Expression VisitCall(Call C)
        {
            C = (Call)base.VisitCall(C);

            try
            {
                if (C.Target.CanCall(C.Arguments))
                {
                    Expression call = C.Target.Call(C.Arguments);
                    if (!ReferenceEquals(call, null))
                        return call;
                }
            }
            catch (Exception Ex) { exceptions.Add(Ex); }
            return C;
        }

        protected override Expression VisitPower(Power P)
        {
            Expression L = Visit(P.Left);
            
            // Transform (x*y)^z => x^z*y^z.
            Product M = L as Product;
            if (!ReferenceEquals(M, null))
                return Visit(Product.New(M.Terms.Select(i => Power.New(i, P.Right))));
            
            Expression R = Visit(P.Right);

            // Transform (x^y)^z => x^(y*z)
            Power LP = L as Power;
            if (!ReferenceEquals(LP, null))
            {
                L = LP.Left;
                R = Visit(Product.New(R, LP.Right)); // TODO: Redundant visit of R?
            }

            // Handle identities.
            Real? LR = AsReal(L);
            if (EqualsZero(LR)) return 0;
            if (EqualsOne(LR)) return 1;

            Real? RR = AsReal(R);
            if (EqualsZero(RR)) return 1;
            if (EqualsOne(RR)) return L;

            // Evaluate result.
            if (LR != null && RR != null)
                return Constant.New(LR.Value ^ RR.Value);
            else
                return Power.New(L, R);
        }
        
        protected override Expression VisitBinary(Binary B)
        {
            Expression L = Visit(B.Left);
            Expression R = Visit(B.Right);

            // Evaluate substitution operators.
            if (B is Substitute)
                return Visit(L.Substitute(Set.MembersOf(R).Cast<Arrow>()));

            Real? LR = AsReal(L);
            Real? RR = AsReal(R);

            // Evaluate relational operators on constants.
            if (LR != null && RR != null)
            {
                switch (B.Operator)
                {
                    case Operator.Equal: return Constant.New(LR.Value == RR.Value);
                    case Operator.NotEqual: return Constant.New(LR.Value != RR.Value);
                    case Operator.Less: return Constant.New(LR.Value < RR.Value);
                    case Operator.Greater: return Constant.New(LR.Value <= RR.Value);
                    case Operator.LessEqual: return Constant.New(LR.Value > RR.Value);
                    case Operator.GreaterEqual: return Constant.New(LR.Value >= RR.Value);
                    case Operator.ApproxEqual: return Constant.New(
                        LR.Value == RR.Value || 
                        Real.Abs(LR.Value - RR.Value) < 1e-12 * Real.Max(Real.Abs(LR.Value), Real.Abs(RR.Value)));
                }
            }

            // Evaluate boolean operators if possible.
            switch (B.Operator)
            {
                case Operator.And:
                    if (IsFalse(LR) || IsFalse(RR))
                        return Constant.New(false);
                    else if (IsTrue(LR) && IsTrue(RR))
                        return Constant.New(true);
                    break;
                case Operator.Or:
                    if (IsTrue(LR) || IsTrue(RR))
                        return Constant.New(true);
                    else if (IsFalse(LR) && IsFalse(RR))
                        return Constant.New(false);
                    break;

                case Operator.Equal:
                case Operator.ApproxEqual:
                    if (L.Equals(R))
                        return Constant.New(true);
                    break;

                case Operator.NotEqual:
                    if (L.Equals(R))
                        return Constant.New(false);
                    break;
            }

            return Binary.New(B.Operator, L, R);
        }

        protected override Expression VisitUnary(Unary U)
        {
            Expression O = Visit(U.Operand);
            Real? C = AsReal(O);
            switch (U.Operator)
            {
                case Operator.Not:
                    if (IsTrue(C))
                        return Constant.New(false);
                    else if (IsFalse(C))
                        return Constant.New(true);
                    break;
            }

            return Unary.New(U.Operator, O);
        }

        // Get a nullable real from x.
        protected static Real? AsReal(Expression x)
        {
            if (x is Constant)
                return (Real)x;
            else
                return null;
        }

        // Get the constant real value from x, or the default if x is not constant.
        protected static Real AsReal(Expression x, Real Default)
        {
            if (x is Constant)
                return (Real)x;
            else
                return Default;
        }

        protected static bool EqualsZero(Real? R) { return R != null ? R.Value.EqualsZero() : false; }
        protected static bool EqualsOne(Real? R) { return R != null ? R.Value.EqualsOne() : false; }
        protected static bool IsTrue(Real? R) { return R != null ? !R.Value.EqualsZero() : false; }
        protected static bool IsFalse(Real? R) { return R != null ? R.Value.EqualsZero() : false; }
    }

    public static class EvaluateExtension
    {
        /// <summary>
        /// Evaluate expression x.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static Expression Evaluate(this Expression x) { return new EvaluateVisitor().Visit(x); }

        /// <summary>
        /// Evaluate an expression.
        /// </summary>
        /// <param name="f">Expression to evaluate.</param>
        /// <param name="x0">List of variable, value pairs to evaluate the function at.</param>
        /// <returns>The evaluated expression.</returns>
        public static Expression Evaluate(this Expression f, IDictionary<Expression, Expression> x0) { return f.Substitute(x0).Evaluate(); }

        /// <summary>
        /// Evaluate an expression at x = x0.
        /// </summary>
        /// <param name="f">Expression to evaluate.</param>
        /// <param name="x">Arrow expressions representing substitutions to evaluate.</param>
        /// <returns>The evaluated expression.</returns>
        public static Expression Evaluate(this Expression f, IEnumerable<Arrow> x) { return f.Evaluate(x.ToDictionary(i => i.Left, i => i.Right)); }
        public static Expression Evaluate(this Expression f, params Arrow[] x) { return f.Evaluate(x.AsEnumerable()); }

        /// <summary>
        /// Evaluate an expression at x = x0.
        /// </summary>
        /// <param name="f">Expression to evaluate.</param>
        /// <param name="x">Variable to evaluate at.</param>
        /// <param name="x0">Value to evaluate for.</param>
        /// <returns>The evaluated expression.</returns>
        public static Expression Evaluate(this Expression f, Expression x, Expression x0) { return f.Evaluate(new Dictionary<Expression, Expression> { { x, x0 } }); }

        public static IEnumerable<Expression> Evaluate(this IEnumerable<Expression> f, IDictionary<Expression, Expression> x0) 
        {
            EvaluateVisitor V = new EvaluateVisitor();
            return f.Select(i => V.Visit(i.Substitute(x0)));
        }
        public static IEnumerable<Expression> Evaluate(this IEnumerable<Expression> f, IEnumerable<Arrow> x) { return f.Evaluate(x.ToDictionary(i => i.Left, i => i.Right)); }
        public static IEnumerable<Expression> Evaluate(this IEnumerable<Expression> f, params Arrow[] x) { return f.Evaluate(x.AsEnumerable()); }
        public static IEnumerable<Expression> Evaluate(this IEnumerable<Expression> f, Expression x, Expression x0) { return f.Evaluate(new Dictionary<Expression, Expression> { { x, x0 } }); }
    }
}
