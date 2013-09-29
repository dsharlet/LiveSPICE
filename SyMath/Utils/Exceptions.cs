using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyMath
{
    /// <summary>
    /// Exception thrown for issues with algebraic manipulations.
    /// </summary>
    public class AlgebraException : System.Exception
    {
        public AlgebraException(string Message) : base(Message) { }
    }
}
