using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using NSubstitute.Core;
using Unity.Core;

// this namespace contains types that must be public in order to be usable from patched assemblies, yet
// we do not want used from normal client api
namespace NSubstitute.Elevated.WeaverInternals
{
    // used when generating mocked default ctors
    public class MockPlaceholderType {}

    // important: keep all non-mscorlib types out of the public surface area of this class, so as to
    // avoid needing to add more references than NSubstitute.Elevated to the assembly during patching.

    public static class PatchedAssemblyBridge
    {
        // returns true if a mock is in place and it is taking over functionality. instance may be null
        // if static. mockedReturnValue is ignored in a void return func.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TryMock(Type actualType, object instance, Type mockedReturnType, out object mockedReturnValue, Type[] methodGenericTypes, object[] args)
        {
            if (!(SubstitutionContext.Current is ElevatedSubstitutionContext elevated))
            {
                mockedReturnValue = mockedReturnType.GetDefaultValue();
                return false;
            }

            var method = (MethodInfo) new StackTrace(1).GetFrame(0).GetMethod();

            if (method.IsGenericMethodDefinition)
                method = method.MakeGenericMethod(methodGenericTypes);

            return elevated.ElevatedSubstituteManager.TryMock(actualType, instance, mockedReturnType, out mockedReturnValue, method, methodGenericTypes, args);
        }
    }
}
