using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using LinqExpression = System.Linq.Expressions.Expression;
using LinqExpressions = System.Linq.Expressions;

namespace SyMath
{
    /// <summary>
    /// Base class for a function.
    /// </summary>
    public abstract class Function : NamedAtom
    {
        protected Function(string Name) : base(Name) { }

        /// <summary>
        /// 
        /// </summary>
        public abstract IEnumerable<Variable> Parameters { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Args"></param>
        /// <returns></returns>
        public abstract Expression Call(IEnumerable<Expression> Args);
        public Expression Call(params Expression[] Args) { return Call(Args.AsEnumerable()); }

        /// <summary>
        /// Check if this function could be called with the given parameters.
        /// </summary>
        /// <param name="Args"></param>
        /// <returns></returns>
        public virtual bool CanCall(IEnumerable<Expression> Args) { return CanCall(); }
        public virtual bool CanCall() { return true; }

        /// <summary>
        /// Compile function call.
        /// </summary>
        /// <param name="Args"></param>
        /// <returns></returns>
        public virtual LinqExpression Compile(IEnumerable<LinqExpression> Args) { throw new NotImplementedException("Cannot compile function " + Name + " of type " + GetType().FullName); }

        /// <summary>
        /// Compile function to a lambda.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public LinqExpressions.Expression<T> Compile<T>() 
        { 
            IEnumerable<Expression> Args = Parameters.Select((i, j) => Variable.New("x" + j.ToString()));
            return Call(Args).Compile<T>(Args);
        }

        /// <summary>
        /// Substitute the variables into the expressions.
        /// </summary>
        /// <param name="x0"></param>
        /// <param name="IsTransform"></param>
        /// <returns></returns>
        public virtual Expression Substitute(Call C, IDictionary<Expression, Expression> x0, bool IsTransform)
        {
            return SyMath.Call.New(C.Target, C.Arguments.Select(i => i.Substitute(x0)));
        }
    }
}
