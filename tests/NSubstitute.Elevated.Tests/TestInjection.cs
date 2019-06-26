using System;
using System.Runtime.CompilerServices;
using NSubstitute.Elevated.RuntimeInjection;
using NSubstitute.Elevated.WeaverInternals;
using NUnit.Framework;
using Shouldly;

namespace NSubstitute.Elevated.Tests
{
    class StaticClass
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int ReturnArgument(int i)
        {
            return i;
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int AddArguments(int a, int b, int c)
        {
            return a + b + c;
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int ReturnHalfArgument(int i)
        {
            return i/2;
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static object ReturnArgument(object i)
        {
            return i;
        }
    }
    
    public class TestInjection
    {
        IDisposable m_Dispose;

        [OneTimeSetUp]
        public void Setup()
        {
            m_Dispose = RuntimeInjectionSupport.AutoHook();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            m_Dispose.Dispose();
        }
        
        [Test]
        public void StaticMethodInjectionWorks()
        {
            using (SubstituteStatic.For<StaticClass>())
            {
                StaticClass.ReturnArgument(1).Returns(10);
                StaticClass.ReturnArgument(4).Returns(14);
                
                StaticClass.ReturnHalfArgument(2).Returns(4);

                StaticClass.ReturnArgument(1).ShouldBe(10);
                StaticClass.ReturnArgument(4).ShouldBe(14);
                
                StaticClass.ReturnHalfArgument(2).ShouldBe(4);
            }
            
            StaticClass.ReturnArgument(1).ShouldBe(1);
            StaticClass.ReturnArgument(4).ShouldBe(4);
            StaticClass.ReturnHalfArgument(2).ShouldBe(1);
        }
        
        [Test]
        public void StaticMethodInjectionWorksIfForPartsOfIsNotUsed()
        {
            using (SubstituteStatic.For<StaticClass>())
            {
                StaticClass.ReturnArgument(2).ShouldBe(0);
            }
        }
        
        [Test]
        public void StaticMethodInjectionWorksIfForPartsOfIsUsed()
        {
            using (SubstituteStatic.ForPartsOf<StaticClass>())
            {
                StaticClass.ReturnArgument(4).Returns(10);
                
                StaticClass.ReturnArgument(4).ShouldBe(10);
                StaticClass.ReturnArgument(2).ShouldBe(2);
            }
        }
        
        [Test]
        public void StaticMethodInjectionWorksWithReferenceTypeReturn()
        {
            using (SubstituteStatic.For<StaticClass>())
            {
                StaticClass.ReturnArgument("gabriele").Returns("cds");
                
                StaticClass.ReturnArgument("gabriele").ShouldBe("cds");
            }
                
            StaticClass.ReturnArgument("gabriele").ShouldBe("gabriele");
        }
        
        [Test]
        public void StaticMethodInjectionWorksWithMultipleArguments()
        {
            using (SubstituteStatic.For<StaticClass>())
            {
                StaticClass.AddArguments(1,2,3).Returns(12);
                
                StaticClass.AddArguments(1, 2, 3).ShouldBe(12);
            }
            
            StaticClass.AddArguments(1, 2, 3).ShouldBe(6);
        }
        
        [Test]
        [Ignore("This needs removing the need for the backing field.")]
        public void MscorlibStaticMethodWorks()
        {
            using (SubstituteStatic.For<DateTime>())
            {
                DateTime.Now.Returns(new DateTime(1983, 6, 29));
                
                DateTime.Now.ShouldBe(new DateTime(1983, 6, 29));
            }
        }

        class TestClass
        {
            private object __mock__data;

            public int Field;

            [MethodImpl(MethodImplOptions.NoInlining)]
            public int SetFieldAndReturn(int value)
            {
                return Field = value;
            }
        }
        
        [Test]
        public void InstanceMethodWorks()
        {
            var testClassMocked = Substitute.For<TestClass>();

            testClassMocked.SetFieldAndReturn(10).Returns(5);
            testClassMocked.SetFieldAndReturn(10).ShouldBe(5);
        }
    }
}