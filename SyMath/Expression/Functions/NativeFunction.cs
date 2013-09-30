using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using LinqExpression = System.Linq.Expressions.Expression;

namespace SyMath
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
    /// Attribute to identify the method to call for compilation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class CompileTarget : Attribute
    {
        private MethodInfo target;

        public CompileTarget(MethodInfo Target) { target = Target; }
        public CompileTarget(Delegate Target) { target = Target.Method; }
        public CompileTarget(Type T, string Name) { target = T.GetMethod(Name); }

        public MethodInfo Target { get { return target; } }
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
            object Result;
            if (Method.IsStatic)
                Result = Method.Invoke(null, Args.ToArray<object>());
            else
                Result = Method.Invoke(Args.First(), Args.Skip(1).ToArray<object>());

            if (Result is Expression)
                return Result as Expression;
            else
                return Constant.New(Result);
        }

        public override bool CanCall(IEnumerable<Expression> Args)
        {
            return Method.GetParameters().Length == Args.Count() - (Method.IsStatic ? 0 : 1);
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
                C = SyMath.Call.New(C.Target, C.Arguments.Select(i => i.Substitute(now)));

            if (late.Empty())
                return C;
            else
                return SyMath.Substitute.New(C, late.Count > 1 ? (Expression)Set.New(late) : late.Single());
        }

        public override LinqExpression Compile(IEnumerable<LinqExpression> Args)
        {
            CompileTarget compiled = method.GetCustomAttribute<CompileTarget>();
            if (compiled != null)
                return LinqExpression.Call(compiled.Target, Args);
            throw new NotImplementedException("Cannot compile method " + Name);
        }

        public override bool Equals(Expression E)
        {
            NativeFunction F = E as NativeFunction;
            if (F != null)
                return method == F.method;
            return base.Equals(E);
        }
    }
}
