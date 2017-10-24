using System;
using System.Linq;
using NSubstitute.Core;
using NSubstitute.Exceptions;
using NSubstitute.Proxies;
using NSubstitute.Proxies.CastleDynamicProxy;
using NSubstitute.Proxies.DelegateProxy;

namespace NSubstitute.Elevated
{
    class ElevatedProxyFactory : IProxyFactory
    {
        readonly ElevatedProxyMapper m_ElevatedProxyMapper;
        readonly IProxyFactory m_DefaultProxyFactory = new ProxyFactory(new DelegateProxyFactory(), new CastleDynamicProxyFactory());

        public ElevatedProxyFactory(ElevatedProxyMapper elevatedProxyMapper) => m_ElevatedProxyMapper = elevatedProxyMapper;

        object IProxyFactory.GenerateProxy(ICallRouter callRouter, Type typeToProxy, Type[] additionalInterfaces, object[] constructorArguments)
        {
            if (!ShouldHandle(typeToProxy))
                return m_DefaultProxyFactory.GenerateProxy(callRouter, typeToProxy, additionalInterfaces, constructorArguments);

            if (typeToProxy == typeof(SubstituteStatic.Proxy))
            {
                if (additionalInterfaces != null && additionalInterfaces.Any())
                    throw new SubstituteException("Can not substitute interfaces as static");

                var actualType = (Type)constructorArguments[0];

                return m_ElevatedProxyMapper.MockStatic(actualType, callRouter);
            }

            throw NotImplementedException();

            return null;
        }

        static bool ShouldHandle(Type typeToProxy)
        {
            if (typeToProxy.IsInterface || typeToProxy.IsAbstract)
                return false;

            // TEMP
            if (typeToProxy.FullName != "SystemUnderTest.SimpleClass")
                return false;

            return true;
        }
    }
}
