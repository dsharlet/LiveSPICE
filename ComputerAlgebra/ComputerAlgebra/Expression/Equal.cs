using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ComputerAlgebra
{
    /// <summary>
    /// Represents an x == y expression.
    /// </summary>
    public class Equal : Binary
    {
        protected Equal(Expression L, Expression R) : base(Operator.Equal, L, R) { }
        public static Equal New(Expression L, Expression R) { return new Equal(L, R); }
    }
}
