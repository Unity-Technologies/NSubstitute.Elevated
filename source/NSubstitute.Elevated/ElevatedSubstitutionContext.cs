using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using NiceIO;
using NSubstitute.Core;
using NSubstitute.Core.Arguments;
using NSubstitute.Elevated.Weaver;
using NSubstitute.Exceptions;
using NSubstitute.Routing;
using Unity.Core;

namespace NSubstitute.Elevated
{
    // motivation:
    //
    //   1. it's the clean way to hook in our own proxy factory to the nsub machinery
    //   2. provide access to the sub manager so patched assemblies can route hooked calls through nsub (the so-called 'elevated' mock part)
    //
    public class ElevatedSubstitutionContext : ISubstitutionContext
    {
        readonly ISubstitutionContext m_Forwarder;
        readonly ISubstituteFactory m_ElevatedSubstituteFactory;

        // ReSharper disable once MemberCanBePrivate.Global
        public ElevatedSubstitutionContext([NotNull] ISubstitutionContext forwarder)
        {
            m_Forwarder = forwarder;
            ElevatedSubstituteManager = new ElevatedSubstituteManager(this);
            m_ElevatedSubstituteFactory = new SubstituteFactory(this,
                    new ElevatedCallRouterFactory(), ElevatedSubstituteManager, new CallRouterResolver());
        }

        public static IDisposable AutoHook(string assemblyLocation)
        {
            var hookedContext = SubstitutionContext.Current;
            var thisContext = new ElevatedSubstitutionContext(hookedContext);
            SubstitutionContext.Current = thisContext;

            // TODO: return a new IDisposable class that also contains the list of patch results, then in caller verify that against expected (don't want to go too wide)

            var patchAllDependentAssemblies = ElevatedWeaver.PatchAllDependentAssemblies(
                new NPath(assemblyLocation), PatchOptions.PatchTestAssembly).ToList();

            return new DelegateDisposable(() =>
                {
                    if (SubstitutionContext.Current != thisContext)
                        throw new SubstituteException("Unexpected hook in place of ours");
                    SubstitutionContext.Current = hookedContext;
                });
        }

        internal ElevatedSubstituteManager ElevatedSubstituteManager { get; }

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
}
