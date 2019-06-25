using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace NSubstitute.Elevated.RuntimeInjection {
    class TryMockProxyGenerator
    {
        struct Signature
        {
            public MethodInfo MethodInfo { get; }

            Signature(MethodInfo methodInfo)
            {
                MethodInfo = methodInfo;
            }

            public bool Equals(Signature s)
            {
                // TODO: improve!
                return MethodInfo.Name == s.MethodInfo.Name;
            }

            public static Signature For(MethodInfo methodInfo)
            {
                return new Signature(methodInfo);
            }
        }

        Dictionary<Signature, Delegate> m_Cache = new Dictionary<Signature, Delegate>(new SignatureComparer());

        struct SignatureComparer : IEqualityComparer<Signature>
        {
            public bool Equals(Signature x, Signature y)
            {
                var xmi = x.MethodInfo;
                var ymi = y.MethodInfo;

                if (xmi.IsStatic != ymi.IsStatic)
                    return false;
                
                if (xmi.ReturnType != ymi.ReturnType)
                    return false;

                var xparams = xmi.GetParameters();
                var yparams = ymi.GetParameters();
                
                if (xparams.Length != yparams.Length)
                    return false;

                for (var i = 0; i < xparams.Length; ++i)
                {
                    if (xparams[i].ParameterType != yparams[i].ParameterType)
                        return false;

                    if (xparams[i].Attributes != yparams[i].Attributes)
                        return false;
                }
                
                return x.Equals(y);
            }

            public int GetHashCode(Signature s)
            {
                var hashCode = s.MethodInfo.ReturnType.GetHashCode();

                foreach (var parameterInfo in s.MethodInfo.GetParameters())
                {
                    hashCode ^= parameterInfo.ParameterType.GetHashCode();
                }

                if (s.MethodInfo.IsStatic)
                    hashCode ^= 43867;
                
                // TODO: generic args
                // TODO: other stuff?
                return hashCode;
            }
        }

        internal void Purge()
        {
            m_Cache.Clear();
        }

        internal MethodInfo GetOrCreateTryMockProxyFor(MethodInfo methodInfo)
        {
            var signature = Signature.For(methodInfo);
            if (m_Cache.TryGetValue(signature, out var @delegate))
                return @delegate.GetMethodInfo();
            
            // TODO: cache the proxy based on the provided information.
            // TODO: make sure names are unique.

            var dynamicMethod = new DynamicMethod(
                $"{methodInfo.Name}_Proxy_{methodInfo.GetParameters().Length}",
                methodInfo.Attributes,
                methodInfo.CallingConvention,
                methodInfo.ReturnType,
                methodInfo.GetParameters().Select(p => p.ParameterType).ToArray(),
                methodInfo.Module,
                true);

            var tryMockMethod = typeof(SubstituteManager).GetMethod(nameof(SubstituteManager.TryMockWrapper));
            var getTypeFromHandle = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle));

            var generator = dynamicMethod.GetILGenerator();

            // object returnValue;
            generator.DeclareLocal(typeof(object));

            // var a = typeof(StaticClass);
            generator.Emit(OpCodes.Ldtoken, methodInfo.DeclaringType);
            generator.Emit(OpCodes.Call, getTypeFromHandle);

            // var b = null;
            generator.Emit(OpCodes.Ldnull);

            // var c = typeof(int);
            generator.Emit(OpCodes.Ldtoken, typeof(int));
            generator.Emit(OpCodes.Call, getTypeFromHandle);

            // out returnValue
            generator.Emit(OpCodes.Ldloca_S, 0);

            // var d = new Type[0];
            generator.Emit(OpCodes.Ldc_I4_0);
            generator.Emit(OpCodes.Newarr, typeof(Type));

            // var e = new[] { (object) i };
            generator.Emit(OpCodes.Ldc_I4_1); // size
            generator.Emit(OpCodes.Newarr, typeof(object));
            generator.Emit(OpCodes.Dup);
            generator.Emit(OpCodes.Ldc_I4_0); // index
            generator.Emit(OpCodes.Ldarg_0); // arg-index
            generator.Emit(OpCodes.Box, typeof(int)); // convert type to System.Object
            generator.Emit(OpCodes.Stelem_Ref);

            // ElevatedMockingSupport.TryMock(a, b, c, out returnValue, d, e);
            generator.Emit(OpCodes.Call, tryMockMethod);
            generator.Emit(OpCodes.Pop);

            //return (int)returnValue;
            generator.Emit(OpCodes.Ldloc_0);
            generator.Emit(OpCodes.Unbox_Any, methodInfo.ReturnType); // TODO: convert
            generator.Emit(OpCodes.Ret);

            foreach (var parameterInfo in methodInfo.GetParameters())
            {
                dynamicMethod.DefineParameter(parameterInfo.Position, parameterInfo.Attributes, parameterInfo.Name);
            }

            @delegate = dynamicMethod.CreateDelegate(
                typeof(Func<,>).MakeGenericType( // TODO: this should be dynamic
                    methodInfo.GetParameters()[0].ParameterType,
                    methodInfo.ReturnType));

            m_Cache.Add(signature, @delegate);
            
            // TODO: are we leaking this?
            return @delegate.GetMethodInfo();
        }
    }
}