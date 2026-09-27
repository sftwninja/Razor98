// LINQ to Objects, just the operators Razor uses. should behave like
// System.Core (deferred, stable sort, same exceptions).

using System.Collections;
using System.Collections.Generic;

namespace System.Linq
{
    public interface IGrouping<TKey, TElement> : IEnumerable<TElement>
    {
        TKey Key { get; }
    }

    public interface IOrderedEnumerable<TElement> : IEnumerable<TElement>
    {
        IOrderedEnumerable<TElement> CreateOrderedEnumerable<TKey>(Func<TElement, TKey> keySelector,
            IComparer<TKey> comparer, bool descending);
    }

    public interface ILookup<TKey, TElement> : IEnumerable<IGrouping<TKey, TElement>>
    {
        int Count { get; }
        IEnumerable<TElement> this[TKey key] { get; }
        bool Contains(TKey key);
    }

    public static class Enumerable
    {
        static InvalidOperationException NoElements()
        {
            return new InvalidOperationException("Sequence contains no elements");
        }

        static InvalidOperationException NoMatch()
        {
            return new InvalidOperationException("Sequence contains no matching element");
        }

        static InvalidOperationException MoreThanOne()
        {
            return new InvalidOperationException("Sequence contains more than one element");
        }

        static void NotNull(object value, string name)
        {
            if (value == null)
                throw new ArgumentNullException(name);
        }

        // Generation

        public static IEnumerable<TResult> Empty<TResult>()
        {
            return new TResult[0];
        }

        public static IEnumerable<int> Range(int start, int count)
        {
            if (count < 0 || (long)start + count - 1 > int.MaxValue)
                throw new ArgumentOutOfRangeException("count");
            return RangeIterator(start, count);
        }

        static IEnumerable<int> RangeIterator(int start, int count)
        {
            for (int i = 0; i < count; i++)
                yield return start + i;
        }

        public static IEnumerable<TResult> Repeat<TResult>(TResult element, int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException("count");
            return RepeatIterator(element, count);
        }

        static IEnumerable<TResult> RepeatIterator<TResult>(TResult element, int count)
        {
            for (int i = 0; i < count; i++)
                yield return element;
        }

        // Filtering and projection

        public static IEnumerable<TSource> Where<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            NotNull(source, "source");
            NotNull(predicate, "predicate");
            return WhereIterator(source, predicate);
        }

        static IEnumerable<TSource> WhereIterator<TSource>(IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            foreach (TSource item in source)
                if (predicate(item))
                    yield return item;
        }

        public static IEnumerable<TSource> Where<TSource>(this IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
        {
            NotNull(source, "source");
            NotNull(predicate, "predicate");
            return WhereIndexIterator(source, predicate);
        }

        static IEnumerable<TSource> WhereIndexIterator<TSource>(IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
        {
            int i = 0;
            foreach (TSource item in source)
                if (predicate(item, i++))
                    yield return item;
        }

        public static IEnumerable<TResult> Select<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            NotNull(source, "source");
            NotNull(selector, "selector");
            return SelectIterator(source, selector);
        }

        static IEnumerable<TResult> SelectIterator<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            foreach (TSource item in source)
                yield return selector(item);
        }

        public static IEnumerable<TResult> Select<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, int, TResult> selector)
        {
            NotNull(source, "source");
            NotNull(selector, "selector");
            return SelectIndexIterator(source, selector);
        }

        static IEnumerable<TResult> SelectIndexIterator<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, int, TResult> selector)
        {
            int i = 0;
            foreach (TSource item in source)
                yield return selector(item, i++);
        }

        public static IEnumerable<TResult> SelectMany<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, IEnumerable<TResult>> selector)
        {
            NotNull(source, "source");
            NotNull(selector, "selector");
            return SelectManyIterator(source, selector);
        }

        static IEnumerable<TResult> SelectManyIterator<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, IEnumerable<TResult>> selector)
        {
            foreach (TSource item in source)
                foreach (TResult inner in selector(item))
                    yield return inner;
        }

        public static IEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(this IEnumerable<TSource> source,
            Func<TSource, IEnumerable<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector)
        {
            NotNull(source, "source");
            NotNull(collectionSelector, "collectionSelector");
            NotNull(resultSelector, "resultSelector");
            return SelectManyIterator(source, collectionSelector, resultSelector);
        }

        static IEnumerable<TResult> SelectManyIterator<TSource, TCollection, TResult>(IEnumerable<TSource> source,
            Func<TSource, IEnumerable<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector)
        {
            foreach (TSource item in source)
                foreach (TCollection inner in collectionSelector(item))
                    yield return resultSelector(item, inner);
        }

        public static IEnumerable<TResult> Cast<TResult>(this IEnumerable source)
        {
            NotNull(source, "source");
            IEnumerable<TResult> typed = source as IEnumerable<TResult>;
            return typed ?? CastIterator<TResult>(source);
        }

        static IEnumerable<TResult> CastIterator<TResult>(IEnumerable source)
        {
            foreach (object item in source)
                yield return (TResult)item;
        }

        public static IEnumerable<TResult> OfType<TResult>(this IEnumerable source)
        {
            NotNull(source, "source");
            return OfTypeIterator<TResult>(source);
        }

        static IEnumerable<TResult> OfTypeIterator<TResult>(IEnumerable source)
        {
            foreach (object item in source)
                if (item is TResult)
                    yield return (TResult)item;
        }

        public static IEnumerable<TSource> AsEnumerable<TSource>(this IEnumerable<TSource> source)
        {
            return source;
        }

        // Partitioning

        public static IEnumerable<TSource> Take<TSource>(this IEnumerable<TSource> source, int count)
        {
            NotNull(source, "source");
            return TakeIterator(source, count);
        }

        static IEnumerable<TSource> TakeIterator<TSource>(IEnumerable<TSource> source, int count)
        {
            if (count <= 0)
                yield break;
            foreach (TSource item in source)
            {
                yield return item;
                if (--count == 0)
                    yield break;
            }
        }

        public static IEnumerable<TSource> Skip<TSource>(this IEnumerable<TSource> source, int count)
        {
            NotNull(source, "source");
            return SkipIterator(source, count);
        }

        static IEnumerable<TSource> SkipIterator<TSource>(IEnumerable<TSource> source, int count)
        {
            foreach (TSource item in source)
            {
                if (count > 0)
                {
                    count--;
                    continue;
                }
                yield return item;
            }
        }

        public static IEnumerable<TSource> TakeWhile<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            NotNull(source, "source");
            NotNull(predicate, "predicate");
            return TakeWhileIterator(source, predicate);
        }

        static IEnumerable<TSource> TakeWhileIterator<TSource>(IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            foreach (TSource item in source)
            {
                if (!predicate(item))
                    yield break;
                yield return item;
            }
        }

        public static IEnumerable<TSource> SkipWhile<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            NotNull(source, "source");
            NotNull(predicate, "predicate");
            return SkipWhileIterator(source, predicate);
        }

        static IEnumerable<TSource> SkipWhileIterator<TSource>(IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            bool yielding = false;
            foreach (TSource item in source)
            {
                if (!yielding && !predicate(item))
                    yielding = true;
                if (yielding)
                    yield return item;
            }
        }

        // Concatenation and sets

        public static IEnumerable<TSource> Concat<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
        {
            NotNull(first, "first");
            NotNull(second, "second");
            return ConcatIterator(first, second);
        }

        static IEnumerable<TSource> ConcatIterator<TSource>(IEnumerable<TSource> first, IEnumerable<TSource> second)
        {
            foreach (TSource item in first)
                yield return item;
            foreach (TSource item in second)
                yield return item;
        }

        public static IEnumerable<TSource> Append<TSource>(this IEnumerable<TSource> source, TSource element)
        {
            NotNull(source, "source");
            return ConcatIterator(source, new[] { element });
        }

        public static IEnumerable<TSource> Prepend<TSource>(this IEnumerable<TSource> source, TSource element)
        {
            NotNull(source, "source");
            return ConcatIterator(new[] { element }, source);
        }

        public static IEnumerable<TSource> Distinct<TSource>(this IEnumerable<TSource> source)
        {
            return Distinct(source, null);
        }

        public static IEnumerable<TSource> Distinct<TSource>(this IEnumerable<TSource> source, IEqualityComparer<TSource> comparer)
        {
            NotNull(source, "source");
            return DistinctIterator(source, comparer);
        }

        static IEnumerable<TSource> DistinctIterator<TSource>(IEnumerable<TSource> source, IEqualityComparer<TSource> comparer)
        {
            HashSet<TSource> seen = new HashSet<TSource>(comparer);
            foreach (TSource item in source)
                if (seen.Add(item))
                    yield return item;
        }

        public static IEnumerable<TSource> Union<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
        {
            return Union(first, second, null);
        }

        public static IEnumerable<TSource> Union<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource> comparer)
        {
            NotNull(first, "first");
            NotNull(second, "second");
            return DistinctIterator(ConcatIterator(first, second), comparer);
        }

        public static IEnumerable<TSource> Intersect<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
        {
            return Intersect(first, second, null);
        }

        public static IEnumerable<TSource> Intersect<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource> comparer)
        {
            NotNull(first, "first");
            NotNull(second, "second");
            return IntersectIterator(first, second, comparer);
        }

        static IEnumerable<TSource> IntersectIterator<TSource>(IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource> comparer)
        {
            HashSet<TSource> set = new HashSet<TSource>(second, comparer);
            foreach (TSource item in first)
                if (set.Remove(item))
                    yield return item;
        }

        public static IEnumerable<TSource> Except<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
        {
            return Except(first, second, null);
        }

        public static IEnumerable<TSource> Except<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource> comparer)
        {
            NotNull(first, "first");
            NotNull(second, "second");
            return ExceptIterator(first, second, comparer);
        }

        static IEnumerable<TSource> ExceptIterator<TSource>(IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource> comparer)
        {
            HashSet<TSource> set = new HashSet<TSource>(second, comparer);
            foreach (TSource item in first)
                if (set.Add(item))
                    yield return item;
        }

        public static IEnumerable<TSource> Reverse<TSource>(this IEnumerable<TSource> source)
        {
            NotNull(source, "source");
            return ReverseIterator(source);
        }

        static IEnumerable<TSource> ReverseIterator<TSource>(IEnumerable<TSource> source)
        {
            List<TSource> list = new List<TSource>(source);
            for (int i = list.Count - 1; i >= 0; i--)
                yield return list[i];
        }

        public static IEnumerable<TResult> Zip<TFirst, TSecond, TResult>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second,
            Func<TFirst, TSecond, TResult> resultSelector)
        {
            NotNull(first, "first");
            NotNull(second, "second");
            NotNull(resultSelector, "resultSelector");
            return ZipIterator(first, second, resultSelector);
        }

        static IEnumerable<TResult> ZipIterator<TFirst, TSecond, TResult>(IEnumerable<TFirst> first, IEnumerable<TSecond> second,
            Func<TFirst, TSecond, TResult> resultSelector)
        {
            using (IEnumerator<TFirst> a = first.GetEnumerator())
            using (IEnumerator<TSecond> b = second.GetEnumerator())
                while (a.MoveNext() && b.MoveNext())
                    yield return resultSelector(a.Current, b.Current);
        }

        public static IEnumerable<TSource> DefaultIfEmpty<TSource>(this IEnumerable<TSource> source)
        {
            return DefaultIfEmpty(source, default(TSource));
        }

        public static IEnumerable<TSource> DefaultIfEmpty<TSource>(this IEnumerable<TSource> source, TSource defaultValue)
        {
            NotNull(source, "source");
            return DefaultIfEmptyIterator(source, defaultValue);
        }

        static IEnumerable<TSource> DefaultIfEmptyIterator<TSource>(IEnumerable<TSource> source, TSource defaultValue)
        {
            bool any = false;
            foreach (TSource item in source)
            {
                any = true;
                yield return item;
            }
            if (!any)
                yield return defaultValue;
        }

        // Ordering

        public static IOrderedEnumerable<TSource> OrderBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            return OrderBy(source, keySelector, null);
        }

        public static IOrderedEnumerable<TSource> OrderBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey> comparer)
        {
            NotNull(source, "source");
            NotNull(keySelector, "keySelector");
            return new OrderedEnumerable<TSource>(source, null).Then(keySelector, comparer, false);
        }

        public static IOrderedEnumerable<TSource> OrderByDescending<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            return OrderByDescending(source, keySelector, null);
        }

        public static IOrderedEnumerable<TSource> OrderByDescending<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey> comparer)
        {
            NotNull(source, "source");
            NotNull(keySelector, "keySelector");
            return new OrderedEnumerable<TSource>(source, null).Then(keySelector, comparer, true);
        }

        public static IOrderedEnumerable<TSource> ThenBy<TSource, TKey>(this IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            NotNull(source, "source");
            return source.CreateOrderedEnumerable(keySelector, null, false);
        }

        public static IOrderedEnumerable<TSource> ThenBy<TSource, TKey>(this IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey> comparer)
        {
            NotNull(source, "source");
            return source.CreateOrderedEnumerable(keySelector, comparer, false);
        }

        public static IOrderedEnumerable<TSource> ThenByDescending<TSource, TKey>(this IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            NotNull(source, "source");
            return source.CreateOrderedEnumerable(keySelector, null, true);
        }

        public static IOrderedEnumerable<TSource> ThenByDescending<TSource, TKey>(this IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey> comparer)
        {
            NotNull(source, "source");
            return source.CreateOrderedEnumerable(keySelector, comparer, true);
        }

        sealed class OrderedEnumerable<TElement> : IOrderedEnumerable<TElement>
        {
            readonly IEnumerable<TElement> _source;
            // compare (element, index); later keys only break ties
            readonly Comparison<KeyValuePair<TElement, int>> _compare;

            public OrderedEnumerable(IEnumerable<TElement> source, Comparison<KeyValuePair<TElement, int>> compare)
            {
                _source = source;
                _compare = compare;
            }

            public OrderedEnumerable<TElement> Then<TKey>(Func<TElement, TKey> keySelector, IComparer<TKey> comparer, bool descending)
            {
                IComparer<TKey> cmp = comparer ?? Comparer<TKey>.Default;
                Comparison<KeyValuePair<TElement, int>> parent = _compare;
                Comparison<KeyValuePair<TElement, int>> next = delegate (KeyValuePair<TElement, int> x, KeyValuePair<TElement, int> y)
                {
                    if (parent != null)
                    {
                        int p = parent(x, y);
                        if (p != 0)
                            return p;
                    }
                    int c = cmp.Compare(keySelector(x.Key), keySelector(y.Key));
                    return descending ? -c : c;
                };
                return new OrderedEnumerable<TElement>(_source, next);
            }

            public IOrderedEnumerable<TElement> CreateOrderedEnumerable<TKey>(Func<TElement, TKey> keySelector, IComparer<TKey> comparer, bool descending)
            {
                return Then(keySelector, comparer, descending);
            }

            public IEnumerator<TElement> GetEnumerator()
            {
                List<KeyValuePair<TElement, int>> items = new List<KeyValuePair<TElement, int>>();
                int i = 0;
                foreach (TElement item in _source)
                    items.Add(new KeyValuePair<TElement, int>(item, i++));
                Comparison<KeyValuePair<TElement, int>> compare = _compare;
                // List.Sort isn't stable, use index for ties
                items.Sort(delegate (KeyValuePair<TElement, int> x, KeyValuePair<TElement, int> y)
                {
                    int c = compare(x, y);
                    return c != 0 ? c : x.Value.CompareTo(y.Value);
                });
                foreach (KeyValuePair<TElement, int> item in items)
                    yield return item.Key;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        // Grouping

        sealed class Grouping<TKey, TElement> : IGrouping<TKey, TElement>
        {
            internal readonly List<TElement> Elements = new List<TElement>();

            public Grouping(TKey key)
            {
                Key = key;
            }

            public TKey Key { get; private set; }

            public IEnumerator<TElement> GetEnumerator()
            {
                return Elements.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        sealed class Lookup<TKey, TElement> : ILookup<TKey, TElement>
        {
            // null key group kept separately
            readonly Dictionary<TKey, Grouping<TKey, TElement>> _groups;
            readonly List<Grouping<TKey, TElement>> _ordered = new List<Grouping<TKey, TElement>>();
            Grouping<TKey, TElement> _nullGroup;

            public Lookup(IEqualityComparer<TKey> comparer)
            {
                _groups = new Dictionary<TKey, Grouping<TKey, TElement>>(comparer ?? EqualityComparer<TKey>.Default);
            }

            public void Add(TKey key, TElement element)
            {
                Grouping<TKey, TElement> group;
                if (key == null)
                {
                    if (_nullGroup == null)
                    {
                        _nullGroup = new Grouping<TKey, TElement>(key);
                        _ordered.Add(_nullGroup);
                    }
                    group = _nullGroup;
                }
                else if (!_groups.TryGetValue(key, out group))
                {
                    group = new Grouping<TKey, TElement>(key);
                    _groups.Add(key, group);
                    _ordered.Add(group);
                }
                group.Elements.Add(element);
            }

            public int Count
            {
                get { return _ordered.Count; }
            }

            public IEnumerable<TElement> this[TKey key]
            {
                get
                {
                    Grouping<TKey, TElement> group;
                    if (key == null)
                        return _nullGroup != null ? (IEnumerable<TElement>)_nullGroup : new TElement[0];
                    return _groups.TryGetValue(key, out group) ? (IEnumerable<TElement>)group : new TElement[0];
                }
            }

            public bool Contains(TKey key)
            {
                return key == null ? _nullGroup != null : _groups.ContainsKey(key);
            }

            public IEnumerator<IGrouping<TKey, TElement>> GetEnumerator()
            {
                foreach (Grouping<TKey, TElement> group in _ordered)
                    yield return group;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public static IEnumerable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            return GroupBy(source, keySelector, (IEqualityComparer<TKey>)null);
        }

        public static IEnumerable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey> comparer)
        {
            NotNull(source, "source");
            NotNull(keySelector, "keySelector");
            return GroupByIterator(source, keySelector, comparer);
        }

        static IEnumerable<IGrouping<TKey, TSource>> GroupByIterator<TSource, TKey>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey> comparer)
        {
            foreach (IGrouping<TKey, TSource> group in ToLookup(source, keySelector, comparer))
                yield return group;
        }

        public static IEnumerable<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
        {
            NotNull(source, "source");
            NotNull(keySelector, "keySelector");
            NotNull(elementSelector, "elementSelector");
            return GroupByIterator(source, keySelector, elementSelector);
        }

        static IEnumerable<IGrouping<TKey, TElement>> GroupByIterator<TSource, TKey, TElement>(IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
        {
            foreach (IGrouping<TKey, TElement> group in ToLookup(source, keySelector, elementSelector))
                yield return group;
        }

        public static IEnumerable<TResult> GroupBy<TSource, TKey, TResult>(this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector, Func<TKey, IEnumerable<TSource>, TResult> resultSelector)
        {
            NotNull(resultSelector, "resultSelector");
            return Select(GroupBy(source, keySelector), delegate (IGrouping<TKey, TSource> g) { return resultSelector(g.Key, g); });
        }

        public static ILookup<TKey, TSource> ToLookup<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            return ToLookup(source, keySelector, (IEqualityComparer<TKey>)null);
        }

        public static ILookup<TKey, TSource> ToLookup<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey> comparer)
        {
            NotNull(source, "source");
            NotNull(keySelector, "keySelector");
            Lookup<TKey, TSource> lookup = new Lookup<TKey, TSource>(comparer);
            foreach (TSource item in source)
                lookup.Add(keySelector(item), item);
            return lookup;
        }

        public static ILookup<TKey, TElement> ToLookup<TSource, TKey, TElement>(this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
        {
            NotNull(source, "source");
            NotNull(keySelector, "keySelector");
            NotNull(elementSelector, "elementSelector");
            Lookup<TKey, TElement> lookup = new Lookup<TKey, TElement>(null);
            foreach (TSource item in source)
                lookup.Add(keySelector(item), elementSelector(item));
            return lookup;
        }

        public static IEnumerable<TResult> Join<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector)
        {
            NotNull(outer, "outer");
            NotNull(inner, "inner");
            NotNull(outerKeySelector, "outerKeySelector");
            NotNull(innerKeySelector, "innerKeySelector");
            NotNull(resultSelector, "resultSelector");
            return JoinIterator(outer, inner, outerKeySelector, innerKeySelector, resultSelector);
        }

        static IEnumerable<TResult> JoinIterator<TOuter, TInner, TKey, TResult>(IEnumerable<TOuter> outer, IEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector)
        {
            ILookup<TKey, TInner> lookup = ToLookup(inner, innerKeySelector);
            foreach (TOuter item in outer)
            {
                TKey key = outerKeySelector(item);
                if (key == null)
                    continue;
                foreach (TInner match in lookup[key])
                    yield return resultSelector(item, match);
            }
        }

        // Conversion

        public static TSource[] ToArray<TSource>(this IEnumerable<TSource> source)
        {
            NotNull(source, "source");
            return new List<TSource>(source).ToArray();
        }

        public static List<TSource> ToList<TSource>(this IEnumerable<TSource> source)
        {
            NotNull(source, "source");
            return new List<TSource>(source);
        }

        public static HashSet<TSource> ToHashSet<TSource>(this IEnumerable<TSource> source)
        {
            NotNull(source, "source");
            return new HashSet<TSource>(source);
        }

        public static Dictionary<TKey, TSource> ToDictionary<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            return ToDictionary(source, keySelector, delegate (TSource x) { return x; }, null);
        }

        public static Dictionary<TKey, TSource> ToDictionary<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey> comparer)
        {
            return ToDictionary(source, keySelector, delegate (TSource x) { return x; }, comparer);
        }

        public static Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
        {
            return ToDictionary(source, keySelector, elementSelector, null);
        }

        public static Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer)
        {
            NotNull(source, "source");
            NotNull(keySelector, "keySelector");
            NotNull(elementSelector, "elementSelector");
            Dictionary<TKey, TElement> result = new Dictionary<TKey, TElement>(comparer);
            foreach (TSource item in source)
                result.Add(keySelector(item), elementSelector(item));
            return result;
        }

        // Element operators

        public static TSource First<TSource>(this IEnumerable<TSource> source)
        {
            NotNull(source, "source");
            IList<TSource> list = source as IList<TSource>;
            if (list != null)
            {
                if (list.Count > 0)
                    return list[0];
            }
            else
            {
                foreach (TSource item in source)
                    return item;
            }
            throw NoElements();
        }

        public static TSource First<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            NotNull(source, "source");
            NotNull(predicate, "predicate");
            foreach (TSource item in source)
                if (predicate(item))
                    return item;
            throw NoMatch();
        }

        public static TSource FirstOrDefault<TSource>(this IEnumerable<TSource> source)
        {
            NotNull(source, "source");
            foreach (TSource item in source)
                return item;
            return default(TSource);
        }

        public static TSource FirstOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            NotNull(source, "source");
            NotNull(predicate, "predicate");
            foreach (TSource item in source)
                if (predicate(item))
                    return item;
            return default(TSource);
        }

        public static TSource Last<TSource>(this IEnumerable<TSource> source)
        {
            NotNull(source, "source");
            IList<TSource> list = source as IList<TSource>;
            if (list != null)
            {
                if (list.Count > 0)
                    return list[list.Count - 1];
                throw NoElements();
            }
            bool found = false;
            TSource last = default(TSource);
            foreach (TSource item in source)
            {
                found = true;
                last = item;
            }
            if (!found)
                throw NoElements();
            return last;
        }

        public static TSource Last<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            NotNull(source, "source");
            NotNull(predicate, "predicate");
            bool found = false;
            TSource last = default(TSource);
            foreach (TSource item in source)
            {
                if (predicate(item))
                {
                    found = true;
                    last = item;
                }
            }
            if (!found)
                throw NoMatch();
            return last;
        }

        public static TSource LastOrDefault<TSource>(this IEnumerable<TSource> source)
        {
            NotNull(source, "source");
            TSource last = default(TSource);
            foreach (TSource item in source)
                last = item;
            return last;
        }

        public static TSource LastOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            NotNull(source, "source");
            NotNull(predicate, "predicate");
            TSource last = default(TSource);
            foreach (TSource item in source)
                if (predicate(item))
                    last = item;
            return last;
        }

        public static TSource Single<TSource>(this IEnumerable<TSource> source)
        {
            NotNull(source, "source");
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                    throw NoElements();
                TSource result = e.Current;
                if (e.MoveNext())
                    throw MoreThanOne();
                return result;
            }
        }

        public static TSource Single<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            NotNull(source, "source");
            NotNull(predicate, "predicate");
            bool found = false;
            TSource result = default(TSource);
            foreach (TSource item in source)
            {
                if (!predicate(item))
                    continue;
                if (found)
                    throw MoreThanOne();
                found = true;
                result = item;
            }
            if (!found)
                throw NoMatch();
            return result;
        }

        public static TSource SingleOrDefault<TSource>(this IEnumerable<TSource> source)
        {
            NotNull(source, "source");
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                    return default(TSource);
                TSource result = e.Current;
                if (e.MoveNext())
                    throw MoreThanOne();
                return result;
            }
        }

        public static TSource SingleOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            NotNull(source, "source");
            NotNull(predicate, "predicate");
            bool found = false;
            TSource result = default(TSource);
            foreach (TSource item in source)
            {
                if (!predicate(item))
                    continue;
                if (found)
                    throw MoreThanOne();
                found = true;
                result = item;
            }
            return result;
        }

        public static TSource ElementAt<TSource>(this IEnumerable<TSource> source, int index)
        {
            NotNull(source, "source");
            IList<TSource> list = source as IList<TSource>;
            if (list != null)
                return list[index];
            if (index >= 0)
            {
                foreach (TSource item in source)
                    if (index-- == 0)
                        return item;
            }
            throw new ArgumentOutOfRangeException("index");
        }

        public static TSource ElementAtOrDefault<TSource>(this IEnumerable<TSource> source, int index)
        {
            NotNull(source, "source");
            if (index < 0)
                return default(TSource);
            IList<TSource> list = source as IList<TSource>;
            if (list != null)
                return index < list.Count ? list[index] : default(TSource);
            foreach (TSource item in source)
                if (index-- == 0)
                    return item;
            return default(TSource);
        }

        // Quantifiers

        public static bool Any<TSource>(this IEnumerable<TSource> source)
        {
            NotNull(source, "source");
            using (IEnumerator<TSource> e = source.GetEnumerator())
                return e.MoveNext();
        }

        public static bool Any<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            NotNull(source, "source");
            NotNull(predicate, "predicate");
            foreach (TSource item in source)
                if (predicate(item))
                    return true;
            return false;
        }

        public static bool All<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            NotNull(source, "source");
            NotNull(predicate, "predicate");
            foreach (TSource item in source)
                if (!predicate(item))
                    return false;
            return true;
        }

        public static bool Contains<TSource>(this IEnumerable<TSource> source, TSource value)
        {
            ICollection<TSource> collection = source as ICollection<TSource>;
            if (collection != null)
                return collection.Contains(value);
            return Contains(source, value, null);
        }

        public static bool Contains<TSource>(this IEnumerable<TSource> source, TSource value, IEqualityComparer<TSource> comparer)
        {
            NotNull(source, "source");
            IEqualityComparer<TSource> cmp = comparer ?? EqualityComparer<TSource>.Default;
            foreach (TSource item in source)
                if (cmp.Equals(item, value))
                    return true;
            return false;
        }

        public static bool SequenceEqual<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
        {
            return SequenceEqual(first, second, null);
        }

        public static bool SequenceEqual<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource> comparer)
        {
            NotNull(first, "first");
            NotNull(second, "second");
            IEqualityComparer<TSource> cmp = comparer ?? EqualityComparer<TSource>.Default;
            using (IEnumerator<TSource> a = first.GetEnumerator())
            using (IEnumerator<TSource> b = second.GetEnumerator())
            {
                while (a.MoveNext())
                {
                    if (!b.MoveNext() || !cmp.Equals(a.Current, b.Current))
                        return false;
                }
                return !b.MoveNext();
            }
        }

        // Aggregates

        public static int Count<TSource>(this IEnumerable<TSource> source)
        {
            NotNull(source, "source");
            ICollection<TSource> collection = source as ICollection<TSource>;
            if (collection != null)
                return collection.Count;
            ICollection legacy = source as ICollection;
            if (legacy != null)
                return legacy.Count;
            int count = 0;
            using (IEnumerator<TSource> e = source.GetEnumerator())
                while (e.MoveNext())
                    count++;
            return count;
        }

        public static int Count<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            NotNull(source, "source");
            NotNull(predicate, "predicate");
            int count = 0;
            foreach (TSource item in source)
                if (predicate(item))
                    count++;
            return count;
        }

        public static long LongCount<TSource>(this IEnumerable<TSource> source)
        {
            NotNull(source, "source");
            long count = 0;
            using (IEnumerator<TSource> e = source.GetEnumerator())
                while (e.MoveNext())
                    count++;
            return count;
        }

        public static TSource Aggregate<TSource>(this IEnumerable<TSource> source, Func<TSource, TSource, TSource> func)
        {
            NotNull(source, "source");
            NotNull(func, "func");
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                    throw NoElements();
                TSource result = e.Current;
                while (e.MoveNext())
                    result = func(result, e.Current);
                return result;
            }
        }

        public static TAccumulate Aggregate<TSource, TAccumulate>(this IEnumerable<TSource> source, TAccumulate seed,
            Func<TAccumulate, TSource, TAccumulate> func)
        {
            NotNull(source, "source");
            NotNull(func, "func");
            TAccumulate result = seed;
            foreach (TSource item in source)
                result = func(result, item);
            return result;
        }

        public static TResult Aggregate<TSource, TAccumulate, TResult>(this IEnumerable<TSource> source, TAccumulate seed,
            Func<TAccumulate, TSource, TAccumulate> func, Func<TAccumulate, TResult> resultSelector)
        {
            NotNull(resultSelector, "resultSelector");
            return resultSelector(Aggregate(source, seed, func));
        }

        public static int Sum(this IEnumerable<int> source)
        {
            NotNull(source, "source");
            int sum = 0;
            foreach (int v in source)
                sum = checked(sum + v);
            return sum;
        }

        public static long Sum(this IEnumerable<long> source)
        {
            NotNull(source, "source");
            long sum = 0;
            foreach (long v in source)
                sum = checked(sum + v);
            return sum;
        }

        public static double Sum(this IEnumerable<double> source)
        {
            NotNull(source, "source");
            double sum = 0;
            foreach (double v in source)
                sum += v;
            return sum;
        }

        public static float Sum(this IEnumerable<float> source)
        {
            NotNull(source, "source");
            double sum = 0;
            foreach (float v in source)
                sum += v;
            return (float)sum;
        }

        public static decimal Sum(this IEnumerable<decimal> source)
        {
            NotNull(source, "source");
            decimal sum = 0;
            foreach (decimal v in source)
                sum += v;
            return sum;
        }

        public static int Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector)
        {
            return Sum(Select(source, selector));
        }

        public static long Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, long> selector)
        {
            return Sum(Select(source, selector));
        }

        public static double Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector)
        {
            return Sum(Select(source, selector));
        }

        public static float Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, float> selector)
        {
            return Sum(Select(source, selector));
        }

        public static decimal Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal> selector)
        {
            return Sum(Select(source, selector));
        }

        public static double Average(this IEnumerable<int> source)
        {
            NotNull(source, "source");
            long sum = 0;
            long count = 0;
            foreach (int v in source)
            {
                sum = checked(sum + v);
                count++;
            }
            if (count == 0)
                throw NoElements();
            return (double)sum / count;
        }

        public static double Average(this IEnumerable<long> source)
        {
            NotNull(source, "source");
            long sum = 0;
            long count = 0;
            foreach (long v in source)
            {
                sum = checked(sum + v);
                count++;
            }
            if (count == 0)
                throw NoElements();
            return (double)sum / count;
        }

        public static double Average(this IEnumerable<double> source)
        {
            NotNull(source, "source");
            double sum = 0;
            long count = 0;
            foreach (double v in source)
            {
                sum += v;
                count++;
            }
            if (count == 0)
                throw NoElements();
            return sum / count;
        }

        public static double Average<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector)
        {
            return Average(Select(source, selector));
        }

        public static double Average<TSource>(this IEnumerable<TSource> source, Func<TSource, long> selector)
        {
            return Average(Select(source, selector));
        }

        public static double Average<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector)
        {
            return Average(Select(source, selector));
        }

        // like System.Core: empty value-type sequence throws, reference
        // type returns null, nulls skipped
        static TSource Extreme<TSource>(IEnumerable<TSource> source, int sign)
        {
            NotNull(source, "source");
            Comparer<TSource> cmp = Comparer<TSource>.Default;
            bool nullable = default(TSource) == null;
            bool found = false;
            TSource best = default(TSource);
            foreach (TSource item in source)
            {
                if (nullable && item == null)
                    continue;
                if (!found || cmp.Compare(item, best) * sign > 0)
                {
                    best = item;
                    found = true;
                }
            }
            if (!found && !nullable)
                throw NoElements();
            return best;
        }

        public static TSource Min<TSource>(this IEnumerable<TSource> source)
        {
            return Extreme(source, -1);
        }

        public static TSource Max<TSource>(this IEnumerable<TSource> source)
        {
            return Extreme(source, 1);
        }

        public static TResult Min<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            return Extreme(Select(source, selector), -1);
        }

        public static TResult Max<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            return Extreme(Select(source, selector), 1);
        }
    }
}
