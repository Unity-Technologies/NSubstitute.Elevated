using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Enumerable = System.Linq.Enumerable;

namespace Unity.Core
{
    public static class EnumerableExtensions
    {
        [NotNull]
        public static IEnumerable<T> WhereNotNull<T>([NotNull] this IEnumerable<T> @this) where T : class
        => @this.Where(item => !(item is null));

        [NotNull]
        public static IEnumerable<T> OrEmpty<T>([CanBeNull] this IEnumerable<T> @this)
        => @this ?? Enumerable.Empty<T>();

        [NotNull]
        public static HashSet<T> ToHashSet<T>([NotNull] this IEnumerable<T> @this, IEqualityComparer<T> comparer)
        => new HashSet<T>(@this, comparer);

        [NotNull]
        public static HashSet<T> ToHashSet<T>([NotNull] this IEnumerable<T> @this)
        => new HashSet<T>(@this);

        [NotNull]
        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>([NotNull] this IEnumerable<(TKey key, TValue value)> @this)
        => @this.ToDictionary(item => item.key, item => item.value);

        public static IEnumerable<T> Append<T>([NotNull] this IEnumerable<T> @this, T value)
        {
            foreach (var i in @this)
                yield return i;
            yield return value;
        }

        public static IEnumerable<T> Prepend<T>([NotNull] this IEnumerable<T> @this, T value)
        {
            yield return value;
            foreach (var i in @this)
                yield return i;
        }

        public static bool IsNullOrEmpty<T>([CanBeNull] this IEnumerable<T> @this)
        => @this == null || !@this.Any();
    }
}
