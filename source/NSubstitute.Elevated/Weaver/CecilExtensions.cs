using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Mono.Cecil;
using Unity.Core;

namespace NSubstitute.Elevated.Weaver
{
    public enum IncludeNested { No, Yes }

    public static class CecilExtensions
    {
        [NotNull]
        public static IEnumerable<TypeDefinition> SelectTypes([NotNull] this AssemblyDefinition @this, IncludeNested includeNested)
        {
            var types = @this.Modules.SelectMany(m => m.Types);
            if (includeNested == IncludeNested.Yes)
                types = types.SelectMany(t => t.NestedTypes.Append(t));
            return types;
        }

        public static int InheritanceChainLength([NotNull] this TypeReference @this)
        {
            if (@this.DeclaringType == null)
                return 0;

            var baseType = @this.Resolve().BaseType;
            if (baseType == null)
                return 1;

            return 1 + InheritanceChainLength(baseType);
        }
    }
}
