using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Collections.Generic
{
    /// <summary>
    /// Extensions for List.
    /// </summary>
    static class ListExtensions
    {
        private class SortComparer<T, TKey> : IComparer<T> where TKey : IComparable<TKey>
        {
            private Func<T, TKey> selector;

            public SortComparer(Func<T, TKey> Selector) { selector = Selector; }

            public int Compare(T L, T R) { return selector(L).CompareTo(selector(R)); }
        }

        /// <summary>
        /// Sort the list using a selector, as in IEnumeable.OrderBy.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="This"></param>
        /// <param name="Selector"></param>
        public static void Sort<T, TKey>(this List<T> This, Func<T, TKey> Selector) where TKey : IComparable<TKey>
        {
            This.Sort(new SortComparer<T, TKey>(Selector));
        }
    }
}
