using System;
using System.Runtime.CompilerServices;
using NSubstitute.Elevated.RuntimeInjection;
using NUnit.Framework;
using Shouldly;

namespace NSubstitute.Elevated.Tests
{
    class StaticClass
    {
        static object __mock__staticData;
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int ReturnArgument(int i)
        {
            return i;
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int ReturnHalfArgument(int i)
        {
            return i/2;
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
        [Ignore("This doesn't work. Need to debug what's going on, maybe it will be easier on windows.")]
        public void StaticMethodInjectionWorks2()
        {
            using (SubstituteStatic.For<StaticClass>())
            {
                StaticClass.ReturnArgument(2).ShouldBe(2);
            }
        }
    }
}