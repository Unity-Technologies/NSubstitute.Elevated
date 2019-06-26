using System;
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
                // requests for additional interfaces on patched types cannot be done at runtime. elevated mocking can't,
                // by definition, go through a runtime dynamic proxy generator that could add such things.
                if (additionalInterfaces.Any())
                    throw new SubstituteException("Cannot add interfaces at runtime to patched types");
                
                var context = ((RuntimeInjectionSupport.Context)SubstitutionContext.Current);
                
                foreach (var originalMethod in typeToProxy.GetMethods())
                {
                    if (CanMock(originalMethod))
                    {
                        var tryMockProxyGenerator = context.TryMockProxyGenerator;
                        tryMockProxyGenerator.GenerateProxiesFor(originalMethod, substituteConfig == SubstituteConfig.CallBaseByDefault);
                    
                        context.AddTrampoline(
                            RuntimeInjectionSupport.InstallDynamicMethodTrampoline(
                                originalMethod,
                                tryMockProxyGenerator.GetTryMockProxydDelegateFor(originalMethod).GetMethodInfo()));
                    }
                    else
                        Console.WriteLine($"Method {originalMethod.DeclaringType.FullName}::{originalMethod.Name} is not being mocked");
                }

                proxy = Activator.CreateInstance(typeToProxy, constructorArguments);
                
                var cache = context.CallRouterCache;
                cache.AddCallRouterForInstance(proxy, callRouter);
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
            var context = ((RuntimeInjectionSupport.Context)SubstitutionContext.Current);
            foreach (var originalMethod in typeToProxy.GetMethods())
            {
                if (CanMock(originalMethod))
                {
                    var tryMockProxyGenerator = context.TryMockProxyGenerator;
                    tryMockProxyGenerator.GenerateProxiesFor(originalMethod, callBaseByDefault);
                    
                    context.AddTrampoline(
                        RuntimeInjectionSupport.InstallDynamicMethodTrampoline(
                            originalMethod,
                            tryMockProxyGenerator.GetTryMockProxydDelegateFor(originalMethod).GetMethodInfo()));
                }
                else
                    Console.WriteLine($"Method {originalMethod.DeclaringType.FullName}::{originalMethod.Name} is not being mocked");
            }
            
            var cache = context.CallRouterCache;
            cache.AddCallRouterForStatic(typeToProxy, callRouter);

            return new SubstituteStatic.Proxy(new DelegateDisposable(() =>
            {
                cache.RemoveCallRouterForStatic(typeToProxy);
                
                context.UnInstallTrampolines();
            }));
        }

        static bool CanMock(MethodInfo methodInfo)
        {
            if (methodInfo.IsVirtual)
                return false;

            // TODO implement icalls
            if (methodInfo.GetMethodBody() == null)
                return false;
            
            if (methodInfo.GetGenericArguments().Length > 0)
                return false;

            // TODO implement out args
            if (methodInfo.GetParameters().Any(p => p.IsOut))
                return false;

            // TODO constructor support
            if (methodInfo.IsConstructor)
                return false;
            
            return true;
        }

        // called from patched assembly code via the PatchedAssemblyBridge. return true if the mock is handling the behavior.
        // false means that the original implementation should run.
        public bool TryMock(Type actualType, object instance, Type mockedReturnType, out object mockedReturnValue, MethodInfo method, Type[] methodGenericTypes, object[] args)
        {
            ICallRouter callRouter;
            var cache = ((RuntimeInjectionSupport.Context)SubstitutionContext.Current).CallRouterCache;
            
            if (instance == null)
            {
                // This is a static method. We store the call router in a global cache, where the key is the type.
                callRouter = cache.CallRouterForStatic(actualType);
            }
            else
            {
                if(actualType.IsValueType)
                    throw new NotSupportedException("TryMock is not supported on value types.");
                else
                    callRouter = cache.CallRouterForInstance(instance);
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
    }
}