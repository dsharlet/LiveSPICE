using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ComputerAlgebra
{
    public static class Combinatorics
    {
        /// <summary>
        /// Enumerate the permutations of x.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="n"></param>
        /// <returns></returns>
        public static IEnumerable<IList<T>> Permutations<T>(this IEnumerable<T> n)
        {
            List<T> l = n.ToList();
            return Permutations(l, l.Count);
        }

        private static IEnumerable<IList<T>> Permutations<T>(IList<T> n, int r)
        {
            if (r == 1)
            {
                yield return n;
            }
            else
            {
                for (int i = 0; i < r; i++)
                {
                    foreach (var j in Permutations(n, r - 1))
                        yield return j;

                    T t = n[r - 1];
                    n.RemoveAt(r - 1);
                    n.Insert(0, t);
                }
            }
        }
        
        /// <summary>
        /// Enumerate the combinations of n of length r.
        /// From: http://www.extensionmethod.net/csharp/ienumerable-t/combinations
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="n"></param>
        /// <param name="r"></param>
        /// <returns></returns>
        public static IEnumerable<IEnumerable<T>> Combinations<T>(this IEnumerable<T> n, int r)
        {
            if (r == 0)
                return new[] { new T[0] };
            else
                return n.SelectMany((e, i) => n.Skip(i + 1).Combinations(r - 1).Select(c => new[] { e }.Concat(c)));
        }
    }
}
