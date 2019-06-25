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
    }
    
    public class TestInjection
    {
        IDisposable m_Dispose;

        [OneTimeSetUp]
        public void Setup()
        {
            m_Dispose = RuntimeInjectionSupport.AutoHook();
            RuntimeInjectionSupport.InstallProxy(typeof(StaticClass).GetMethod("ReturnArgument"));
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            RuntimeInjectionSupport.RemoveProxy();
            m_Dispose.Dispose();
        }
        
        [Test]
        public void StaticMethodInjectionWorks()
        {
            using (SubstituteStatic.For<StaticClass>())
            {
                StaticClass.ReturnArgument(1).Returns(10);
                StaticClass.ReturnArgument(4).Returns(14);
                
                Console.WriteLine(StaticClass.ReturnArgument(1));
                Console.WriteLine(StaticClass.ReturnArgument(4));
                
                StaticClass.ReturnArgument(1).ShouldBe(10);
                StaticClass.ReturnArgument(4).ShouldBe(14);
            }
        }

        /*
        public static int ReturnArgument_Proxy(int i)
        {
            object returnValue = 0;
            Type mockedReturnType = typeof(int);
            Type[] methodGenericTypes = new Type[0];
            object[] args = new[] { (object) i };
            RuntimeInjectionSupport.TryMock(typeof(StaticClass), null, mockedReturnType, out returnValue, methodGenericTypes, args);

            return (int)returnValue;
        }
        */
    }
}