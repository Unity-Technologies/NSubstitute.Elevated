using System;
using System.Collections.Generic;
using System.Reflection;
using NSubstitute.Core;
using NSubstitute.Elevated.Utilities;
using NSubstitute.Exceptions;

namespace NSubstitute.Elevated
{
    class ElevatedProxyMapper
    {
        readonly ISubstitutionContext m_SubstitutionContext;
        readonly CallFactory m_CallFactory;

        public ElevatedProxyMapper(ISubstitutionContext substitutionContext)
        {
            m_SubstitutionContext = substitutionContext;
            m_CallFactory = new CallFactory(m_SubstitutionContext);
        }

        public SubstituteStatic.Proxy MockStatic(Type type, ICallRouter callRouter)
        {
            var staticField = GetStaticFieldInfo(type);
            if (staticField == null)
                throw new SubstituteException("Can not substitute for non-patched types");
            if (staticField.GetValue(null) != null)
                throw new SubstituteException("Can not substitute the same type twice (did you forget to Dispose() your previous substitute?)");

            staticField.SetValue(null, callRouter);

            return new SubstituteStatic.Proxy(new DelegateDisposable(() =>
                {
                    var found = staticField.GetValue(null);
                    if (found == null)
                        throw new SubstituteException("Unexpected static unmock of an already unmocked type");
                    if (found != callRouter)
                        throw new SubstituteException("Discovered unexpected call router attached in static mock context");

                    staticField.SetValue(null, null);
                }));
        }

        public void Mock(Type type, object instance, ICallRouter callRouter)
        {
            var field = GetFieldInfo(type);
            if (field == null)
                throw new SubstituteException("Can not substitute for non-patched types");

            field.SetValue(instance, callRouter);
        }

        public bool TryMock(Type actualType, object instance, Type mockedReturnType, out object mockedReturnValue, MethodInfo method, Type[] methodGenericTypes, object[] args)
        {
            var field = instance == null ? GetStaticFieldInfo(actualType) : GetFieldInfo(actualType);
            var callRouter = (ICallRouter)field?.GetValue(instance);

            if (callRouter != null)
            {
                Func<object> baseResult = () => invocation.Proceed(); // $$$ need to turn this into a func reentry which goes straight to the leftover
                var result = new Lazy<object>(baseResult);
                Func<object> baseMethod = () => result.Value;

                var mappedInvocation = m_CallFactory.Create(method, args, instance, baseMethod);
                Array.Copy(mappedInvocation.GetArguments(), args, args.Length); // $$$ unsure about this..apparently need to copy back results, but our version has a bug on this
                mockedReturnValue = callRouter.Route(mappedInvocation);
                return true;
            }


            mockedReturnValue = mockedReturnType.GetDefaultValue();
            return false;
        }

        FieldInfo GetStaticFieldInfo(Type type) => m_StaticFieldCache.GetOrAdd(type, t => t.GetField("__mockStaticRouter", BindingFlags.Static | BindingFlags.NonPublic));
        FieldInfo GetFieldInfo(Type type) => m_FieldCache.GetOrAdd(type, t => t.GetField("__mockRouter", BindingFlags.Instance | BindingFlags.NonPublic));

        readonly Dictionary<Type, FieldInfo> m_StaticFieldCache = new Dictionary<Type, FieldInfo>();
        readonly Dictionary<Type, FieldInfo> m_FieldCache = new Dictionary<Type, FieldInfo>();
    }
}
