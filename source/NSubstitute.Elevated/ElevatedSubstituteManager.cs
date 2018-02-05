using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using NSubstitute.Core;
using NSubstitute.Elevated.Weaver;
using NSubstitute.Elevated.WeaverInternals;
using NSubstitute.Exceptions;
using NSubstitute.Proxies;
using NSubstitute.Proxies.CastleDynamicProxy;
using NSubstitute.Proxies.DelegateProxy;
using Unity.Core;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace NSubstitute.Elevated
{
    class ElevatedSubstituteManager : IProxyFactory
    {
        readonly CallFactory m_CallFactory;
        readonly IProxyFactory m_DefaultProxyFactory = new ProxyFactory(new DelegateProxyFactory(), new CastleDynamicProxyFactory());
        readonly object[] k_MockedCtorParams = { new MockPlaceholderType() };

        public ElevatedSubstituteManager(ISubstitutionContext substitutionContext)
        {
            m_CallFactory = new CallFactory(substitutionContext);
        }

        void AddMockPlaceholderToAssembly(AssemblyDefinition targetAssembly)
        {
            var mockPlaceholder = new TypeDefinition("NSubstitute.Elevated.WeaverInternals", "MockPlaceholderType", TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit)
            {
                BaseType = targetAssembly.MainModule.TypeSystem.Object
            };
            targetAssembly.MainModule.Types.Add(mockPlaceholder);
        }

        // TODO: Fix
        const string peVerifyLocation = @"C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.1 Tools\x64\PEVerify.exe";
        static void Verify(string assemblyName)
        {
            var p = new Process
            {
                StartInfo =
                {
                    Arguments = $"/nologo \"{assemblyName}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    FileName = peVerifyLocation,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                }
            };

            var error = "";
            var output = "";

            p.OutputDataReceived += (_, e) => output += $"{e.Data}\n";
            p.ErrorDataReceived += (_, e) => error += $"{e.Data}\n";

            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            p.WaitForExit();

            Console.WriteLine(assemblyName);
            if (p.ExitCode != 0)
            {
                Console.Error.WriteLine($"{error}\n{output}");
                Environment.Exit(p.ExitCode);
            }

        }

        object IProxyFactory.GenerateProxy(ICallRouter callRouter, Type typeToProxy, Type[] additionalInterfaces, object[] constructorArguments)
        {
            // TODO:
            //  * new type MockCtorPlaceholder in elevated assy
            //  * generate new empty ctor that takes MockCtorPlaceholder in all mocked types
            //  * support ctor params. throw if foudn and not ForPartsOf. then ForPartsOf determines which ctor we use.
            //  * have a note about static ctors. because they are special, and do not support disposal, can't really mock them right.
            //    best for user to do mock/unmock of static ctors manually (i.e. move into StaticInit/StaticDispose and call directly from test code)

            var patchAllDependentAssemblies = ElevatedWeaver.PatchAllDependentAssemblies(Assembly.GetAssembly(typeToProxy).Location, PatchTestAssembly.Yes).ToList();
            Verify(patchAllDependentAssemblies[1].Path);

            object proxy;
            var substituteConfig = ElevatedSubstitutionContext.TryGetSubstituteConfig(callRouter);

            if (typeToProxy.IsInterface || substituteConfig == null)
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

                if (substituteConfig == SubstituteConfig.OverrideAllCalls)
                {
                    // overriding all calls includes the ctor, so it makes no sense for the user to pass in ctor args
                    if (constructorArguments.Any())
                        throw new SubstituteException("Do not pass ctor args when substituting with elevated mocks (or did you mean to use ForPartsOf?)");

                    // but we use a ctor arg to select the special empty ctor that we patched in
                    constructorArguments = k_MockedCtorParams;
                }

                proxy = Activator.CreateInstanceFrom(patchAllDependentAssemblies[1].Path, typeToProxy.FullName, false,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance, null,
                    constructorArguments, null, null);

                //proxy = Activator.CreateInstance(typeToProxy, constructorArguments);
                GetRouterField(typeToProxy).SetValue(proxy, callRouter);
            }

            return proxy;
        }

        static FieldDefinition CreateFakeFieldForward(TypeDefinition typeDefinition)
        {
            var field = new FieldDefinition(k_FakeForward, FieldAttributes.Assembly, typeDefinition); // TODO: GetName as param
            typeDefinition.Fields.Add(field);
            return field;
        }

        const string k_FakeForward = "__fake_forward";
        static FieldDefinition FakeForwardField(TypeDefinition typeDefinition)
        {
            return typeDefinition.Fields.Single(f => f.Name == k_FakeForward);
        }

        static void AddBaseTypeCtorCall(AssemblyDefinition target, TypeDefinition typeDefinition,
            MethodDefinition methodDefinition)
        {
            if (typeDefinition.BaseType != null && !typeDefinition.IsValueType)
            {
                methodDefinition.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                var baseTypeCtor =
                    target.MainModule.Import(
                        typeDefinition.BaseType.Resolve()
                            .Methods.Single(m => m.IsConstructor && m.Parameters.Count == 0));
                methodDefinition.Body.Instructions.Add(Instruction.Create(OpCodes.Call, baseTypeCtor));
            }
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
}
