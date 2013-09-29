using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using LinqExpression = System.Linq.Expressions.Expression;

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
        /// <param name="Params"></param>
        /// <returns></returns>
        public abstract Expression Call(IEnumerable<Expression> Params);
        public Expression Call(params Expression[] Params) { return Call(Params.AsEnumerable()); }

        /// <summary>
        /// Check if this function could be called with the given parameters.
        /// </summary>
        /// <param name="Params"></param>
        /// <returns></returns>
        public virtual bool CanCall(IEnumerable<Expression> Params) { return CanCall(); }
        public virtual bool CanCall() { return true; }

        /// <summary>
        /// Compile function
        /// </summary>
        /// <param name="Args"></param>
        /// <returns></returns>
        public virtual LinqExpression Compile(IEnumerable<LinqExpression> Args) { throw new NotImplementedException("Cannot compile function " + Name + " of type " + GetType().FullName); }

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
