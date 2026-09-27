// HashSet<T> and the Concurrent types. concurrent ones are just a lock,
// good enough for what Razor does with them.

using System.Collections;
using System.Collections.Generic;

namespace System.Collections.Generic
{
    [Serializable]
    public class HashSet<T> : ICollection<T>
    {
        // Dictionary won't take a null key
        readonly Dictionary<T, bool> _items;
        bool _hasNull;

        public HashSet() : this((IEqualityComparer<T>)null)
        {
        }

        public HashSet(IEqualityComparer<T> comparer)
        {
            _items = new Dictionary<T, bool>(comparer ?? EqualityComparer<T>.Default);
        }

        public HashSet(IEnumerable<T> collection) : this(collection, null)
        {
        }

        public HashSet(IEnumerable<T> collection, IEqualityComparer<T> comparer) : this(comparer)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");
            UnionWith(collection);
        }

        public IEqualityComparer<T> Comparer
        {
            get { return _items.Comparer; }
        }

        public int Count
        {
            get { return _items.Count + (_hasNull ? 1 : 0); }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Add(T item)
        {
            if (item == null)
            {
                if (_hasNull)
                    return false;
                _hasNull = true;
                return true;
            }
            if (_items.ContainsKey(item))
                return false;
            _items.Add(item, true);
            return true;
        }

        void ICollection<T>.Add(T item)
        {
            Add(item);
        }

        public bool Remove(T item)
        {
            if (item == null)
            {
                bool had = _hasNull;
                _hasNull = false;
                return had;
            }
            return _items.Remove(item);
        }

        public bool Contains(T item)
        {
            return item == null ? _hasNull : _items.ContainsKey(item);
        }

        public void Clear()
        {
            _items.Clear();
            _hasNull = false;
        }

        public void UnionWith(IEnumerable<T> other)
        {
            if (other == null)
                throw new ArgumentNullException("other");
            foreach (T item in other)
                Add(item);
        }

        public void ExceptWith(IEnumerable<T> other)
        {
            if (other == null)
                throw new ArgumentNullException("other");
            foreach (T item in other)
                Remove(item);
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            if (other == null)
                throw new ArgumentNullException("other");
            HashSet<T> keep = new HashSet<T>(other, Comparer);
            List<T> drop = new List<T>();
            foreach (T item in this)
                if (!keep.Contains(item))
                    drop.Add(item);
            foreach (T item in drop)
                Remove(item);
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            if (other == null)
                throw new ArgumentNullException("other");
            foreach (T item in other)
                if (Contains(item))
                    return true;
            return false;
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            if (other == null)
                throw new ArgumentNullException("other");
            HashSet<T> set = new HashSet<T>(other, Comparer);
            if (set.Count != Count)
                return false;
            foreach (T item in set)
                if (!Contains(item))
                    return false;
            return true;
        }

        public int RemoveWhere(Predicate<T> match)
        {
            if (match == null)
                throw new ArgumentNullException("match");
            List<T> drop = new List<T>();
            foreach (T item in this)
                if (match(item))
                    drop.Add(item);
            foreach (T item in drop)
                Remove(item);
            return drop.Count;
        }

        public void CopyTo(T[] array)
        {
            CopyTo(array, 0);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            foreach (T item in this)
                array[arrayIndex++] = item;
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (_hasNull)
                yield return default(T);
            foreach (T item in _items.Keys)
                yield return item;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

namespace System.Collections.Concurrent
{
    public class ConcurrentDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        readonly Dictionary<TKey, TValue> _items;
        readonly object _sync = new object();

        public ConcurrentDictionary()
        {
            _items = new Dictionary<TKey, TValue>();
        }

        public ConcurrentDictionary(IEqualityComparer<TKey> comparer)
        {
            _items = new Dictionary<TKey, TValue>(comparer);
        }

        public ConcurrentDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection) : this()
        {
            foreach (KeyValuePair<TKey, TValue> pair in collection)
                _items[pair.Key] = pair.Value;
        }

        public TValue this[TKey key]
        {
            get
            {
                lock (_sync)
                    return _items[key];
            }
            set
            {
                lock (_sync)
                    _items[key] = value;
            }
        }

        public int Count
        {
            get
            {
                lock (_sync)
                    return _items.Count;
            }
        }

        public bool IsEmpty
        {
            get { return Count == 0; }
        }

        // snapshots, same as the real one
        public ICollection<TKey> Keys
        {
            get
            {
                lock (_sync)
                    return new List<TKey>(_items.Keys);
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                lock (_sync)
                    return new List<TValue>(_items.Values);
            }
        }

        public bool ContainsKey(TKey key)
        {
            lock (_sync)
                return _items.ContainsKey(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            lock (_sync)
                return _items.TryGetValue(key, out value);
        }

        public bool TryAdd(TKey key, TValue value)
        {
            lock (_sync)
            {
                if (_items.ContainsKey(key))
                    return false;
                _items.Add(key, value);
                return true;
            }
        }

        public bool TryRemove(TKey key, out TValue value)
        {
            lock (_sync)
            {
                if (!_items.TryGetValue(key, out value))
                    return false;
                _items.Remove(key);
                return true;
            }
        }

        public bool TryUpdate(TKey key, TValue newValue, TValue comparisonValue)
        {
            lock (_sync)
            {
                TValue current;
                if (!_items.TryGetValue(key, out current) || !EqualityComparer<TValue>.Default.Equals(current, comparisonValue))
                    return false;
                _items[key] = newValue;
                return true;
            }
        }

        public TValue GetOrAdd(TKey key, TValue value)
        {
            lock (_sync)
            {
                TValue current;
                if (_items.TryGetValue(key, out current))
                    return current;
                _items.Add(key, value);
                return value;
            }
        }

        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            lock (_sync)
            {
                TValue current;
                if (_items.TryGetValue(key, out current))
                    return current;
                current = valueFactory(key);
                _items.Add(key, current);
                return current;
            }
        }

        public TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
        {
            lock (_sync)
            {
                TValue current;
                TValue result = _items.TryGetValue(key, out current) ? updateValueFactory(key, current) : addValue;
                _items[key] = result;
                return result;
            }
        }

        public TValue AddOrUpdate(TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
        {
            lock (_sync)
            {
                TValue current;
                TValue result = _items.TryGetValue(key, out current) ? updateValueFactory(key, current) : addValueFactory(key);
                _items[key] = result;
                return result;
            }
        }

        public void Clear()
        {
            lock (_sync)
                _items.Clear();
        }

        public KeyValuePair<TKey, TValue>[] ToArray()
        {
            lock (_sync)
            {
                KeyValuePair<TKey, TValue>[] result = new KeyValuePair<TKey, TValue>[_items.Count];
                ((ICollection<KeyValuePair<TKey, TValue>>)_items).CopyTo(result, 0);
                return result;
            }
        }

        // snapshot so callers can modify while enumerating
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<TKey, TValue>>)ToArray()).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class ConcurrentQueue<T> : IEnumerable<T>
    {
        readonly Queue<T> _items = new Queue<T>();
        readonly object _sync = new object();

        public int Count
        {
            get
            {
                lock (_sync)
                    return _items.Count;
            }
        }

        public bool IsEmpty
        {
            get { return Count == 0; }
        }

        public void Enqueue(T item)
        {
            lock (_sync)
                _items.Enqueue(item);
        }

        public bool TryDequeue(out T result)
        {
            lock (_sync)
            {
                if (_items.Count == 0)
                {
                    result = default(T);
                    return false;
                }
                result = _items.Dequeue();
                return true;
            }
        }

        public bool TryPeek(out T result)
        {
            lock (_sync)
            {
                if (_items.Count == 0)
                {
                    result = default(T);
                    return false;
                }
                result = _items.Peek();
                return true;
            }
        }

        public T[] ToArray()
        {
            lock (_sync)
                return _items.ToArray();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)ToArray()).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

namespace System.Threading.Tasks
{
    // only here so the using in ClassicUOManager.cs compiles
    internal static class NamespacePlaceholder
    {
    }
}
