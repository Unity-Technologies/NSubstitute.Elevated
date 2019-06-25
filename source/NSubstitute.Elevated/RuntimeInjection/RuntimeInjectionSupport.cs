using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using NSubstitute.Core;
using NSubstitute.Core.Arguments;
using NSubstitute.Exceptions;
using NSubstitute.Routing;
using Unity.Core;

namespace NSubstitute.Elevated.RuntimeInjection
{
    public class RuntimeInjectionSupport
    {
        internal class Context : ISubstitutionContext
        {
            readonly ISubstitutionContext m_Forwarder;
            readonly ISubstituteFactory m_ElevatedSubstituteFactory;

            // ReSharper disable once MemberCanBePrivate.Global
            Context(ISubstitutionContext forwarder)
            {
                m_Forwarder = forwarder;
                SubstituteManager = new SubstituteManager(this);
                m_ElevatedSubstituteFactory = new SubstituteFactory(this,
                    new ElevatedCallRouterFactory(), SubstituteManager, new CallRouterResolver());
            }

            internal static IDisposable AutoHook()
            {
                var hookedContext = SubstitutionContext.Current;
                var thisContext = new Context(hookedContext);

                SubstitutionContext.Current = thisContext;

                // TODO: return a new IDisposable class that also contains the list of patch results, then in caller verify that against expected (don't want to go too wide)

                return new DelegateDisposable(() =>
                {
                    if (SubstitutionContext.Current != thisContext)
                        throw new SubstituteException("Unexpected hook in place of ours");
                    SubstitutionContext.Current = hookedContext;
                });
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

        // returns true if a mock is in place and it is taking over functionality. instance may be null
        // if static. mockedReturnValue is ignored in a void return func.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TryMock(Type actualType, object instance, Type mockedReturnType, out object mockedReturnValue, Type[] methodGenericTypes, object[] args)
        {
            if (!(SubstitutionContext.Current is Context context))
            {
                mockedReturnValue = mockedReturnType.GetDefaultValue();
                return false;
            }

            var method = (MethodInfo) new StackTrace(1).GetFrame(0).GetMethod();

            if (method.IsGenericMethodDefinition)
                method = method.MakeGenericMethod(methodGenericTypes);

            return context.TryMock(actualType, instance, mockedReturnType, out mockedReturnValue, method, methodGenericTypes, args);
        }

        static MethodInfo GetOrCreateProxyFor(MethodInfo methodInfo)
        {
            // TODO: cache the proxy based on the provided information.
            // TODO: make sure names are unique.
            
            var method = new DynamicMethod(
                $"{methodInfo.Name}_Proxy_{methodInfo.GetParameters().Length}",
                methodInfo.Attributes,
                methodInfo.CallingConvention,
                methodInfo.ReturnType,
                methodInfo.GetParameters().Select(p => p.ParameterType).ToArray(),
                methodInfo.Module,
                true);
            
            var tryMockMethod = typeof(RuntimeInjectionSupport).GetMethod("TryMock");
            var getTypeFromHandle = typeof(Type).GetMethod("GetTypeFromHandle");
            
            var il = method.GetILGenerator();

            // object returnValue;
            il.DeclareLocal(typeof(object));
            
            // var a = typeof(StaticClass);
            il.Emit(OpCodes.Ldtoken, methodInfo.DeclaringType);
            il.Emit(OpCodes.Call, getTypeFromHandle);
            // var b = null;
            il.Emit(OpCodes.Ldnull);
            // var c = typeof(int);
            il.Emit(OpCodes.Ldtoken, typeof(int));
            il.Emit(OpCodes.Call, getTypeFromHandle);
            // out returnValue
            il.Emit(OpCodes.Ldloca_S, 0);
            // var d = new Type[0];
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Newarr, typeof(Type));
            // var e = new[] { (object) i };
            il.Emit(OpCodes.Ldc_I4_1); // size
            il.Emit(OpCodes.Newarr, typeof(object));
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Ldc_I4_0); // index
            il.Emit(OpCodes.Ldarg_0); // arg-index
            il.Emit(OpCodes.Box, typeof(int)); // convert type to System.Object
            il.Emit(OpCodes.Stelem_Ref);
            // ElevatedMockingSupport.TryMock(a, b, c, out returnValue, d, e);
            il.Emit(OpCodes.Call, tryMockMethod);
            il.Emit(OpCodes.Pop);
            //return (int)returnValue;
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Unbox_Any, methodInfo.ReturnType); // TODO: convert
            il.Emit(OpCodes.Ret);

            foreach (var parameterInfo in methodInfo.GetParameters())
            {
                method.DefineParameter(parameterInfo.Position, parameterInfo.Attributes, parameterInfo.Name);
            }

            var @delegate = method.CreateDelegate(
                typeof(Func<,>).MakeGenericType(
                    methodInfo.GetParameters()[0].ParameterType,
                    methodInfo.ReturnType));
            
            return @delegate.GetMethodInfo();
        }

        public static void InstallProxy(MethodInfo staticMethod)
        {
            var origin = GetAddressOfMethod(staticMethod);
            var proxy = GetOrCreateProxyFor(staticMethod);
            var dest = GetAddressOfMethod(proxy);

            MemoryUtilities.WriteJump(origin, dest);
        }

        public static void RemoveProxy()
        {
            
        }

        static long GetAddressOfMethod(MethodInfo methodInfo)
        {
            var handle = methodInfo.MethodHandle;
            RuntimeHelpers.PrepareMethod(handle);
            
            return handle.GetFunctionPointer().ToInt64();
        }

        public static IDisposable AutoHook()
        {
            return Context.AutoHook();
        }
    }
}
