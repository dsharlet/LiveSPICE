using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Reflection.Emit;
using SyMath;
using LinqExpressions = System.Linq.Expressions;
using LinqExpression = System.Linq.Expressions.Expression;

namespace Circuit
{
    // Holds an instance of T and a LinqExpression that maps to the instance.
    class GlobalExpr<T>
    {
        private T x;
        public T Value { get { return x; } set { x = value; } }

        // A Linq Expression to refer to the voltage at this node.
        private LinqExpression expr;
        public LinqExpression Expr { get { return expr; } }

        public static implicit operator LinqExpression(GlobalExpr<T> G) { return G.expr; }

        public GlobalExpr() { expr = LinqExpression.Field(LinqExpression.Constant(this), typeof(GlobalExpr<T>), "x"); }
        public GlobalExpr(T Init) : this() { x = Init; }
    }
}
