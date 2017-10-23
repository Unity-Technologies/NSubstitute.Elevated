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
        public void Class_Default_Constructor_Should_Not_Run()
        {
            var sub = Substitute.For<ClassWithDefaultCtor>();
            sub.ShouldBeOfType<ClassWithDefaultCtor>();

            sub.Value.ShouldBe(0);
        }

        [Test]
        public void Can_Mock_Class_With_No_Default_Ctor()
        {
            Substitute.For<ClassWithNoDefaultCtor>().ShouldBeOfType<ClassWithNoDefaultCtor>();
        }

        [Test]
        public void Can_Mock_Class_Containing_ICall_In_Ctor()
        {
            Substitute.For<ClassWithCtorICall>().ShouldBeOfType<ClassWithCtorICall>();
        }

        [Test]
        public void Can_Mock_Class_Containing_Throw_In_Ctor()
        {
            Substitute.For<ClassWithCtorThrow>().ShouldBeOfType<ClassWithCtorThrow>();
        }

        [Test]
        public void Mocking_Class_With_Constructor_Params_Should_Throw()
        {
            Should.Throw<SubstituteException>(() => Substitute.For<ClassWithNoDefaultCtor>(null));
            Should.Throw<SubstituteException>(() => Substitute.For<ClassWithNoDefaultCtor>("test"));
            Should.Throw<SubstituteException>(() => Substitute.For<ClassWithNoDefaultCtor>(null, null));
        }

        [Test]
        public void Class_With_No_Methods_Should_Not_Be_Patched()
        {
            // if it's a patched type, mocking will produce identical type (i.e. proxying installed directly in type).
            // if unpatched, then mocking will run standard nsubstitute behavior (i.e. proxying done via dynamicproxy generator, which inherits proxy type from the real type).

            Substitute.For<EmptyClass>().GetType().BaseType.ShouldBe(typeof(EmptyClass));
            Substitute.For<ClassWithNoDefaultCtorNoMethods>(null).GetType().BaseType.ShouldBe(typeof(ClassWithNoDefaultCtorNoMethods));
            Substitute.For<ClassWithNoDefaultCtorNoMethods>("test").GetType().BaseType.ShouldBe(typeof(ClassWithNoDefaultCtorNoMethods));
            Substitute.For<ClassWithNoDefaultCtorNoMethods>(null, null).GetType().BaseType.ShouldBe(typeof(ClassWithNoDefaultCtorNoMethods));
        }

        [Test]
        public void Non_Mocked_Class_With_Dependent_Types_Should_Load_Ok()
        {
            typeof(ClassWithDependency).GetMethod("Dummy").ReturnType.FullName.ShouldBe("mycodedep.DependentType");
        }

        [Test]
        public void Class_With_Dependent_Types_Should_Be_Patched()
        {
            // simple test to ensure that we can patch methods that use types from foreign assemblies

            Substitute.For<ClassWithDependency>().GetType().GetMethod("Dummy").ReturnType.FullName.ShouldBe("mycodedep.DependentType");
        }
    }
}
