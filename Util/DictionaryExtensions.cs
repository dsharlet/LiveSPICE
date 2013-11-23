using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Collections.Generic
{
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Remove all dictionary elements matching the given predicate.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="This"></param>
        /// <param name="Predicate"></param>
        public static void RemoveAll<TKey, TValue>(this IDictionary<TKey, TValue> This,
            Func<KeyValuePair<TKey, TValue>, bool> Predicate)
        {
            var Keys = This.Where(i => Predicate(i)).ToList();
            foreach (var i in Keys)
                This.Remove(i.Key);
        }
    }
}
