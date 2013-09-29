using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyMath
{
    /// <summary>
    /// Dictionary implementing behavior similar to std::map for operator [].
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class DefaultDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    {
        public DefaultDictionary() { }
        public DefaultDictionary(TValue Default) { def = Default; }
        public DefaultDictionary(IDictionary<TKey, TValue> Copy) : base(Copy) { }
        public DefaultDictionary(IDictionary<TKey, TValue> Copy, TValue Default) : base(Copy) { def = Default; }

        protected TValue def = default(TValue);

        /// <summary>
        /// The default value for new values.
        /// </summary>
        public TValue Default
        {
            get { return def; }
            set { def = value; }
        }

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
                if (!base.TryGetValue(K, out V))
                {
                    V = def != null ? def : (TValue)Activator.CreateInstance(typeof(TValue));
                    base[K] = V;
                }
                return V;
            }
            set
            {
                base[K] = value;
            }
        }
    }

    /// <summary>
    /// DefaultDictionary that uses Default = 0, suitable for summing expressions.
    /// </summary>
    public class SumDictionary : DefaultDictionary<Expression, Expression>
    {
        public SumDictionary() : base(Constant.Zero) { }
        public SumDictionary(IDictionary<Expression, Expression> Copy) : base(Copy, Constant.Zero) { }
    }
}
