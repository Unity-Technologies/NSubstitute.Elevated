using System;
using System.Collections.Generic;
using SystemUnderTest;
using NSubstitute.Elevated.WeaverInternals;
using NSubstitute.Exceptions;
using NUnit.Framework;
using Shouldly;
using Unity.Core;

namespace NSubstitute.Elevated.Tests
{
    [TestFixture]
    class BasicTests
    {
        IDisposable m_Dispose;

        [OneTimeSetUp]
        public void Setup()
        {
            m_Dispose = ElevatedSubstitutionContext.AutoHook(typeof(BasicTests).Assembly.Location, new [] {"SystemUnderTest"});
//            PatchedAssemblyBridgeX.TryMock = PatchedAssemblyBridge.TryMock;
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            m_Dispose.Dispose();
        }

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
        public void ClassWithNoDefaultCtor_TypeDoesNotChange()
        {
            var sub = Substitute.For<ClassWithNoDefaultCtor>();

            sub.ShouldBeOfType<ClassWithNoDefaultCtor>();
        }

#       if TEST_ICALLS
        [Test]
        public void ClassWithICallInCtor_TypeDoesNotChange()
        {
            // $ TODO: make this into an actual test of the icall thing. currently just checks that doesn't throw..not that interesting

            var sub = Substitute.For<ClassWithCtorICall>();

            sub.ShouldBeOfType<ClassWithCtorICall>();
        }

#       endif

        [Test]
        public void ClassWithThrowInCtor_TypeDoesNotChange()
        {
            var sub = Substitute.For<ClassWithCtorThrow>();

            sub.ShouldBeOfType<ClassWithCtorThrow>();
        }

        [Test]
        public void ClassWithCtorParams_WhenMocked_ShouldThrow()
        {
            Should.Throw<MissingMethodException>(() => Substitute.For<ClassWithNoDefaultCtor>());
            Should.Throw<SubstituteException>(() => Substitute.For<ClassWithNoDefaultCtor>("test"));
            Should.Throw<SubstituteException>(() => Substitute.For<ClassWithNoDefaultCtor>(null, null));
        }

        [Test]
        public void ClassWithNoMethods_ShouldBeIgnoredByWeaver()
        {
            // if it's a patched type, mocking will produce identical type (i.e. proxying installed directly in type).
            // if unpatched, then mocking will run standard nsubstitute behavior (i.e. proxying done via dynamicproxy generator, which inherits proxy type from the real type).

            var subEmpty = Substitute.For<EmptyClass>();
            subEmpty.GetType().BaseType.ShouldBe(typeof(EmptyClass));

            var subNoCtor1 = Substitute.For<ClassWithNoDefaultCtorNoMethods>(null);
            subNoCtor1.GetType().BaseType.ShouldBe(typeof(ClassWithNoDefaultCtorNoMethods));

            var subNoCtor2 = Substitute.For<ClassWithNoDefaultCtorNoMethods>("test");
            subNoCtor2.GetType().BaseType.ShouldBe(typeof(ClassWithNoDefaultCtorNoMethods));

            var subNoCtor3 = Substitute.For<ClassWithNoDefaultCtorNoMethods>(null, null);
            subNoCtor3.GetType().BaseType.ShouldBe(typeof(ClassWithNoDefaultCtorNoMethods));
        }

        [Test]
        public void NonMockedClassWithDependentTypes_Loads()
        {
            // ReSharper disable once PossibleNullReferenceException
            typeof(ClassWithDependency).GetMethod("Dummy").ReturnType.FullName.ShouldBe("DependentAssembly.DependentType");
        }

        [Test]
        public void ClassWithDependentTypes_CanUseDependentAssemblies()
        {
            // simple test to ensure that we can patch methods that use types from foreign assemblies

            var sub = Substitute.For<ClassWithDependency>();

            // ReSharper disable once PossibleNullReferenceException
            sub.GetType().GetMethod("Dummy").ReturnType.FullName.ShouldBe("mycodedep.DependentType");

            // $$$ TODO: test that the type is itself patched (look for __mockthingy)
        }
//
//        [Test]
//        public void SimpleClass_FullMock_DoesNotCallDefaultImpls()
//        {
//            var sub = Substitute.For<SimpleClass>();
//
//            sub.VoidMethod(5);
//            sub.Modified.ShouldBe(0);
//
//            sub.ReturnMethod(5).ShouldBe(0);
//            sub.Modified.ShouldBe(0);
//
//            sub.ReturnMethod(5).Returns(10);
//            sub.ReturnMethod(5).ShouldBe(10);
//            sub.Modified.ShouldBe(0);
//        }
//
//        [Test]
//        public void SimpleClass_PartialMock_CallsDefaultImpls()
//        {
//            var sub = Substitute.ForPartsOf<SimpleClass>();
//
//            sub.VoidMethod(5);
//            sub.Modified.ShouldBe(5);
//
//            sub.ReturnMethod(3).ShouldBe(8);
//            sub.Modified.ShouldBe(8);
//
//            sub.ReturnMethod(Arg.Is(4)).Returns(10);
//            sub.ReturnMethod(4).ShouldBe(10);
//            sub.Modified.ShouldBe(8);
//        }
    }
}
