using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Mono.Reflection;

namespace NSubstitute.Elevated.RuntimeInjection {
    class TryMockProxyGenerator
    {
        struct Proxies
        {
            public Delegate TryMockProxy;
            public Delegate OriginalMethodProxy;
        }
        
        Dictionary<MethodInfo, Proxies> m_Cache = new Dictionary<MethodInfo, Proxies>(new MethodInfoComparer());

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

        public void PurgeAllFor(MethodInfo originalMethod)
        {
            m_Cache.Remove(originalMethod);
        }

        internal void GenerateProxiesFor(MethodInfo methodInfo, bool generateOriginalMethodProxy)
        {
            if(m_Cache.ContainsKey(methodInfo))
                return;
            // TODO: are we leaking this?
            m_Cache.Add(methodInfo, new Proxies
            {
                TryMockProxy = TryMockProxyFor(methodInfo),
                OriginalMethodProxy = generateOriginalMethodProxy ? OriginalMethodProxyFor(methodInfo) : null
            });
        }

        public Delegate GetTryMockProxydDelegateFor(MethodInfo methodInfo)
        {
            if (m_Cache.TryGetValue(methodInfo, out var @delegate))
                return @delegate.TryMockProxy;

            throw new InvalidOperationException();
        }

        public Delegate GetOriginalMethodDelegateFor(MethodInfo methodInfo)
        {
            if (m_Cache.TryGetValue(methodInfo, out var @delegate))
                return @delegate.OriginalMethodProxy;

            throw new InvalidOperationException();
        }
        
        static Delegate OriginalMethodProxyFor(MethodInfo methodInfo)
        {
            var dynamicMethod = OriginalProxyGenerator.GenerateProxy(methodInfo, $"{methodInfo.Name}_OriginalProxy_{methodInfo.GetHashCode()}");

            return dynamicMethod.CreateDelegate(DelegateTypeFor(methodInfo));
        }

        static Delegate TryMockProxyFor(MethodInfo methodInfo)
        {
            var parameterInfos = methodInfo.GetParameters();
            var dynamicMethod = new DynamicMethod(
                $"{methodInfo.Name}_Proxy_{methodInfo.GetHashCode()}",
                methodInfo.Attributes | MethodAttributes.Static,
                CallingConventions.HasThis,
                methodInfo.ReturnType,
                ParameterTypesFor(methodInfo).ToArray(),
                methodInfo.Module,
                true);

//            foreach (var parameterInfo in parameterInfos)
//            {
//                dynamicMethod.DefineParameter(parameterInfo.Position, parameterInfo.Attributes, parameterInfo.Name);
//            }

            var tryMockMethod = typeof(SubstituteManager).GetMethod(nameof(SubstituteManager.TryMockWrapper));
            var getTypeFromHandle = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle));
            var getMethodFromHandle = typeof(MethodBase).GetMethod(nameof(MethodBase.GetMethodFromHandle), new[]{typeof(RuntimeMethodHandle)});

            var generator = dynamicMethod.GetILGenerator();

            // object returnValue;
            generator.DeclareLocal(typeof(object));

            // var a = typeof(methodInfo.DeclaringType);
            generator.Emit(OpCodes.Ldtoken, methodInfo.DeclaringType);
            generator.Emit(OpCodes.Call, getTypeFromHandle);

            // var b = null || instance;
            if (methodInfo.IsStatic)
                generator.Emit(OpCodes.Ldnull);
            else
                generator.Emit(OpCodes.Ldarg_0);

            // var c = typeof(methodInfo.ReturnType);
            generator.Emit(OpCodes.Ldtoken, methodInfo.ReturnType);
            generator.Emit(OpCodes.Call, getTypeFromHandle);

            // out returnValue
            generator.Emit(OpCodes.Ldloca_S, 0);

            // Generic Arguments
            // var d = new Type[0];
            generator.Emit(OpCodes.Ldc_I4_0);
            generator.Emit(OpCodes.Newarr, typeof(Type));
            
            // methodInfo
            generator.Emit(OpCodes.Ldtoken, methodInfo);
            generator.Emit(OpCodes.Call, getMethodFromHandle);

            // Arguments
            // args = new[parameterInfos.Length];
            generator.Emit(OpCodes.Ldc_I4, parameterInfos.Length); // TODO: this will fail with > then Int.MaxValue parameters ...
            generator.Emit(OpCodes.Newarr, typeof(object));

            var index = 0;
            var argIndex = 0;
            if (!methodInfo.IsStatic) argIndex += 1;

            foreach (var parameterInfo in parameterInfos)
            {
                // args[index] = (object) arg;
                generator.Emit(OpCodes.Dup);
                generator.Emit(OpCodes.Ldc_I4, index); // index
                generator.Emit(OpCodes.Ldarg, argIndex); // arg-index
                if (parameterInfo.ParameterType.IsValueType)
                    generator.Emit(OpCodes.Box, parameterInfo.ParameterType);
                generator.Emit(OpCodes.Stelem_Ref);
                ++index;
                ++argIndex;
            }

            // ElevatedMockingSupport.TryMock(a, b, c, out returnValue, d, methodInfo, args);
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

            return dynamicMethod.CreateDelegate(DelegateTypeFor(methodInfo));
        }

        static IEnumerable<Type> ParameterTypesFor(MethodInfo methodInfo)
        {
            if (!methodInfo.IsStatic)
                yield return methodInfo.DeclaringType;

            foreach (var parameterInfo in methodInfo.GetParameters())
                yield return parameterInfo.ParameterType;
        }

        static Type DelegateTypeFor(MethodInfo methodInfo)
        {
            var type = BaseDelegateTypeFor(methodInfo);

            return type.MakeGenericType(GenericArgumentsFor(methodInfo).ToArray());
        }

        static Type BaseDelegateTypeFor(MethodInfo methodInfo)
        {
            var parametersCount = methodInfo.GetParameters().Length;
            if (!methodInfo.IsStatic)
                parametersCount += 1;
            
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
            if (!methodInfo.IsStatic)
                yield return methodInfo.DeclaringType;
            
            foreach (var parameterInfo in methodInfo.GetParameters())
                yield return parameterInfo.ParameterType;

            if (methodInfo.ReturnType != typeof(void))
                yield return methodInfo.ReturnType;
        }
    }
}