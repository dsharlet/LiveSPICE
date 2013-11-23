using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyMath
{
    /// <summary>
    /// DefaultDictionary that uses Default = 0, suitable for summing expressions.
    /// </summary>
    public class SumDictionary : DefaultDictionary<Expression, Expression>
    {
        public SumDictionary() : base(0) { }
        public SumDictionary(IDictionary<Expression, Expression> Copy) : base(Copy, 0) { }
    }
}
