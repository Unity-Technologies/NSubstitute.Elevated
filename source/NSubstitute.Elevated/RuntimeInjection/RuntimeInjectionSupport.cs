using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using NSubstitute.Core;
using NSubstitute.Core.Arguments;
using NSubstitute.Exceptions;
using NSubstitute.Routing;
using Unity.Core;

namespace NSubstitute.Elevated.RuntimeInjection
{
    class RuntimeInjectionSupport
    {
        internal class Context : ISubstitutionContext
        {
            readonly ISubstitutionContext m_Forwarder;
            readonly ISubstituteFactory m_ElevatedSubstituteFactory;

            public TryMockProxyGenerator TryMockProxyGenerator { get; } = new TryMockProxyGenerator();

            // ReSharper disable once MemberCanBePrivate.Global
            internal Context(ISubstitutionContext forwarder)
            {
                m_Forwarder = forwarder;
                SubstituteManager = new SubstituteManager(this);
                m_ElevatedSubstituteFactory = new SubstituteFactory(this,
                    new ElevatedCallRouterFactory(), SubstituteManager, new CallRouterResolver());
            }

            SubstituteManager SubstituteManager { get; }

            class ElevatedCallRouterFactory : ICallRouterFactory
            {
                public ICallRouter Create(ISubstitutionContext substitutionContext, SubstituteConfig config)
                    => new InjectedCallRouter(new SubstituteState(substitutionContext, config), substitutionContext, new RouteFactory());
            }

            class InjectedCallRouter : CallRouter
            {
                public InjectedCallRouter(ISubstituteState substituteState, ISubstitutionContext context, IRouteFactory routeFactory)
                    : base(substituteState, context, routeFactory) => SubstituteConfig = substituteState.SubstituteConfig;

                public SubstituteConfig SubstituteConfig { get; }
            }

            internal bool TryMock(Type actualType, object instance, Type mockedReturnType, out object mockedReturnValue, MethodInfo method, Type[] methodGenericTypes, object[] args)
            {
                return SubstituteManager.TryMock(actualType, instance, mockedReturnType, out mockedReturnValue, method, methodGenericTypes, args);
            }

            internal static SubstituteConfig? TryGetSubstituteConfig(ICallRouter callRouter)
                => (callRouter as InjectedCallRouter)?.SubstituteConfig;

            // this is the only one we're overriding for now, so we can hook our own factory in there.
            ISubstituteFactory ISubstitutionContext.SubstituteFactory => m_ElevatedSubstituteFactory;

            SequenceNumberGenerator ISubstitutionContext.SequenceNumberGenerator => m_Forwarder.SequenceNumberGenerator;

            PendingSpecificationInfo ISubstitutionContext.PendingSpecificationInfo
            {
                get => m_Forwarder.PendingSpecificationInfo;
                set => m_Forwarder.PendingSpecificationInfo = value;
            }

            bool ISubstitutionContext.IsQuerying => m_Forwarder.IsQuerying;

            void ISubstitutionContext.AddToQuery(object target, ICallSpecification callSpecification)
            {
                m_Forwarder.AddToQuery(target, callSpecification);
            }

            void ISubstitutionContext.ClearLastCallRouter()
            {
                m_Forwarder.ClearLastCallRouter();
            }

            IList<IArgumentSpecification> ISubstitutionContext.DequeueAllArgumentSpecifications()
            {
                return m_Forwarder.DequeueAllArgumentSpecifications();
            }

            void ISubstitutionContext.EnqueueArgumentSpecification(IArgumentSpecification spec)
            {
                m_Forwarder.EnqueueArgumentSpecification(spec);
            }

            ICallRouter ISubstitutionContext.GetCallRouterFor(object substitute)
            {
                return m_Forwarder.GetCallRouterFor(substitute);
            }

            IRouteFactory ISubstitutionContext.GetRouteFactory()
            {
                return m_Forwarder.GetRouteFactory();
            }

            void ISubstitutionContext.LastCallRouter(ICallRouter callRouter)
            {
                m_Forwarder.LastCallRouter(callRouter);
            }

            ConfiguredCall ISubstitutionContext.LastCallShouldReturn(IReturn value, MatchArgs matchArgs)
            {
                return m_Forwarder.LastCallShouldReturn(value, matchArgs);
            }

            void ISubstitutionContext.RaiseEventForNextCall(Func<ICall, object[]> getArguments)
            {
                m_Forwarder.RaiseEventForNextCall(getArguments);
            }

            IQueryResults ISubstitutionContext.RunQuery(Action calls)
            {
                return m_Forwarder.RunQuery(calls);
            }
        }

        struct DynamicMethodTrampolineDisposer : IDisposable
        {
            long m_OriginOffset;
            byte[] m_OriginalBytes;

            public DynamicMethodTrampolineDisposer(long originOffset, byte[] originalBytes)
            {
                m_OriginOffset = originOffset;
                m_OriginalBytes = originalBytes;
            }

            public void Dispose()
            {
                MemoryUtilities.WriteBytes(m_OriginOffset, m_OriginalBytes);
            }
        }

        public static unsafe IDisposable InstallDynamicMethodTrampoline(MethodInfo staticMethod, MethodInfo dynamicMethod)
        {
            var origin = GetAddressOfMethod(staticMethod);
            var dest = GetAddressOfMethod(dynamicMethod);

            var memory = origin;
            var originOffset = memory;
            byte[] originalBytes;
            
            MemoryUtilities.UnprotectMemoryPage(memory);
            
            if (IntPtr.Size == sizeof(long))
            {
                if (MemoryUtilities.CompareBytes(memory, new byte[] { 0xe9 }))
                {
                    var offset = MemoryUtilities.ReadInt(memory + 1);
                    memory += 5 + offset;
                }

                originOffset = memory;
                originalBytes = new byte[12];
                
                var p = (byte*)memory;
                for(var i = 0; i < originalBytes.Length; ++i, ++p)
                    originalBytes[i] = *p;

                memory = MemoryUtilities.WriteBytes(memory, new byte[] { 0x48, 0xB8 });
                memory = MemoryUtilities.WriteLong(memory, dest);
                memory = MemoryUtilities.WriteBytes(memory, new byte[] { 0xFF, 0xE0 });
            }
            else
            {
                originalBytes = new byte[8];
                
                var p = (byte*)memory;
                for(var i = 0; i < originalBytes.Length; ++i, ++p)
                    originalBytes[i] = *p;
                
                memory = MemoryUtilities.WriteByte(memory, 0x68);
                memory = MemoryUtilities.WriteInt(memory, (int)dest);
                memory = MemoryUtilities.WriteByte(memory, 0xc3);
            }

            MemoryUtilities.FlushInstructionCache(originOffset);

            MemoryUtilities.ProtectMemoryPage(originOffset);

            return new DynamicMethodTrampolineDisposer(originOffset, originalBytes);
        }

        static long GetAddressOfMethod(MethodInfo methodInfo)
        {
            var handle = methodInfo.MethodHandle;
            RuntimeHelpers.PrepareMethod(handle);
            
            return handle.GetFunctionPointer().ToInt64();
        }

        public static IDisposable AutoHook()
        {
            var hookedContext = SubstitutionContext.Current;
            var thisContext = new Context(hookedContext);

            SubstitutionContext.Current = thisContext;

            // TODO: return a new IDisposable class that also contains the list of patch results, then in caller verify that against expected (don't want to go too wide)

            return new DelegateDisposable(() =>
            {
                if (SubstitutionContext.Current != thisContext)
                    throw new SubstituteException("Unexpected hook in place of ours");
                
                thisContext.TryMockProxyGenerator.Purge();
                SubstitutionContext.Current = hookedContext;
            });
        }
    }
}
