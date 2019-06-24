using System;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using Shouldly;

namespace NSubstitute.Elevated.Tests
{
    public class TestPatching
    {
        IDisposable m_Dispose;

        [OneTimeSetUp]
        public void Setup()
        {
            m_Dispose = PatchingSubstituteContext.AutoHook();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            m_Dispose.Dispose();
        }
        
        
        public class StaticClass
        {
            static object __mock__staticData;
        
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static int ReturnArgument(int i)
            {
                return i;
            }
        }
        
        [Test]
        public void StaticMethodPatchingWorks()
        {
            var @delegate = PatchingSupport.Rewrite(TheActualTest1);
            @delegate.DynamicInvoke();
        }
        
        static void TheActualTest1()
        {
            using (SubstituteStatic.For<StaticClass>())
            {
                StaticClass.ReturnArgument(1).Returns(10);

                StaticClass.ReturnArgument(1).ShouldBe(10);
            }
        }

        static void TheActualTest()
        {
            using (SubstituteStatic.For<StaticClass>())
            {
                StaticClass.ReturnArgument(1).Returns(10);
                StaticClass.ReturnArgument(4).Returns(14);

                StaticClass.ReturnArgument(1).ShouldBe(10);
                CallWith4();
            }
        }

        public static void CallWith4()
        {
            StaticClass.ReturnArgument(4).ShouldBe(14);
        }

        public class ExpectedGeneratedCode
        {
            [Test]
            public void StaticMethodPatchingWorks()
            {
                using (SubstituteStatic.For<StaticClass>())
                {
                    ReturnArgument_Proxy(1).Returns(10);
                    ReturnArgument_Proxy(4).Returns(14);

                    ReturnArgument_Proxy(1).ShouldBe(10);
                    CallWith4_Rewriter();
                }
            }

            public static void CallWith4()
            {
                ReturnArgument_Proxy(4).ShouldBe(14);
            }

            public static int ReturnArgument_Proxy(int i)
            {
                // TryMock
                return i;   
            }

            public void CallWith4_Rewriter()
            {
                var callWith4Delegate = GetOrGenerateRewrittenMethodFor(nameof(CallWith4));

                callWith4Delegate.DynamicInvoke();
            }

            Delegate GetOrGenerateRewrittenMethodFor(string name)
            {
                if (!IsMethodRewritten(name))
                {
                    Rewrite(name);
                }

                return null;
            }

            bool IsMethodRewritten(string callWith4Name)
            {
                throw new NotImplementedException();
            }

            void Rewrite(string callWith4Name)
            {
                throw new NotImplementedException();
            }
        }
    }
}

// [ ] Generate proxy dynamically specifically for that method
// [ ] Generate proxy generically
// [ ] Generate proxy for instance
// [ ] Generate proxy for structs
// [ ] Remove the needs of injection (GetHashCode + Global dictionary/instance + per-type in case of statics)
// [ ] Clenaup code