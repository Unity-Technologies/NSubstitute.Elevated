using System;
using System.Linq;
using Mono.Cecil.Rocks;
using NSubstitute.Elevated.Weaver;
using NSubstitute.Elevated.WeaverInternals;
using NUnit.Framework;
using Shouldly;

namespace NSubstitute.Elevated.Tests.Utilities
{
    [TestFixture]
    public class ElevatedWeaverTests
    {
        TestAssembly m_FixtureTestAssembly;

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
            m_FixtureTestAssembly = new TestAssembly(nameof(ElevatedWeaverTests), k_FixtureTestCode);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            m_FixtureTestAssembly?.Dispose();
        }

        [Test]
        public void Interfaces_ShouldNotPatch()
        {
            var type = m_FixtureTestAssembly.GetType("ShouldNotPatch.Interface");
            MockInjector.IsPatched(type).ShouldBeFalse();
        }

        [Test]
        public void PotentiallyBlittableStructs_ShouldNotPatch()
        {
            var type = m_FixtureTestAssembly.GetType("ShouldNotPatch.StructWithLayoutAttr");
            MockInjector.IsPatched(type).ShouldBeFalse();
        }

        [Test]
        public void PrivateNestedTypes_ShouldNotPatch()
        {
            var type = m_FixtureTestAssembly.GetType("ShouldNotPatch.ClassWithPrivateNestedType/PrivateNested");
            MockInjector.IsPatched(type).ShouldBeFalse();
        }

        [Test]
        public void GeneratedTypes_ShouldNotPatch()
        {
            var type = m_FixtureTestAssembly.GetType("ShouldNotPatch.ClassWithGeneratedNestedType");
            type.NestedTypes.Count.ShouldBe(1); // this is the yield state machine, will be mangled name
            MockInjector.IsPatched(type.NestedTypes[0]).ShouldBeFalse();
        }

        [Test]
        public void TopLevelClass_ShouldPatch()
        {
            var type = m_FixtureTestAssembly.GetType("ShouldPatch.ClassWithNestedTypes");
            MockInjector.IsPatched(type).ShouldBeTrue();
        }

        [Test]
        public void PublicNestedClasses_ShouldPatch()
        {
            var type = m_FixtureTestAssembly.GetType("ShouldPatch.ClassWithNestedTypes/PublicNested");
            MockInjector.IsPatched(type).ShouldBeTrue();
        }

        [Test]
        public void InternalNestedClasses_ShouldPatch()
        {
            var type = m_FixtureTestAssembly.GetType("ShouldPatch.ClassWithNestedTypes/InternalNested");
            MockInjector.IsPatched(type).ShouldBeTrue();
        }

        [Test]
        public void Injection_IsConsistentForAllTypes()
        {
            // whatever the reasons are for a given type getting patched or not, we want it to be internally consistent
            foreach (var type in m_FixtureTestAssembly.SelectTypes(IncludeNested.Yes))
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
