using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using LinqExpression = System.Linq.Expressions.Expression;

namespace ComputerAlgebra
{
    /// <summary>
    /// Attribute for disallowing substition through a function.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class NoSubstitute : Attribute
    {
        public NoSubstitute() { }
    }
    
    /// <summary>
    /// Function defined by a native function.
    /// </summary>
    public class NativeFunction : Function
    {
        private MethodInfo method;
        public MethodInfo Method { get { return method; } }

        public override IEnumerable<Variable> Parameters
        {
            get { return Method.GetParameters().Select(i => Variable.New(i.Name)); }
        }

        private NativeFunction(MethodInfo Method) : base(Method.Name) { method = Method; }

        public static NativeFunction New(MethodInfo Method) { return new NativeFunction(Method); }

        public override Expression Call(IEnumerable<Expression> Args)
        {
            object _this = null;
            if (!Method.IsStatic)
            {
                _this = Args.First();
                if (!Method.DeclaringType.IsAssignableFrom(_this.GetType()))
                    return null;
                Args = Args.Skip(1);
            }
            if (!Args.Zip(Method.GetParameters(), (a, p) => p.ParameterType.IsAssignableFrom(a.GetType())).All())
                return null;

            try
            {
                object ret = Method.Invoke(_this, Args.ToArray<object>());
                if (ret is Expression)
                    return ret as Expression;
                else
                    return Constant.New(ret);
            }
            catch (TargetInvocationException Ex)
            {
                throw Ex.InnerException;
            }
        }

        public override bool CanCall(IEnumerable<Expression> Args)
        {
            if (!Method.IsStatic)
                Args = Args.Skip(1);

            return Method.GetParameters().Length == Args.Count();
        }
        
        public override Expression Substitute(Call C, IDictionary<Expression, Expression> x0, bool IsTransform)
        {
            if (IsTransform)
                return base.Substitute(C, x0, IsTransform);

            Dictionary<Expression, Expression> now = new Dictionary<Expression, Expression>(x0);
            List<Arrow> late = new List<Arrow>();

            foreach (var i in method.GetParameters().Zip(C.Arguments, (p, a) => new { p, a }))
            {
                if (i.p.GetCustomAttribute<NoSubstitute>() != null)
                {
                    if (now.ContainsKey(i.a))
                    {
                        late.Add(Arrow.New(i.a, now[i.a]));
                        now.Remove(i.a);
                    }
                }
            }

            if (!now.Empty())
                C = ComputerAlgebra.Call.New(C.Target, C.Arguments.Select(i => i.Substitute(now)));

            if (late.Empty())
                return C;
            else
                return ComputerAlgebra.Substitute.New(C, late.Count > 1 ? (Expression)Set.New(late) : late.Single());
        }
        
        public override bool Equals(Expression E)
        {
            NativeFunction F = E as NativeFunction;
            if (ReferenceEquals(F, null)) return false;

            return method.Equals(F.method);
        }
        public override int GetHashCode() { return method.GetHashCode(); }
    }
}
