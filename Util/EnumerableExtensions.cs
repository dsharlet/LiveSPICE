using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace System.Collections.Generic
{
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Test if an IEnumerable is empty.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="This"></param>
        /// <returns></returns>
        [DebuggerStepThrough]
        public static bool Empty<T>(this IEnumerable<T> This) { return !This.Any(); }

        /// <summary>
        /// Returns true if Predicate maps none of the elements of the IEnumerable map to true.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="This"></param>
        /// <param name="Predicate"></param>
        /// <returns></returns>
        [DebuggerStepThrough]
        public static bool None<T>(this IEnumerable<T> This, Func<T, bool> Predicate) { return !This.Any(Predicate); }

        /// <summary>
        /// Cast This to List.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="This"></param>
        /// <returns></returns>
        [DebuggerStepThrough]
        public static List<T> AsList<T>(this IEnumerable<T> This)
        {
            if (This is List<T>)
                return (List<T>)This;
            else
                return This.ToList();
        }

        /// <summary>
        /// Enumerate an IEnumerable, except elements in E.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="This"></param>
        /// <param name="Excepted"></param>
        /// <param name="Comparer"></param>
        /// <returns></returns>
        [DebuggerStepThrough]
        public static IEnumerable<T> ExceptUnique<T>(this IEnumerable<T> This, IEnumerable<T> Excepted, IEqualityComparer<T> Comparer)
        {
            // TODO: Can this be done without creating a new list?
            List<T> excepted = Excepted.ToList();
            foreach (T i in This)
            {
                if (excepted.Contains(i, Comparer))
                    excepted.Remove(i);
                else
                    yield return i;
            }
        }
        [DebuggerStepThrough]
        public static IEnumerable<T> ExceptUnique<T>(this IEnumerable<T> This, IEnumerable<T> Excepted)
        {
            return ExceptUnique(This, Excepted, EqualityComparer<T>.Default);
        }

        /// <summary>
        /// Except a single element from an IEnumerable.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="This"></param>
        /// <param name="Excepted"></param>
        /// <param name="Comparer"></param>
        /// <returns></returns>
        [DebuggerStepThrough]
        public static IEnumerable<T> Except<T>(this IEnumerable<T> This, T Excepted, IEqualityComparer<T> Comparer)
        {
            return This.Where(i => !Comparer.Equals(Excepted, i));
        }
        [DebuggerStepThrough]
        public static IEnumerable<T> Except<T>(this IEnumerable<T> This, T Excepted)
        {
            return Except(This, Excepted, EqualityComparer<T>.Default);
        }
        [DebuggerStepThrough]
        public static IEnumerable<T> ExceptUnique<T>(this IEnumerable<T> This, T Excepted, IEqualityComparer<T> Comparer)
        {
            bool found = false;
            foreach (T i in This)
            {
                if (!found && Comparer.Equals(i, Excepted))
                    found = true;
                else
                    yield return i;
            }
        }
        [DebuggerStepThrough]
        public static IEnumerable<T> ExceptUnique<T>(this IEnumerable<T> This, T Excepted)
        {
            return ExceptUnique(This, Excepted, EqualityComparer<T>.Default);
        }

        /// <summary>
        /// 3 way Zip.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <param name="C"></param>
        /// <param name="f"></param>
        /// <returns></returns>
        [DebuggerStepThrough]
        public static IEnumerable<R> Zip<T, U, V, R>(this IEnumerable<T> A, IEnumerable<U> B, IEnumerable<V> C, Func<T, U, V, R> f)
        {
            return A.Zip(B.Zip(C, (i, j) => new Tuple<U, V>(i, j)), (i, j) => f(i, j.Item1, j.Item2));
        }

        /// <summary>
        /// Check if all of the elements in the enumeration are true.
        /// </summary>
        /// <param name="This"></param>
        /// <returns></returns>
        [DebuggerStepThrough]
        public static bool All(this IEnumerable<bool> This) { return This.All(i => i); }

        /// <summary>
        /// Check if any of the elements in the enumeration are true.
        /// </summary>
        /// <param name="This"></param>
        /// <returns></returns>
        [DebuggerStepThrough]
        public static bool Any(this IEnumerable<bool> This) { return This.Any(i => i); }

        /// <summary>
        /// Append elements to an IEnumerable.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="This"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        [DebuggerStepThrough]
        public static IEnumerable<T> Append<T>(this IEnumerable<T> This, params T[] x)
        {
            return This.Concat(x);
        }

        /// <summary>
        /// Lexical comparison between IEnumerables L and R.
        /// </summary>
        /// <param name="L"></param>
        /// <param name="R"></param>
        /// <returns></returns>
        [DebuggerStepThrough]
        public static int LexicalCompareTo<T>(this IEnumerable<T> L, IEnumerable<T> R) where T : IComparable<T>
        {
            IEnumerator<T> LI = L.GetEnumerator();
            IEnumerator<T> RI = R.GetEnumerator();

            while (true)
            {
                bool LN = LI.MoveNext();
                bool RN = RI.MoveNext();

                if (LN && RN)
                {
                    int compare = LI.Current.CompareTo(RI.Current);
                    if (compare != 0)
                        return compare;
                }
                else if (LN)
                    return 1;
                else if (RN)
                    return -1;
                else
                    return 0;
            }
        }

        /// <summary>
        /// Find the minimum of a value and the minimum of the sequence mapped by a predicate.
        /// </summary>
        /// <param name="This">Sequence to find the minimum of.</param>
        /// <param name="Predicate">Function to apply to the sequence values.</param>
        /// <param name="Init">The initial value of the minimum.</param>
        /// <returns>The minimum of Init and the minimum of the sequence.</returns>
        [DebuggerStepThrough]
        public static TMin Min<T, TMin>(this IEnumerable<T> This, Func<T, TMin> Predicate, TMin Init) where TMin : IComparable<TMin>
        {
            TMin min = Init;
            foreach (T i in This)
            {
                TMin pi = Predicate(i);
                if (pi.CompareTo(min) < 0)
                    min = pi;
            }
            return min;
        }
        /// <summary>
        /// Find the maximum of a value and the maximum of the sequence mapped by a predicate.
        /// </summary>
        /// <param name="This">Sequence to find the maximum of.</param>
        /// <param name="Predicate">Function to apply to the sequence values.</param>
        /// <param name="Init">The initial value of the maximum.</param>
        /// <returns>The maximum of Init and the maximum of the sequence.</returns>
        [DebuggerStepThrough]
        public static TMax Max<T, TMax>(this IEnumerable<T> This, Func<T, TMax> Predicate, TMax Init) where TMax : IComparable<TMax>
        {
            TMax max = Init;
            foreach (T i in This)
            {
                TMax pi = Predicate(i);
                if (pi.CompareTo(max) > 0)
                    max = pi;
            }
            return max;
        }

        /// <summary>
        /// Return which element of this IEnumerable has the minimum value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TMin"></typeparam>
        /// <param name="This"></param>
        /// <param name="Predicate"></param>
        /// <returns></returns>
        [DebuggerStepThrough]
        public static T ArgMin<T, TMin>(this IEnumerable<T> This, Func<T, TMin> Predicate) where TMin : IComparable<TMin>
        {
            T min = This.First();
            TMin arg = Predicate(min);
            foreach (T i in This.Skip(1))
            {
                TMin argi = Predicate(i);
                if (argi.CompareTo(arg) < 0)
                {
                    arg = argi;
                    min = i;
                }
            }
            return min;
        }
        /// <summary>
        /// Return which element of this IEnumerable has the maximum value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TMin"></typeparam>
        /// <param name="This"></param>
        /// <param name="Predicate"></param>
        /// <returns></returns>
        [DebuggerStepThrough]
        public static T ArgMax<T, TMax>(this IEnumerable<T> This, Func<T, TMax> Predicate) where TMax : IComparable<TMax>
        {
            T max = This.First();
            TMax arg = Predicate(max);
            foreach (T i in This.Skip(1))
            {
                TMax argi = Predicate(i);
                if (argi.CompareTo(arg) > 0)
                {
                    arg = argi;
                    max = i;
                }
            }
            return max;
        }
        
        /// <summary>
        /// Reverse of a String.Split operation.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="List"></param>
        /// <param name="Delim"></param>
        /// <returns></returns>
        [DebuggerStepThrough]
        public static string UnSplit<T>(this IEnumerable<T> This, string Delim)
        {
            StringBuilder S = new StringBuilder();
            if (This.Any())
            {
                S.Append(This.First().ToString());
                foreach (T i in This.Skip(1))
                    S.Append(Delim + i.ToString());
            }
            return S.ToString();
        }
        [DebuggerStepThrough]
        public static string UnSplit<T>(this IEnumerable<T> This, char Delim) { return UnSplit(This, new string(Delim, 1)); }
        [DebuggerStepThrough]
        public static string UnSplit<T>(this IEnumerable<T> This) { return UnSplit(This, ""); }

        /// <summary>
        /// Compute the hash of an ordered IEnumerable.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="This"></param>
        /// <returns></returns>
        [DebuggerStepThrough]
        public static int OrderedHashCode<T>(this IEnumerable<T> This)
        {
            int hash = 5381;
            foreach (T i in This)
                hash = 33 * hash + i.GetHashCode();
            return hash;
        }

        /// <summary>
        /// Compute the hash of an unordered IEnumerable.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="This"></param>
        /// <returns></returns>
        [DebuggerStepThrough]
        public static int UnorderedHashCode<T>(this IEnumerable<T> This)
        {
            int hash = 0;
            foreach (T i in This)
                hash = hash ^ i.GetHashCode();
            return hash;
        }

        /// <summary>
        /// Cache the results of an IEnumerable.
        /// From: http://www.extensionmethod.net/csharp/ienumerable-t/cache
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        [DebuggerStepThrough]
        public static IEnumerable<T> Cache<T>(this IEnumerable<T> source)
        {
            return CacheHelper(source.GetEnumerator());
        }

        private static IEnumerable<T> CacheHelper<T>(IEnumerator<T> source)
        {
            var isEmpty = new Lazy<bool>(() => !source.MoveNext());
            var head = new Lazy<T>(() => source.Current);
            var tail = new Lazy<IEnumerable<T>>(() => CacheHelper(source));

            return CacheHelper(isEmpty, head, tail);
        }

        private static IEnumerable<T> CacheHelper<T>(
            Lazy<bool> isEmpty,
            Lazy<T> head,
            Lazy<IEnumerable<T>> tail)
        {
            if (isEmpty.Value)
                yield break;

            yield return head.Value;
            foreach (var value in tail.Value)
                yield return value;
        }
    }
}
