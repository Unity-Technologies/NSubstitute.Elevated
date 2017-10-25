using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NSubstitute.Core;
using NSubstitute.Elevated.Utilities;
using NSubstitute.Exceptions;
using NSubstitute.Proxies;
using NSubstitute.Proxies.CastleDynamicProxy;
using NSubstitute.Proxies.DelegateProxy;

namespace NSubstitute.Elevated
{
    class ElevatedSubstituteManager : IProxyFactory
    {
        readonly CallFactory m_CallFactory;
        readonly IProxyFactory m_DefaultProxyFactory = new ProxyFactory(new DelegateProxyFactory(), new CastleDynamicProxyFactory());

        public ElevatedSubstituteManager(ISubstitutionContext substitutionContext)
        {
            m_CallFactory = new CallFactory(substitutionContext);
        }

        object IProxyFactory.GenerateProxy(ICallRouter callRouter, Type typeToProxy, Type[] additionalInterfaces, object[] constructorArguments)
        {
            object proxy;

            var shouldForward = typeToProxy.IsInterface;

            // TEMP
            shouldForward |= typeToProxy.FullName != "SystemUnderTest.SimpleClass";

            if (shouldForward)
            {
                proxy = m_DefaultProxyFactory.GenerateProxy(callRouter, typeToProxy, additionalInterfaces, constructorArguments);
            }
            else if (typeToProxy == typeof(SubstituteStatic.Proxy))
            {
                if (additionalInterfaces != null && additionalInterfaces.Any())
                    throw new SubstituteException("Cannot substitute interfaces as static");
                if (constructorArguments.Length != 1)
                    throw new SubstituteException("Unexpected use of SubstituteStatic.For");

                // the type we want comes from SubstituteStatic.For as a single ctor arg
                var actualType = (Type)constructorArguments[0];

                proxy = CreateStaticProxy(actualType, callRouter);
            }
            else
            {
                // requests for additional interfaces on patched types cannot be done at runtime. elevated mocking can't,
                // by definition, go through a runtime dynamic proxy generator that could add such things.
                if (additionalInterfaces.Any())
                    throw new SubstituteException("Cannot add interfaces at runtime to patched types");

                // nsubstitute's dynamic proxy works on concrete classes by inheriting via a runtime-generated type, overriding
                // virtuals with interceptor behavior. because the base is unmodified, it needs ctor params to be passed in
                // for the proxy to pass to base. this in turn likely runs code, and we're not really working with an actual
                // mock.
                //
                // elevated mocking via assembly patching, by contrast, lets us a) insert a new default ctor where missing, and
                // b) bypass any existing default ctor code from executing. we end up with a true mock. therefore, it makes no
                // sense to ever pass in ctor args, so this case becomes an exception.
                if (constructorArguments.Any())
                    throw new SubstituteException("Do not pass ctor args when substituting with elevated mocks");

                proxy = CreateProxy(typeToProxy, callRouter);
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
                        throw new SubstituteException("Unexpected static unmock of an already unmocked type");
                    if (found != callRouter)
                        throw new SubstituteException("Discovered unexpected call router attached in static mock context");

                    field.SetValue(null, null);
                }));
        }

        object CreateProxy(Type typeToProxy, ICallRouter callRouter)
        {
            var field = GetRouterField(typeToProxy);

            var newInstance = Activator.CreateInstance(typeToProxy);
            field.SetValue(newInstance, callRouter);

            return newInstance;
        }

        // called from patched assembly code via the PatchedAssemblyBridge. return true if the mock is handling the behavior.
        // false means that the original implementation should run.
        public bool TryMock(Type actualType, object instance, Type mockedReturnType, out object mockedReturnValue, MethodInfo method, Type[] methodGenericTypes, object[] args)
        {
            var field = instance == null ? GetStaticRouterField(actualType) : GetRouterField(actualType);
            var callRouter = (ICallRouter)field?.GetValue(instance);

            if (callRouter != null)
            {
                object CallOriginalMethod()
                {
                    // $$$ need to turn this into a func reentry which goes straight to the leftover by setting a flag or something
                    throw new NotSupportedException();
                }

                var call = m_CallFactory.Create(method, args, instance, CallOriginalMethod);
                mockedReturnValue = callRouter.Route(call); // $$$ may need to copy back mappedInvocation.GetArguments() on top of `args`...unsure
                return true;
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
        FieldInfo GetStaticRouterField(Type type) => m_RouterStaticFieldCache.GetOrAdd(type, t => GetRouterField(t, "__mockStaticData", BindingFlags.Static));
        FieldInfo GetRouterField(Type type) => m_RouterFieldCache.GetOrAdd(type, t => GetRouterField(t, "__mockData", BindingFlags.Instance));

        static FieldInfo GetRouterField(Type type, string fieldName, BindingFlags bindingFlags)
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
