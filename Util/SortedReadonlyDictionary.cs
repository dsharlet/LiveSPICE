using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Util
{
    [DebuggerTypeProxy(typeof(IDictionaryDebugView<,>))]
    [DebuggerDisplay("Count = {Count}")]
    public class SortedReadonlyDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
    {
        private readonly Dictionary<TKey, int> _indexMap;
        private readonly TValue[] _values;

        public TValue[] RawData => _values;

        public SortedReadonlyDictionary(IReadOnlyDictionary<TKey, TValue> source)
        {
            _indexMap = source.Select((kv, idx) => (kv, idx)).ToDictionary(item => item.kv.Key, item => item.idx);
            _values = source.Values.ToArray();
        }

        public TValue this[TKey key] => _values[_indexMap[key]];

        public IEnumerable<TKey> Keys => _indexMap.Keys;

        public IEnumerable<TValue> Values => _values;

        public int Count => _values.Length;

        public bool ContainsKey(TKey key) => _indexMap.ContainsKey(key);

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _indexMap.Select(i => new KeyValuePair<TKey, TValue>(i.Key, _values[i.Value])).GetEnumerator();
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (_indexMap.TryGetValue(key, out var idx))
            {
                value = _values[idx];
                return true;
            }
            value = default;
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
