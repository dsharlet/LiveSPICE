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

        private List<Variable> parameters;
        public override IEnumerable<Variable> Parameters { get { return parameters; } }

        private ExprFunction(string Name, Expression Body, IEnumerable<Variable> Params) : base(Name) { body = Body; parameters = Params.ToList(); }

        public static ExprFunction New(string Name, Expression Body, IEnumerable<Variable> Params) { return new ExprFunction(Name, Body, Params); }
        public static ExprFunction New(string Name, Expression Body, params Variable[] Params) { return new ExprFunction(Name, Body, Params); }
        public static ExprFunction New(string Name, IEnumerable<Variable> Params) { return new ExprFunction(Name, null, Params); }
        public static ExprFunction New(string Name, params Variable[] Params) { return new ExprFunction(Name, null, Params); }

        public override Expression Call(IEnumerable<Expression> Args)
        {
            return body.Evaluate(parameters.Zip(Args, (a, b) => Arrow.New(a, b)));
        }

        public override bool CanCall(IEnumerable<Expression> Args)
        {
            return CanCall() && parameters.Count() == Args.Count();
        }

        public override bool CanCall()
        {
            return !ReferenceEquals(body, null);
        }

        public override LinqExpression CompileCall(IEnumerable<LinqExpression> Args, IEnumerable<Type> Libraries)
        {
            // Try using the base first, which will call a function from one of the libraries if possible. 
            try
            {
                return base.CompileCall(Args, Libraries);
            }
            catch (System.Exception)
            {
                return body.Compile(parameters.Zip(Args, (i, j) => new { i, j }).ToDictionary(i => (Expression)i.i, i => i.j), Libraries);
            }
        }
    }
}
