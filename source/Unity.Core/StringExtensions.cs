using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Unity.Core
{
    public static class StringExtensions
    {
        [ContractAnnotation("null=>true", true), Pure]
        public static bool IsNullOrEmpty([CanBeNull] this string @this) => string.IsNullOrEmpty(@this);
        [ContractAnnotation("null=>true", true), Pure]
        public static bool IsNullOrWhiteSpace([CanBeNull] this string @this) => string.IsNullOrWhiteSpace(@this);

        public static bool IsEmpty([NotNull] this string @this) => @this.Length == 0;
        public static bool Any([NotNull] this string @this) => @this.Length != 0;

        // left/mid/right are 'basic' inspired names, and never throw

        [NotNull]
        public static string Left([NotNull] this string @this, int maxChars)
        {
            return @this.Substring(0, Math.Min(maxChars, @this.Length));
        }

        [NotNull]
        public static string Mid([NotNull] this string @this, int offset, int maxChars = -1)
        {
            if (offset < 0)
                throw new ArgumentException("offset must be >= 0", nameof(offset));

            var safeOffset = offset.Clamp(0, @this.Length);
            var actualMaxChars = @this.Length - safeOffset;

            var safeMaxChars = maxChars < 0 ? actualMaxChars : Math.Min(maxChars, actualMaxChars);

            return @this.Substring(safeOffset, safeMaxChars);
        }

        [NotNull]
        public static string Right([NotNull] this string @this, int maxChars)
        {
            var safeMaxChars = Math.Min(maxChars, @this.Length);
            return @this.Substring(@this.Length - safeMaxChars, safeMaxChars);
        }

        [NotNull]
        public static string StringJoin([NotNull] this IEnumerable @this, [NotNull] string separator)
        => string.Join(separator, @this.Cast<object>());

        [NotNull]
        public static string StringJoin([NotNull] this IEnumerable @this, char separator)
        => string.Join(new string(separator, 1), @this.Cast<object>());

        [NotNull]
        public static string StringJoin<T, TSelected>([NotNull] this IEnumerable<T> @this, [NotNull] Func<T, TSelected> selector, [NotNull] string separator)
        => string.Join(separator, @this.Select(selector));

        [NotNull]
        public static string StringJoin<T, TSelected>([NotNull] this IEnumerable<T> @this, [NotNull] Func<T, TSelected> selector, char separator)
        => string.Join(new string(separator, 1), @this.Select(selector));
    }
}
