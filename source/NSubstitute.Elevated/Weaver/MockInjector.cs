using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using NSubstitute.Elevated.WeaverInternals;
using Unity.Core;
using Assembly = System.Reflection.Assembly;
using AssemblyMetadataAttribute = System.Reflection.AssemblyMetadataAttribute;

namespace NSubstitute.Elevated.Weaver
{
    class MockInjector
    {
        static readonly string k_MarkAsPatchedKey, k_MarkAsPatchedValue;

        readonly TypeDefinition m_MockPlaceholderType;
        readonly MethodDefinition m_PatchedAssemblyBridgeTryMock;

        public const string InjectedMockStaticDataName = "__mock__staticData", InjectedMockDataName = "__mock__data";

        static MockInjector()
        {
            k_MarkAsPatchedKey = Assembly.GetExecutingAssembly().GetName().Name;

            var assemblyHash = Assembly.GetExecutingAssembly().Evidence.GetHostEvidence<Hash>();
            if (assemblyHash == null)
                throw new Exception("Assembly not stamped with a hash");

            k_MarkAsPatchedValue = assemblyHash.SHA1.ToHexString();
        }

        public MockInjector(AssemblyDefinition nsubElevatedAssembly)
        {
            m_MockPlaceholderType = nsubElevatedAssembly.MainModule
                .GetType(typeof(MockPlaceholderType).FullName);

            m_PatchedAssemblyBridgeTryMock = nsubElevatedAssembly.MainModule
                .GetType(typeof(PatchedAssemblyBridge).FullName)
                .Methods.Single(m => m.Name == nameof(PatchedAssemblyBridge.TryMock));
        }

        public void Patch(AssemblyDefinition assembly)
        {
            // patch all types

            var typesToProcess = assembly
                .SelectTypes(IncludeNested.Yes)
                .OrderBy(t => t.InheritanceChainLength())   // process base classes first
                .ToList();                                  // copy to a list in case patch work we do would invalidate the enumerator

            foreach (var type in typesToProcess)
                Patch(type);

            // add an attr to mark the assembly as patched

            var mainModule = assembly.MainModule;
            var types = mainModule.TypeSystem;

            var metadataAttrName = typeof(AssemblyMetadataAttribute);
            var metadataAttrType = new TypeReference(metadataAttrName.Namespace, metadataAttrName.Name, mainModule, types.CoreLibrary);
            var metadataAttrCtor = new MethodReference(".ctor", types.Void, metadataAttrType) { HasThis = true };
            metadataAttrCtor.Parameters.Add(new ParameterDefinition(types.String));
            metadataAttrCtor.Parameters.Add(new ParameterDefinition(types.String));

            var metadataAttr = new CustomAttribute(metadataAttrCtor);
            metadataAttr.ConstructorArguments.Add(new CustomAttributeArgument(types.String, k_MarkAsPatchedKey));
            metadataAttr.ConstructorArguments.Add(new CustomAttributeArgument(types.String, k_MarkAsPatchedValue));

            assembly.CustomAttributes.Add(metadataAttr);
        }

        public static bool IsPatched(AssemblyDefinition assembly)
        {
            return assembly.CustomAttributes.Any(a =>
                a.AttributeType.FullName == typeof(AssemblyMetadataAttribute).FullName &&
                a.ConstructorArguments.Count == 2 &&
                a.ConstructorArguments[0].Value as string == k_MarkAsPatchedKey &&
                a.ConstructorArguments[1].Value as string == k_MarkAsPatchedValue);
        }

        public static bool IsPatched(string assemblyPath)
        {
            using (var assembly = AssemblyDefinition.ReadAssembly(assemblyPath))
                return IsPatched(assembly);
        }

        void Patch(TypeDefinition type)
        {
            if (type.IsInterface)
                return;
            if (type.IsNestedPrivate)
                return;
            if (type.Name == "<Module>")
                return;
            if (type.IsExplicitLayout)
                return;
            if (type.CustomAttributes.Any(a => a.AttributeType.FullName == typeof(CompilerGeneratedAttribute).FullName))
                return;

            try
            {
                foreach (var method in type.Methods)
                    Patch(method);

                void AddField(string fieldName, FieldAttributes fieldAttributes)
                {
                    type.Fields.Add(new FieldDefinition(fieldName,
                            FieldAttributes.Private | FieldAttributes.NotSerialized | fieldAttributes,
                            type.Module.TypeSystem.Object));
                }

                AddField(InjectedMockStaticDataName, FieldAttributes.Static);
                AddField(InjectedMockDataName, 0);

                AddMockCtor(type);
            }
            catch (Exception e)
            {
                throw new Exception($"Internal error during mock injection into type {type.FullName}", e);
            }
        }

        public static bool IsPatched(TypeDefinition type)
        {
            var mockStaticField = type.Fields.SingleOrDefault(f => f.Name == InjectedMockStaticDataName);
            var mockField = type.Fields.SingleOrDefault(f => f.Name == InjectedMockDataName);
            if ((mockStaticField != null) != (mockField != null))
                throw new Exception("Unexpected mismatch between static and instance mock injected fields");

            return mockStaticField != null;
        }

        void AddMockCtor(TypeDefinition type)
        {
            var ctor = new MethodDefinition(".ctor",
                    MethodAttributes.Public | MethodAttributes.RTSpecialName | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                    type.Module.TypeSystem.Void)
            {
                IsManaged = true,
                DeclaringType = type,
                HasThis = true,
            };
            ctor.Parameters.Add(new ParameterDefinition(type.Module.ImportReference(m_MockPlaceholderType)));

            var body = ctor.Body;
            body.Instructions.Clear();

            var il = body.GetILProcessor();

            var baseCtors = type.BaseType.Resolve().GetConstructors();

            var baseMockCtor = (MethodReference)baseCtors.SingleOrDefault(c => c.Parameters.SequenceEqual(ctor.Parameters));
            if (baseMockCtor != null)
            {
                baseMockCtor = type.BaseType.IsGenericInstance
                    ? new MethodReference(baseMockCtor.Name, baseMockCtor.ReturnType, type.BaseType) { HasThis = baseMockCtor.HasThis }
                    : type.Module.ImportReference(baseMockCtor);

                il.Append(il.Create(OpCodes.Ldarg_0));
                il.Append(il.Create(OpCodes.Ldarg_1));
                il.Append(il.Create(OpCodes.Call, baseMockCtor));
            }
            else
            {
                var baseCtor = type.Module.ImportReference(baseCtors.Single(c => !c.Parameters.Any()));

                il.Append(il.Create(OpCodes.Ldarg_0));
                il.Append(il.Create(OpCodes.Call, baseCtor));
            }

            il.Append(il.Create(OpCodes.Ret));

            type.Methods.Add(ctor);
        }
        void Patch(MethodDefinition method)
        {
            if (method.IsCompilerControlled || method.IsConstructor || method.IsAbstract)
                return;

            // $$$ DOWIT
        }
    }
}
