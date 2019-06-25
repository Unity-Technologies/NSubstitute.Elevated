using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace NSubstitute.Elevated.RuntimeInjection {
    class TryMockProxyGenerator
    {
        Dictionary<MethodInfo, Delegate> m_Cache = new Dictionary<MethodInfo, Delegate>(new MethodInfoComparer());

        struct MethodInfoComparer : IEqualityComparer<MethodInfo>
        {
            public bool Equals(MethodInfo xmi, MethodInfo ymi)
            {
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
                
                return xmi.Equals(ymi);
            }

            public int GetHashCode(MethodInfo s)
            {
                var hashCode = s.ReturnType.GetHashCode();

                foreach (var parameterInfo in s.GetParameters())
                {
                    hashCode ^= parameterInfo.ParameterType.GetHashCode();
                }

                if (s.IsStatic)
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
            if (m_Cache.TryGetValue(methodInfo, out var @delegate))
                return @delegate.GetMethodInfo();

            var parameterInfos = methodInfo.GetParameters();
            var dynamicMethod = new DynamicMethod(
                $"{methodInfo.Name}_Proxy_{methodInfo.GetHashCode()}",
                methodInfo.Attributes,
                methodInfo.CallingConvention,
                methodInfo.ReturnType,
                parameterInfos.Select(p => p.ParameterType).ToArray(),
                methodInfo.Module,
                true);

            var tryMockMethod = typeof(SubstituteManager).GetMethod(nameof(SubstituteManager.TryMockWrapper));
            var getTypeFromHandle = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle));

            var generator = dynamicMethod.GetILGenerator();

            // object returnValue;
            generator.DeclareLocal(typeof(object));

            // var a = typeof(methodInfo.DeclaringType);
            generator.Emit(OpCodes.Ldtoken, methodInfo.DeclaringType);
            generator.Emit(OpCodes.Call, getTypeFromHandle);

            // var b = null;
            generator.Emit(OpCodes.Ldnull);

            // var c = typeof(methodInfo.ReturnType);
            generator.Emit(OpCodes.Ldtoken, methodInfo.ReturnType);
            generator.Emit(OpCodes.Call, getTypeFromHandle);

            // out returnValue
            generator.Emit(OpCodes.Ldloca_S, 0);

            // Generic Arguments
            // var d = new Type[0];
            generator.Emit(OpCodes.Ldc_I4_0);
            generator.Emit(OpCodes.Newarr, typeof(Type));

            // Arguments
            // args = new[parameterInfos.Length];
            generator.Emit(OpCodes.Ldc_I4, parameterInfos.Length); // TODO: this will fail with > then Int.MaxValue parameters ...
            generator.Emit(OpCodes.Newarr, typeof(object));

            var index = 0;
            foreach (var parameterInfo in parameterInfos)
            {
                // args[index] = (object) arg;
                generator.Emit(OpCodes.Dup);
                generator.Emit(OpCodes.Ldc_I4, index); // index
                generator.Emit(OpCodes.Ldarg, index); // arg-index
                if(parameterInfo.ParameterType.IsValueType)
                    generator.Emit(OpCodes.Box, parameterInfo.ParameterType);
                generator.Emit(OpCodes.Stelem_Ref);
                ++index;
            }

            // ElevatedMockingSupport.TryMock(a, b, c, out returnValue, d, args);
            generator.Emit(OpCodes.Call, tryMockMethod);
            generator.Emit(OpCodes.Pop);

            //return (methodInfo.ReturnType)returnValue;
            if (methodInfo.ReturnType.IsValueType)
            {
                generator.Emit(OpCodes.Ldloc_0);
                generator.Emit(OpCodes.Unbox_Any, methodInfo.ReturnType);
                generator.Emit(OpCodes.Ret);
            }
            else
            {
                generator.Emit(OpCodes.Ldloc_0);
                generator.Emit(OpCodes.Castclass, methodInfo.ReturnType);
                generator.Emit(OpCodes.Ret);
            }

            foreach (var parameterInfo in parameterInfos)
            {
                dynamicMethod.DefineParameter(parameterInfo.Position, parameterInfo.Attributes, parameterInfo.Name);
            }

            @delegate = dynamicMethod.CreateDelegate(DelegateTypeFor(methodInfo));

            m_Cache.Add(methodInfo, @delegate);
            
            // TODO: are we leaking this?
            return @delegate.GetMethodInfo();
        }

        static Type DelegateTypeFor(MethodInfo methodInfo)
        {
            var type = BaseDelegateTypeFor(methodInfo);
            
            return type.MakeGenericType(GenericArgumentsFor(methodInfo).ToArray());
        }

        static Type BaseDelegateTypeFor(MethodInfo methodInfo)
        {
            var parametersCount = methodInfo.GetParameters().Length;
            if (methodInfo.ReturnType == typeof(void))
            {
                switch (parametersCount)
                {
                    case  0: return typeof(Action);
                    case  1: return typeof(Action<>);
                    case  2: return typeof(Action<,>);
                    case  3: return typeof(Action<,,>);
                    case  4: return typeof(Action<,,,>);
                    case  5: return typeof(Action<,,,,>);
                    case  6: return typeof(Action<,,,,,>);
                    case  7: return typeof(Action<,,,,,,>);
                    case  8: return typeof(Action<,,,,,,,>);
                    case  9: return typeof(Action<,,,,,,,,>);
                    case 10: return typeof(Action<,,,,,,,,,>);
                    case 11: return typeof(Action<,,,,,,,,,,>);
                    case 12: return typeof(Action<,,,,,,,,,,,>);
                    case 13: return typeof(Action<,,,,,,,,,,,,>);
                    case 14: return typeof(Action<,,,,,,,,,,,,,>);
                    case 15: return typeof(Action<,,,,,,,,,,,,,,>);
                    case 16: return typeof(Action<,,,,,,,,,,,,,,,>);
                }
            }
            else
            {
                switch (parametersCount)
                {
                    case  0: return typeof(Func<>);
                    case  1: return typeof(Func<,>);
                    case  2: return typeof(Func<,,>);
                    case  3: return typeof(Func<,,,>);
                    case  4: return typeof(Func<,,,,>);
                    case  5: return typeof(Func<,,,,,>);
                    case  6: return typeof(Func<,,,,,,>);
                    case  7: return typeof(Func<,,,,,,,>);
                    case  8: return typeof(Func<,,,,,,,,>);
                    case  9: return typeof(Func<,,,,,,,,,>);
                    case 10: return typeof(Func<,,,,,,,,,,>);
                    case 11: return typeof(Func<,,,,,,,,,,,>);
                    case 12: return typeof(Func<,,,,,,,,,,,,>);
                    case 13: return typeof(Func<,,,,,,,,,,,,,>);
                    case 14: return typeof(Func<,,,,,,,,,,,,,,>);
                    case 15: return typeof(Func<,,,,,,,,,,,,,,,>);
                    case 16: return typeof(Func<,,,,,,,,,,,,,,,,>);
                }
            }
            
            throw new NotSupportedException("Too many parameters");
        }

        static IEnumerable<Type> GenericArgumentsFor(MethodInfo methodInfo)
        {
            foreach (var parameterInfo in methodInfo.GetParameters())
                yield return parameterInfo.ParameterType;

            if (methodInfo.ReturnType != typeof(void))
                yield return methodInfo.ReturnType;
        }
    }
}