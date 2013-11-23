using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ComputerAlgebra
{
    /// <summary>
    /// Represents an x : y expression.
    /// </summary>
    public class Substitute : Binary
    {
        protected Substitute(Expression L, Expression R) : base(Operator.Substitute, L, R) { }
        public static Substitute New(Expression L, Expression R) { return new Substitute(L, R); }
    }
}
