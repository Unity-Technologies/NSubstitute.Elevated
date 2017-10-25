using System;
using System.Collections.Generic;
using System.Linq;

namespace NSubstitute.Elevated
{
    public static class Extensions
    {
        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> @this, TKey key, Func<TKey, TValue> createFunc)
        {
            if (@this.TryGetValue(key, out var found))
                return found;

            found = createFunc(key);
            @this.Add(key, found);
            return found;
        }

        public static object GetDefaultValue(this Type @this)
        {
            object defaultValue = null;
            if (@this.IsValueType && @this != typeof(void))
                defaultValue = Activator.CreateInstance(@this);
            return defaultValue;
        }

        public static bool NullOrEmpty<T>(this IEnumerable<T> @this)
        {
            return @this == null || !@this.Any();
        }
    }
}
