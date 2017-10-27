using System;
using System.Linq;
using NSubstitute.Elevated.Weaver;
using NUnit.Framework;
using Shouldly;

namespace NSubstitute.Elevated.Tests
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

                [StructLayout(LayoutKind.Explicit)]
                struct StructWithLayoutAttr { }             // don't want to risk breaking things by changing size

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
            var type = m_FixtureTestAssembly.GetType("ShouldNotPatch.ClassWithPrivateNestedType");
            var nestedType = type.NestedTypes.Single(t => t.Name == "PrivateNested");
            MockInjector.IsPatched(nestedType).ShouldBeFalse();
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
            var type = m_FixtureTestAssembly.GetType("ShouldPatch.ClassWithNestedTypes");
            var nestedType = type.NestedTypes.Single(t => t.Name == "PublicNested");
            MockInjector.IsPatched(nestedType).ShouldBeTrue();
        }

        [Test]
        public void InternalNestedClasses_ShouldPatch()
        {
            var type = m_FixtureTestAssembly.GetType("ShouldPatch.ClassWithNestedTypes");
            var nestedType = type.NestedTypes.Single(t => t.Name == "InternalNested");
            MockInjector.IsPatched(nestedType).ShouldBeTrue();
        }
    }
}
