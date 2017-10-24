using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using NSubstitute.Core;
using NSubstitute.Core.Arguments;
using NSubstitute.Elevated.Utilities;
using NSubstitute.Exceptions;
using NSubstitute.Routing;

namespace NSubstitute.Elevated
{
    // this class exists solely to hook in our own proxy factory to the nsub machinery
    public class ElevatedSubstitutionContext : ISubstitutionContext
    {
        readonly ISubstitutionContext m_Forwarder;
        readonly ISubstituteFactory m_ElevatedSubstituteFactory;

        public ElevatedSubstitutionContext([NotNull] ISubstitutionContext forwarder)
        {
            m_Forwarder = forwarder;
            m_ElevatedSubstituteFactory = new SubstituteFactory(this,
                    new CallRouterFactory(), new ElevatedProxyFactory(ElevatedProxyMapper), new CallRouterResolver());
        }

        public static IDisposable AutoHook()
        {
            var hookedContext = SubstitutionContext.Current;
            var thisContext = new ElevatedSubstitutionContext(hookedContext);
            SubstitutionContext.Current = thisContext;

            return new DelegateDisposable(() =>
                {
                    if (SubstitutionContext.Current != thisContext)
                        throw new SubstituteException("Unexpected hook in place of ours");
                    SubstitutionContext.Current = hookedContext;
                });
        }

        internal ElevatedProxyMapper ElevatedProxyMapper { get; } = new ElevatedProxyMapper();

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
