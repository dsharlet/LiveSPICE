using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using LinqExpression = System.Linq.Expressions.Expression;

namespace SyMath
{
    /// <summary>
    /// Function defined by an expression.
    /// </summary>
    public class ExprFunction : Function
    {
        private Expression body;
        public Expression Body { get { return body; } }

        private List<Variable> args;
        public IEnumerable<Variable> Arguments { get { return args; } }

        private ExprFunction(string Name, Expression Body, IEnumerable<Variable> Args) : base(Name) { body = Body; args = Args.ToList(); }

        public static ExprFunction New(string Name, Expression Body, IEnumerable<Variable> Args) { return new ExprFunction(Name, Body, Args); }
        public static ExprFunction New(string Name, Expression Body, params Variable[] Args) { return new ExprFunction(Name, Body, Args); }
        public static ExprFunction New(string Name, IEnumerable<Variable> Args) { return new ExprFunction(Name, null, Args); }
        public static ExprFunction New(string Name, params Variable[] Args) { return new ExprFunction(Name, null, Args); }

        public override Expression Call(IEnumerable<Expression> Params)
        {
            return body.Evaluate(Arguments.Zip(Params, (a, b) => Arrow.New(a, b)));
        }

        public override bool CanCall(IEnumerable<Expression> Params)
        {
            return CanCall() && Arguments.Count() == Params.Count();
        }

        public override bool CanCall()
        {
            return !ReferenceEquals(body, null);
        }

        public override LinqExpression Compile(IEnumerable<LinqExpression> Args)
        {
            return body.Compile(args.Zip(Args, (i, j) => new { i, j }).ToDictionary(i => (Expression)i.i, i => i.j));
        }
    }
}
