using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using NSubstitute.Core;
using NSubstitute.Core.Arguments;
using NSubstitute.Elevated.WeaverInternals;
using NSubstitute.Exceptions;
using NSubstitute.Proxies;
using NSubstitute.Proxies.CastleDynamicProxy;
using NSubstitute.Proxies.DelegateProxy;
using NSubstitute.Routing;
using NUnit.Framework;
using Shouldly;
using Unity.Core;

namespace NSubstitute.Elevated.Tests
{
    class InjectSubstituteManager : IProxyFactory
    {
        readonly CallFactory m_CallFactory;
        readonly IProxyFactory m_DefaultProxyFactory = new ProxyFactory(new DelegateProxyFactory(), new CastleDynamicProxyFactory());
        readonly object[] k_MockedCtorParams = { new MockPlaceholderType() };

        public InjectSubstituteManager(ISubstitutionContext substitutionContext)
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
            var substituteConfig = InjectionContext.TryGetSubstituteConfig(callRouter);

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

                proxy = CreateStaticProxy(actualType, callRouter);
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

        object CreateStaticProxy(Type typeToProxy, ICallRouter callRouter)
        {
            var field = GetStaticRouterField(typeToProxy);
            if (field.GetValue(null) != null)
                throw new SubstituteException("Cannot substitute the same type twice (did you forget to Dispose() your previous substitute?)");

            field.SetValue(null, callRouter);

            return new SubstituteStatic.Proxy(new DelegateDisposable(() =>
                {
                    var found = field.GetValue(null);
                    if (found == null)
                        throw new SubstituteException("Unexpected static unmock of an already-unmocked type");
                    if (found != callRouter)
                        throw new SubstituteException("Discovered unexpected call router attached in static mock context");

                    field.SetValue(null, null);
                }));
        }

        // called from patched assembly code via the PatchedAssemblyBridge. return true if the mock is handling the behavior.
        // false means that the original implementation should run.
        public bool TryMock(Type actualType, object instance, Type mockedReturnType, out object mockedReturnValue, MethodInfo method, Type[] methodGenericTypes, object[] args)
        {
            var field = instance == null ? GetStaticRouterField(actualType) : GetRouterField(actualType);
            var callRouter = (ICallRouter)field?.GetValue(instance);

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
        FieldInfo GetStaticRouterField(Type type) => m_RouterStaticFieldCache.GetOrAdd(type, t => GetRouterField(t, Weaver.MockInjector.InjectedMockStaticDataName, BindingFlags.Static));
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

    public class InjectionContext : ISubstitutionContext
    {
        readonly ISubstitutionContext m_Forwarder;
        readonly ISubstituteFactory m_ElevatedSubstituteFactory;

        // ReSharper disable once MemberCanBePrivate.Global
        public InjectionContext(ISubstitutionContext forwarder)
        {
            m_Forwarder = forwarder;
            InjectSubstituteManager = new InjectSubstituteManager(this);
            m_ElevatedSubstituteFactory = new SubstituteFactory(this,
                    new ElevatedCallRouterFactory(), InjectSubstituteManager, new CallRouterResolver());
        }

        public static IDisposable AutoHook()
        {
            var hookedContext = SubstitutionContext.Current;
            var thisContext = new InjectionContext(hookedContext);
            SubstitutionContext.Current = thisContext;

            // TODO: return a new IDisposable class that also contains the list of patch results, then in caller verify that against expected (don't want to go too wide)

            return new DelegateDisposable(() =>
                {
                    if (SubstitutionContext.Current != thisContext)
                        throw new SubstituteException("Unexpected hook in place of ours");
                    SubstitutionContext.Current = hookedContext;
                });
        }

        internal InjectSubstituteManager InjectSubstituteManager { get; }

        class ElevatedCallRouterFactory : ICallRouterFactory
        {
            public ICallRouter Create(ISubstitutionContext substitutionContext, SubstituteConfig config)
            => new ElevatedCallRouter(new SubstituteState(substitutionContext, config), substitutionContext, new RouteFactory());
        }

        class ElevatedCallRouter : CallRouter
        {
            public ElevatedCallRouter(ISubstituteState substituteState, ISubstitutionContext context, IRouteFactory routeFactory)
                : base(substituteState, context, routeFactory) => SubstituteConfig = substituteState.SubstituteConfig;

            public SubstituteConfig SubstituteConfig { get; }
        }

        internal static SubstituteConfig? TryGetSubstituteConfig(ICallRouter callRouter)
        => (callRouter as ElevatedCallRouter)?.SubstituteConfig;

        // this is the only one we're overriding for now, so we can hook our own factory in there.
        ISubstituteFactory ISubstitutionContext.SubstituteFactory => m_ElevatedSubstituteFactory;

        SequenceNumberGenerator ISubstitutionContext.SequenceNumberGenerator => m_Forwarder.SequenceNumberGenerator;
        PendingSpecificationInfo ISubstitutionContext.PendingSpecificationInfo { get => m_Forwarder.PendingSpecificationInfo; set => m_Forwarder.PendingSpecificationInfo = value; }
        bool ISubstitutionContext.IsQuerying => m_Forwarder.IsQuerying;
        void ISubstitutionContext.AddToQuery(object target, ICallSpecification callSpecification) { m_Forwarder.AddToQuery(target, callSpecification); }
        void ISubstitutionContext.ClearLastCallRouter() { m_Forwarder.ClearLastCallRouter(); }
        IList<IArgumentSpecification> ISubstitutionContext.DequeueAllArgumentSpecifications() { return m_Forwarder.DequeueAllArgumentSpecifications(); }
        void ISubstitutionContext.EnqueueArgumentSpecification(IArgumentSpecification spec) { m_Forwarder.EnqueueArgumentSpecification(spec); }
        ICallRouter ISubstitutionContext.GetCallRouterFor(object substitute) { return m_Forwarder.GetCallRouterFor(substitute); }
        IRouteFactory ISubstitutionContext.GetRouteFactory() { return m_Forwarder.GetRouteFactory(); }
        void ISubstitutionContext.LastCallRouter(ICallRouter callRouter) { m_Forwarder.LastCallRouter(callRouter); }
        ConfiguredCall ISubstitutionContext.LastCallShouldReturn(IReturn value, MatchArgs matchArgs) { return m_Forwarder.LastCallShouldReturn(value, matchArgs); }
        void ISubstitutionContext.RaiseEventForNextCall(Func<ICall, object[]> getArguments) { m_Forwarder.RaiseEventForNextCall(getArguments); }
        IQueryResults ISubstitutionContext.RunQuery(Action calls) { return m_Forwarder.RunQuery(calls); }
    }
    
    public class TestInjection
    {
        IDisposable m_Dispose;

        [OneTimeSetUp]
        public void Setup()
        {
            m_Dispose = InjectionContext.AutoHook();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            m_Dispose.Dispose();
        }
        
        [Test]
        public void StaticMethodInjectionWorks()
        {
            using (SubstituteStatic.For<StaticClass>())
            {
                InstallProxy();
                StaticClass.ReturnArgument(1).Returns(10);
                StaticClass.ReturnArgument(4).Returns(14);
                
                Console.WriteLine(StaticClass.ReturnArgument(1));
                Console.WriteLine(StaticClass.ReturnArgument(4));
                
                StaticClass.ReturnArgument(1).ShouldBe(10);
                StaticClass.ReturnArgument(4).ShouldBe(14);
                RemoveProxy();
            }
        }

        static void InstallProxy()
        {
            var origin = GetMethodStat(typeof(StaticClass).GetMethod("ReturnArgument"));
            var dest = GetMethodStat(typeof(TestInjection).GetMethod("ReturnArgument_Proxy"));

            WriteJump(origin, dest);
        }

        static long GetMethodStat(MethodInfo methodInfo)
        {
            var handle = methodInfo.MethodHandle;
            RuntimeHelpers.PrepareMethod(handle);
            
            return handle.GetFunctionPointer().ToInt64();
        }
        
        internal static void UnprotectMemoryPage(long memory)
        {
            /*
            if (IsWindows)
            {
                var success = VirtualProtect(new IntPtr(memory), new UIntPtr(1), Protection.PAGE_EXECUTE_READWRITE, out _);
                if (success == false)
                    throw new System.ComponentModel.Win32Exception();
            }
            */
        }

        static void ProtectMemoryPage(long memory)
        {
            
        }
        
        public static void WriteJump(long memory, long destination)
        {
            UnprotectMemoryPage(memory);

            if (IntPtr.Size == sizeof(long))
            {
                if (CompareBytes(memory, new byte[] { 0xe9 }))
                {
                    var offset = ReadInt(memory + 1);
                    memory += 5 + offset;
                }

                memory = WriteBytes(memory, new byte[] { 0x48, 0xB8 });
                memory = WriteLong(memory, destination);
                _ = WriteBytes(memory, new byte[] { 0xFF, 0xE0 });
            }
            else
            {
                memory = WriteByte(memory, 0x68);
                memory = WriteInt(memory, (int)destination);
                _ = WriteByte(memory, 0xc3);
            }
            
            //FlushInstructionCache(memory, new UIntPtr(1));

            ProtectMemoryPage(memory);
        }

        internal static unsafe long WriteByte(long memory, byte value)
        {
            var p = (byte*)memory;
            *p = value;
            return memory + sizeof(byte);
        }
        
        internal static unsafe int ReadInt(long memory)
        {
            var p = (int*)memory;
            return *p;
        }
        
        internal static unsafe long WriteLong(long memory, long value)
        {
            var p = (long*)memory;
            *p = value;
            return memory + sizeof(long);
        }
        
        internal static long WriteBytes(long memory, byte[] values)
        {
            foreach (var value in values)
                memory = WriteByte(memory, value);
            return memory;
        }
        
        internal static unsafe long WriteInt(long memory, int value)
        {
            var p = (int*)memory;
            *p = value;
            return memory + sizeof(int);
        }
        
        internal static unsafe bool CompareBytes(long memory, byte[] values)
        {
            var p = (byte*)memory;
            foreach (var value in values)
            {
                if (value != *p) return false;
                p++;
            }
            return true;
        }

        static void RemoveProxy()
        {
            
        }
        
        
        public class StaticClass
        {
            static object __mock__staticData;
        
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static int ReturnArgument(int i)
            {
                //return ReturnArgument_Proxy(i);
                return i;
            }
        }

        public static int ReturnArgument_Proxy(int i)
        {
            object returnValue = 0;
            AdrianosMagic.TryMock(typeof(StaticClass), null, typeof(int), out returnValue, new Type[0], new[] { (object) i });

            return (int)returnValue;
        }


        public static class AdrianosMagic
        {
            // returns true if a mock is in place and it is taking over functionality. instance may be null
            // if static. mockedReturnValue is ignored in a void return func.
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static bool TryMock(Type actualType, object instance, Type mockedReturnType, out object mockedReturnValue, Type[] methodGenericTypes, object[] args)
            {
                if (!(SubstitutionContext.Current is InjectionContext elevated))
                {
                    mockedReturnValue = mockedReturnType.GetDefaultValue();
                    return false;
                }

                var method = (MethodInfo) new StackTrace(1).GetFrame(0).GetMethod();

                if (method.IsGenericMethodDefinition)
                    method = method.MakeGenericMethod(methodGenericTypes);

                return elevated.InjectSubstituteManager.TryMock(actualType, instance, mockedReturnType, out mockedReturnValue, method, methodGenericTypes, args);
            }
        }
    }
}
