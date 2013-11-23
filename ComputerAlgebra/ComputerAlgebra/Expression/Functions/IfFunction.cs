using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using LinqExpression = System.Linq.Expressions.Expression;

namespace ComputerAlgebra
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

            // If both branches are equal, just return one of them as the result.
            if (args[1].Equals(args[2]))
                return args[1];

            // Try to evaluate the condition.
            if (args[0].IsTrue())
                return args[1];
            else if (args[0].IsFalse())
                return args[2];

            // Couldn't evaluate with these arguments.
            throw new ArgumentException();
        }

        public override bool CanCall(IEnumerable<Expression> Args) { return parameters.Count() == Args.Count(); }
        public override bool CanCall() { return true; }
    }
}
