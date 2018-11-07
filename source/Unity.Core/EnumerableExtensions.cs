using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Enumerable = System.Linq.Enumerable;

namespace Unity.Core
{
    public static class EnumerableExtensions
    {
        // should instead be an IReadOnlyList<T> (jetbrains apparently uses this type to detect "safe to double-walk",
        // but our framework doesn't have that type yet. so we have to hack it with ICollection, which is unfortunately
        // read-write. extra bad because we're "unwrapping" what should be a safe enumerable. compromises.. :(
        [NotNull]
        public static ICollection<T> UnDefer<T>([NotNull] this IEnumerable<T> @this)
        => @this as ICollection<T> ?? @this.ToList(); // don't use ToArray, it does extra work

        [NotNull]
        public static IEnumerable<T> WhereNotNull<T>([NotNull] this IEnumerable<T> @this) where T : class
        => @this.Where(item => !ReferenceEquals(item, null));

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

        public static IEnumerable<T> SelectMany<T>([NotNull] this IEnumerable<IEnumerable<T>> @this)
        => @this.SelectMany(_ => _);
    }
}
