using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using NSubstitute.Elevated.Weaver;
using NSubstitute.Elevated.WeaverInternals;
using NUnit.Framework;
using Shouldly;

namespace NSubstitute.Elevated.Tests.Utilities
{
    public class DependentAssemblyTests : PatchingFixture
    {
        [Test]
        public void PatchingDependentAssembly_WhenAlreadyLoaded_ShouldThrow()
        {/*
            var dependentDllName = GetType().Name + "_dependent";
            var dependentAssemblyPath = Compile(BaseDir, dependentDllName, "public class ReferencedType { }" );
            var usingAssemblyPath = Compile(BaseDir, GetType().Name + "_using", "public class UsingType : ReferencedType { }", dependentDllName);

            var dependentAssembly = PatchAndValidateAllDependentAssemblies(dependentAssemblyPath);
            var type = GetType(dependentAssembly, "ReferencedType");
            MockInjector.IsPatched(type).ShouldBeFalse();*/
        }
    }

    // TODO: tests to add
    //
    // sample: CombinatorialAttribute : CombiningStrategyAttribute << this crashes
    //    class Base { Base(int) { } }
    //    class Derived : Base { Derived() : base(1) { } }
    //
    // class with `public delegate void Blah();`
    //
    // patching a system (signed) assembly

    public class ElevatedWeaverTests : PatchingFixture
    {
        AssemblyDefinition m_TestAssembly;

        const string k_FixtureTestCode = @"

            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;

            namespace ShouldNotPatch
            {
                interface Interface { void Foo(); }         // ordinary proxying works

                [StructLayout(LayoutKind.Explicit, Size=4)] // size is necessary to make peverify happy when using StructLayout
                struct StructWithLayoutAttr { }             // don't patch these because adding a field may ruin serialization or blitting or something

                class ClassWithPrivateNestedType
                {
                    class PrivateNested { }                 // unavailable externally, no point
                }

                class ClassWithGeneratedNestedType
                {
                    public IEnumerable<int> Foo()           // this causes a state machine type to be generated which shouldn't be patched
                        { yield return 1; }
                }
            }

            namespace ShouldPatch
            {
                class ClassWithNestedTypes
                {
                    public class PublicNested { }
                    internal class InternalNested { }
                }
            }
            ";

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var testAssemblyPath = Compile(GetType().Name, k_FixtureTestCode);

            var results = ElevatedWeaver.PatchAllDependentAssemblies(testAssemblyPath, PatchOptions.PatchTestAssembly);
            results.Count.ShouldBe(2);
            results.ShouldContain(new PatchResult("mscorlib", null, PatchState.IgnoredForeignAssembly));
            results.ShouldContain(new PatchResult(testAssemblyPath, ElevatedWeaver.GetPatchBackupPathFor(testAssemblyPath), PatchState.Patched));

            m_TestAssembly = AssemblyDefinition.ReadAssembly(testAssemblyPath);
            MockInjector.IsPatched(m_TestAssembly).ShouldBeTrue();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            m_TestAssembly?.Dispose();
        }

        [Test]
        public void Interfaces_ShouldNotPatch()
        {
            var type = GetType(m_TestAssembly, "ShouldNotPatch.Interface");
            MockInjector.IsPatched(type).ShouldBeFalse();
        }

        [Test]
        public void PotentiallyBlittableStructs_ShouldNotPatch()
        {
            var type = GetType(m_TestAssembly, "ShouldNotPatch.StructWithLayoutAttr");
            MockInjector.IsPatched(type).ShouldBeFalse();
        }

        [Test]
        public void PrivateNestedTypes_ShouldNotPatch()
        {
            var type = GetType(m_TestAssembly, "ShouldNotPatch.ClassWithPrivateNestedType/PrivateNested");
            MockInjector.IsPatched(type).ShouldBeFalse();
        }

        [Test]
        public void GeneratedTypes_ShouldNotPatch()
        {
            var type = GetType(m_TestAssembly, "ShouldNotPatch.ClassWithGeneratedNestedType");
            type.NestedTypes.Count.ShouldBe(1); // this is the yield state machine, will be mangled name
            MockInjector.IsPatched(type.NestedTypes[0]).ShouldBeFalse();
        }

        [Test]
        public void TopLevelClass_ShouldPatch()
        {
            var type = GetType(m_TestAssembly, "ShouldPatch.ClassWithNestedTypes");
            MockInjector.IsPatched(type).ShouldBeTrue();
        }

        [Test]
        public void PublicNestedClasses_ShouldPatch()
        {
            var type = GetType(m_TestAssembly, "ShouldPatch.ClassWithNestedTypes/PublicNested");
            MockInjector.IsPatched(type).ShouldBeTrue();
        }

        [Test]
        public void InternalNestedClasses_ShouldPatch()
        {
            var type = GetType(m_TestAssembly, "ShouldPatch.ClassWithNestedTypes/InternalNested");
            MockInjector.IsPatched(type).ShouldBeTrue();
        }

        [Test]
        public void Injection_IsConsistentForAllTypes()
        {
            // whatever the reasons are for a given type getting patched or not, we want it to be internally consistent
            foreach (var type in SelectTypes(m_TestAssembly, IncludeNested.Yes))
            {
                var mockStaticField = type.Fields.SingleOrDefault(f => f.Name == MockInjector.InjectedMockStaticDataName);
                var mockField = type.Fields.SingleOrDefault(f => f.Name == MockInjector.InjectedMockDataName);
                var mockCtor = type.GetConstructors().SingleOrDefault(c => c.Parameters.Count == 1 && c.Parameters[0].ParameterType.FullName == typeof(MockPlaceholderType).FullName);

                var count = (mockStaticField != null ? 1 : 0) + (mockField != null ? 1 : 0) + (mockCtor != null ? 1 : 0);
                count.ShouldBeOneOf(0, 3);
            }
        }
    }
}
