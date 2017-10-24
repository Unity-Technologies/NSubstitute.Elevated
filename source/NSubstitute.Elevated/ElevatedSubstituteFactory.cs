using System;
using NSubstitute.Core;

namespace NSubstitute.Elevated
{
    class ElevatedSubstituteFactory : ISubstituteFactory
    {
        readonly ISubstituteFactory m_Forwarder;

        public ElevatedSubstituteFactory(ISubstituteFactory forwarder)
        => m_Forwarder = forwarder;

        object ISubstituteFactory.Create(Type[] typesToProxy, object[] constructorArguments)
        => m_Forwarder.Create(typesToProxy, constructorArguments);

        object ISubstituteFactory.CreatePartial(Type[] typesToProxy, object[] constructorArguments)
        => m_Forwarder.CreatePartial(typesToProxy, constructorArguments);

        ICallRouter ISubstituteFactory.GetCallRouterCreatedFor(object substitute)
        => m_Forwarder.GetCallRouterCreatedFor(substitute);
    }
}
