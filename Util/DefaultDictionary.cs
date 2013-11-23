using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Collections.Generic
{
    /// <summary>
    /// Dictionary implementing behavior similar to std::map for operator [].
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class DefaultDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    {
        private TValue def = default(TValue);
        /// <summary>
        /// The default value for new values.
        /// </summary>
        public TValue Default { get { return def; } set { def = value; } }

        public DefaultDictionary() { }
        public DefaultDictionary(TValue Default) { def = Default; }
        public DefaultDictionary(IDictionary<TKey, TValue> Copy) : base(Copy) { }
        public DefaultDictionary(IDictionary<TKey, TValue> Copy, TValue Default) : base(Copy) { def = Default; }

        /// <summary>
        /// The same as Dictionary's indexer, except if the key does not exist, return Default.
        /// </summary>
        /// <param name="K"></param>
        /// <returns></returns>
        public new TValue this[TKey K]
        {
            get
            {
                TValue V;
                if (base.TryGetValue(K, out V))
                    return V;
                else
                    return Default;
            }
            set
            {
                if (Equals(Default, value))
                    base.Remove(K);
                else
                    base[K] = value;
            }
        }
    }
}
