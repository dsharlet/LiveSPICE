using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ComputerAlgebra
{
    /// <summary>
    /// IEqualityComparer implementing reference equality.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ReferenceEqualityComparer<T> : EqualityComparer<T>
    {
        public override bool Equals(T a, T b) { return ReferenceEquals(a, b); }
        public override int GetHashCode(T obj) { return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj); }
    }
}
