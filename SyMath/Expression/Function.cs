using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Reflection;
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
        /// Compile a call to this function to a native function call.
        /// </summary>
        /// <param name="Args"></param>
        /// <param name="Libraries"></param>
        /// <returns></returns>
        public virtual LinqExpression CompileCall(IEnumerable<LinqExpression> Args, IEnumerable<Type> Libraries)
        {
            // Get the types of the compiled arguments.
            Type[] types = Args.Select(i => i.Type).ToArray();

            // Find a method with the same name and matching arguments.
            MethodInfo method = null;
            foreach (Type i in Libraries)
            {
                // If the method is not found, check the base type.
                for (Type t = i; t != null; t = t.BaseType)
                {
                    MethodInfo m = t.GetMethod(Name, BindingFlags.Static | BindingFlags.Public, null, types, null);
                    if (m != null)
                    {
                        // If we already found a method, throw ambiguous.
                        if (method != null)
                            throw new AmbiguousMatchException(Name);
                        method = m;
                        break;
                    }
                }
            }

            // Generate a call to the found method.
            if (method != null)
                return LinqExpression.Call(method, Args);
            else
                throw new InvalidOperationException("Could not find method for function '" + Name + "'");
        }

        /// <summary>
        /// Compile function to a lambda.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Compile<T>(IEnumerable<Type> Libraries)
        {
            Dictionary<Expression, LinqExpression> map = new Dictionary<Expression, LinqExpression>();
            List<LinqExpressions.ParameterExpression> parameters = new List<LinqExpressions.ParameterExpression>();

            // Get the invoke function for T so we can get the types of the arguments.
            MethodInfo invoke = typeof(T).GetMethod("Invoke");
            foreach (var i in Parameters.Zip(invoke.GetParameters(), (a, b) => new { a, b }))
            {
                LinqExpressions.ParameterExpression p = LinqExpression.Parameter(i.b.ParameterType, i.a.Name);
                map.Add(i.a, p);
                parameters.Add(p);
            }
            
            return LinqExpression.Lambda<T>(
                Call(Parameters).Compile(map, Libraries),
                parameters).Compile();
        }

        public T Compile<T>() { return Compile<T>(null); }

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
