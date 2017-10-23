using System;
using SystemUnderTest;
using NSubstitute;
using NSubstitute.Exceptions;
using NUnit.Framework;
using Shouldly;

namespace Unity.NSubstitute.Tests
{
    [TestFixture]
    class BasicTests
    {
        [Test]
        public void MockByInterface_ShouldUseNSubDefaultBehavior()
        {
            var sub = Substitute.For<IBasicInterface>();
            sub.GetType().FullName.ShouldBe("Castle.Proxies.IBasicInterfaceProxy");

            sub.GetValue().ShouldBe(0);
            sub.GetValue().Returns(5);
            sub.GetValue().ShouldBe(5);
        }

        [Test]
        public void MockByVirtualClass_ShouldUseNSubDefaultBehavior()
        {
            var sub = Substitute.ForPartsOf<ClassWithVirtuals>();
            sub.GetType().FullName.ShouldBe("Castle.Proxies.ClassWithVirtualsProxy");

            sub.GetValue().ShouldBe(4);
            sub.GetValue().Returns(6);
            sub.GetValue().ShouldBe(6);
        }

        [Test]
        public void ClassWithDefaultCtor_MockedMethod_ShouldNotRun()
        {
            var sub = Substitute.For<ClassWithDefaultCtor>();
            sub.ShouldBeOfType<ClassWithDefaultCtor>();

            sub.Value.ShouldBe(0);
        }

        [Test]
        public void ClassWithNoDefaultCtor_MocksWithoutError()
        {
            Substitute.For<ClassWithNoDefaultCtor>().ShouldBeOfType<ClassWithNoDefaultCtor>();
        }

#       if TEST_ICALLS
        [Test]
        public void ClassWithICallInCtor_MocksWithoutError()
        {
            // $ TODO: make this into an actual test of the icall thing. currently just checks that doesn't throw..not that interesting

            Substitute.For<ClassWithCtorICall>().ShouldBeOfType<ClassWithCtorICall>();
        }

#       endif

        [Test]
        public void ClassWithThrowInCtor_MocksWithoutError()
        {
            Substitute.For<ClassWithCtorThrow>().ShouldBeOfType<ClassWithCtorThrow>();
        }

        [Test]
        public void ClassWithCtorParams_WhenMocked_ShouldThrow()
        {
            Should.Throw<SubstituteException>(() => Substitute.For<ClassWithNoDefaultCtor>(null));
            Should.Throw<SubstituteException>(() => Substitute.For<ClassWithNoDefaultCtor>("test"));
            Should.Throw<SubstituteException>(() => Substitute.For<ClassWithNoDefaultCtor>(null, null));
        }

        [Test]
        public void ClassWithNoMethods_ShouldBeIgnoredByWeaver()
        {
            // if it's a patched type, mocking will produce identical type (i.e. proxying installed directly in type).
            // if unpatched, then mocking will run standard nsubstitute behavior (i.e. proxying done via dynamicproxy generator, which inherits proxy type from the real type).

            Substitute.For<EmptyClass>().GetType().BaseType.ShouldBe(typeof(EmptyClass));
            Substitute.For<ClassWithNoDefaultCtorNoMethods>(null).GetType().BaseType.ShouldBe(typeof(ClassWithNoDefaultCtorNoMethods));
            Substitute.For<ClassWithNoDefaultCtorNoMethods>("test").GetType().BaseType.ShouldBe(typeof(ClassWithNoDefaultCtorNoMethods));
            Substitute.For<ClassWithNoDefaultCtorNoMethods>(null, null).GetType().BaseType.ShouldBe(typeof(ClassWithNoDefaultCtorNoMethods));
        }

        [Test]
        public void NonMockedClassWithDependentTypes_LoadsWithoutError()
        {
            // ReSharper disable once PossibleNullReferenceException
            typeof(ClassWithDependency).GetMethod("Dummy").ReturnType.FullName.ShouldBe("mycodedep.DependentType");
        }

        [Test]
        public void ClassWithDependentTypes_MocksWithoutError()
        {
            // simple test to ensure that we can patch methods that use types from foreign assemblies

            // ReSharper disable once PossibleNullReferenceException
            Substitute.For<ClassWithDependency>().GetType().GetMethod("Dummy").ReturnType.FullName.ShouldBe("mycodedep.DependentType");
        }
    }
}
