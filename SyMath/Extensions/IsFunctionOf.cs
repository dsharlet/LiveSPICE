using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyMath
{
    /// <summary>
    /// Visitor for finding variables in expressions. Returns null if the expression is a function of x.
    /// </summary>
    class IsFunctionOfVisitor : RecursiveExpressionVisitor
    {
        protected List<Expression> x;

        public IsFunctionOfVisitor(IEnumerable<Expression> x) { this.x = x.ToList(); }

        public override Expression Visit(Expression E)
        {
            if (x.Contains(E))
                return null;
            return base.Visit(E);
        }
    }

    public static class IsFunctionOfExtension
    {
        /// <summary>
        /// Check if f is a function of any variable in x.
        /// </summary>
        /// <param name="f"></param>
        /// <param name="x"></param>
        /// <returns>true if f is a function of any variable in x.</returns>
        public static bool IsFunctionOf(this Expression f, IEnumerable<Expression> x)
        {
            return ReferenceEquals(new IsFunctionOfVisitor(x).Visit(f), null);
        }

        /// <summary>
        /// Check if f is a function of x.
        /// </summary>
        /// <param name="f"></param>
        /// <param name="x"></param>
        /// <returns>true if f is a function of x.</returns>
        public static bool IsFunctionOf(this Expression f, params Expression[] x) 
        { 
            return IsFunctionOf(f, x.AsEnumerable());
        }

        /// <summary>
        /// Check if f is a function of x.
        /// </summary>
        /// <param name="f"></param>
        /// <param name="x"></param>
        /// <returns>true if f is a function of x.</returns>
        public static bool IsFunctionOf(this Expression f, Expression x)
        {
            return IsFunctionOf(f, Set.MembersOf(x));
        }
    }
}
