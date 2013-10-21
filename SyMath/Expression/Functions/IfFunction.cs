using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using LinqExpression = System.Linq.Expressions.Expression;

namespace SyMath
{
    /// <summary>
    /// If function.
    /// </summary>
    public class IfFunction : Function
    {
        private Variable[] parameters = new Variable[] { Variable.New("c"), Variable.New("t"), Variable.New("f") };
        public override IEnumerable<Variable> Parameters { get { return parameters; } }

        private IfFunction() : base("If") { }

        public static IfFunction New() { return new IfFunction(); }

        public override Expression Call(IEnumerable<Expression> Args)
        {
            Expression[] args = Args.ToArray();
            if (args[1].Equals(args[2]))
                return args[1];
            if (args[0].IsTrue())
                return args[1];
            else if (args[0].IsFalse())
                return args[2];
            else
                throw new ArgumentException();
        }

        public override bool CanCall(IEnumerable<Expression> Args)
        {
            return parameters.Count() == Args.Count();
        }

        public override bool CanCall()
        {
            return true;
        }

        public override LinqExpression CompileCall(IEnumerable<LinqExpression> Args, IEnumerable<Type> Libraries)
        {
            LinqExpression[] args = Args.ToArray();
            return LinqExpression.Condition(args[0], args[1], args[2]);
        }
    }
}
