using System.Collections.Generic;
using System.Linq;

namespace Util
{
    public static class IEnumerableExtensions
    {
        public static IEnumerable<(T, int)> WithIndex<T>(this IEnumerable<T> src)
        {
            return src.Select<T, (T, int)>((T x, int index) => (x, index));
        }
    }
}
