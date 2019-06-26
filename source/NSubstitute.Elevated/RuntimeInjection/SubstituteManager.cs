using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using NSubstitute.Core;
using NSubstitute.Elevated.WeaverInternals;
using NSubstitute.Exceptions;
using NSubstitute.Proxies;
using NSubstitute.Proxies.CastleDynamicProxy;
using NSubstitute.Proxies.DelegateProxy;
using Unity.Core;

namespace NSubstitute.Elevated.RuntimeInjection
{
    class SubstituteManager : IProxyFactory
    {
        readonly CallFactory m_CallFactory;
        readonly IProxyFactory m_DefaultProxyFactory = new ProxyFactory(new DelegateProxyFactory(), new CastleDynamicProxyFactory());
        readonly object[] k_MockedCtorParams = { new MockPlaceholderType() };

        public SubstituteManager(ISubstitutionContext substitutionContext)
        {
            m_CallFactory = new CallFactory(substitutionContext);
        }

        object IProxyFactory.GenerateProxy(ICallRouter callRouter, Type typeToProxy, Type[] additionalInterfaces, object[] constructorArguments)
        {
            // TODO:
            //  * new type MockCtorPlaceholder in elevated assy
            //  * generate new empty ctor that takes MockCtorPlaceholder in all mocked types
            //  * support ctor params. throw if foudn and not ForPartsOf. then ForPartsOf determines which ctor we use.
            //  * have a note about static ctors. because they are special, and do not support disposal, can't really mock them right.
            //    best for user to do mock/unmock of static ctors manually (i.e. move into StaticInit/StaticDispose and call directly from test code)

            object proxy;
            var substituteConfig = RuntimeInjectionSupport.Context.TryGetSubstituteConfig(callRouter);

            if (typeToProxy.IsInterface || substituteConfig == null)
            {
                //proxy = m_DefaultProxyFactory.GenerateProxy(callRouter, typeToProxy, additionalInterfaces, constructorArguments);
                throw new NotImplementedException();
            }
            else if (typeToProxy == typeof(SubstituteStatic.Proxy))
            {
                if (additionalInterfaces?.Any() == true)
                    throw new SubstituteException("Cannot substitute interfaces as static");
                if (constructorArguments.Length != 1)
                    throw new SubstituteException("Unexpected use of SubstituteStatic.For");

                // the type we want comes from SubstituteStatic.For as a single ctor arg
                var actualType = (Type)constructorArguments[0];

                proxy = CreateStaticProxy(actualType, callRouter, substituteConfig == SubstituteConfig.CallBaseByDefault);
            }
            else
            {
                /*
                // requests for additional interfaces on patched types cannot be done at runtime. elevated mocking can't,
                // by definition, go through a runtime dynamic proxy generator that could add such things.
                if (additionalInterfaces.Any())
                    throw new SubstituteException("Cannot add interfaces at runtime to patched types");

                switch (substituteConfig) {
                    case SubstituteConfig.OverrideAllCalls:

                        // overriding all calls includes the ctor, so it makes no sense for the user to pass in ctor args
                        if (constructorArguments != null && constructorArguments.Any())
                            throw new SubstituteException("Do not pass ctor args when substituting with elevated mocks (or did you mean to use ForPartsOf?)");

                        // but we use a ctor arg to select the special empty ctor that we patched in
                        constructorArguments = k_MockedCtorParams;
                        break;
                    case SubstituteConfig.CallBaseByDefault:
                        var castleDynamicProxyFactory = new CastleDynamicProxyFactory();
                        return castleDynamicProxyFactory.GenerateProxy(callRouter, typeToProxy, additionalInterfaces, constructorArguments);
                    case null:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

//                var proxyWrap = Activator.CreateInstanceFrom(patchAllDependentAssemblies[1].Path, typeToProxy.FullName, false,
//                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance, null,
//                    constructorArguments, null, null);
//                proxy = proxyWrap.Unwrap();

                proxy = Activator.CreateInstance(typeToProxy, constructorArguments);
                GetRouterField(proxy.GetType()).SetValue(proxy, callRouter);
                */
                throw new NotImplementedException();
            }

            return proxy;
        }

        // returns true if a mock is in place and it is taking over functionality. instance may be null
        // if static. mockedReturnValue is ignored in a void return func.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TryMockWrapper(Type actualType, object instance, Type mockedReturnType, out object mockedReturnValue, Type[] methodGenericTypes, MethodBase originalMethod, object[] args)
        {
            if (!(SubstitutionContext.Current is RuntimeInjectionSupport.Context context))
            {
                mockedReturnValue = mockedReturnType.GetDefaultValue();
                return false;
            }

            return context.TryMock(actualType, instance, mockedReturnType, out mockedReturnValue, originalMethod, methodGenericTypes, args);
        }


        object CreateStaticProxy(Type typeToProxy, ICallRouter callRouter, bool callBaseByDefault)
        {
            var trampolines = new List<IDisposable>();

            foreach (var originalMethod in typeToProxy.GetMethods())
            {
                if (CanMock(originalMethod))
                {
                    var tryMockProxyGenerator = ((RuntimeInjectionSupport.Context)SubstitutionContext.Current).TryMockProxyGenerator;
                    tryMockProxyGenerator.GenerateProxiesFor(originalMethod, callBaseByDefault);
                    
                    trampolines.Add(
                        RuntimeInjectionSupport.InstallDynamicMethodTrampoline(
                            originalMethod,
                            tryMockProxyGenerator.GetTryMockProxydDelegateFor(originalMethod).GetMethodInfo()));
                }
                else
                    Console.WriteLine($"Method {originalMethod.DeclaringType.FullName}::{originalMethod.Name} is not being mocked");
            }
            
            var cache = ((RuntimeInjectionSupport.Context)SubstitutionContext.Current).CallRouterCache;
            cache.AddCallRouterForStatic(typeToProxy, callRouter);

            return new SubstituteStatic.Proxy(new DelegateDisposable(() =>
            {
                cache.RemoveCallRouterForStatic(typeToProxy);
                
                foreach (var trampoline in trampolines)
                {
                    trampoline.Dispose();
                }
                trampolines.Clear();
            }));
        }

        static bool CanMock(MethodInfo methodInfo)
        {
            if (methodInfo.GetGenericArguments().Length > 0)
                return false;

            if (!methodInfo.IsStatic)
                return false;
            
            return true;
        }

        // called from patched assembly code via the PatchedAssemblyBridge. return true if the mock is handling the behavior.
        // false means that the original implementation should run.
        public bool TryMock(Type actualType, object instance, Type mockedReturnType, out object mockedReturnValue, MethodInfo method, Type[] methodGenericTypes, object[] args)
        {
            ICallRouter callRouter;
            if (instance == null)
            {
                // This is a static method. We store the call router in a global cache, where the key is the type.
                var cache = ((RuntimeInjectionSupport.Context)SubstitutionContext.Current).CallRouterCache;
                callRouter = cache.CallRouterForStatic(actualType);
            }
            else
            {
                var field = GetRouterField(actualType);
                callRouter = (ICallRouter)field?.GetValue(instance);
            }

            if (callRouter != null)
            {
                var shouldCallOriginalMethod = false;
                var call = m_CallFactory.Create(method, args, instance, () => shouldCallOriginalMethod = true);
                mockedReturnValue = callRouter.Route(call);

                return !shouldCallOriginalMethod;
            }

            mockedReturnValue = mockedReturnType.GetDefaultValue();
            return false;
        }

        // motivation for router mapping being stored with the type/instance:
        //
        //   1. avoid problem of "gc leak vs. substitute requires disposal" by storing the router link in the instance
        //   2. support for struct instances (only possible to associate call routers with individual structs from the inside)
        //   3. is a simple way to check that a type has been patched
        //
        //FieldInfo GetStaticRouterField(Type type) => m_RouterStaticFieldCache.GetOrAdd(type, t => GetRouterField(t, Weaver.MockInjector.InjectedMockStaticDataName, BindingFlags.Static));
        FieldInfo GetRouterField(Type type) => m_RouterFieldCache.GetOrAdd(type, t => GetRouterField(t, Weaver.MockInjector.InjectedMockDataName, BindingFlags.Instance));

        static FieldInfo GetRouterField(IReflect type, string fieldName, BindingFlags bindingFlags)
        {
            var field = type.GetField(fieldName, bindingFlags | BindingFlags.NonPublic);
            if (field == null)
                throw new SubstituteException("Cannot substitute for non-patched types");

            if (field.FieldType != typeof(object))
                throw new SubstituteException("Unexpected mock data type found on patched type");

            return field;
        }

        readonly Dictionary<Type, FieldInfo> m_RouterStaticFieldCache = new Dictionary<Type, FieldInfo>();
        readonly Dictionary<Type, FieldInfo> m_RouterFieldCache = new Dictionary<Type, FieldInfo>();
    }
}