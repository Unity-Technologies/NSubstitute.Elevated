using System;

#if TEST_ICALLS
using System.Runtime.CompilerServices;
#endif

// ReSharper disable MemberInitializerValueIgnored
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameter.Local

namespace SystemUnderTest
{
    public interface IBasicInterface
    {
        int GetValue();
    }

    public class ClassWithVirtuals
    {
        public virtual int GetValue() => 4;
    }

    public class ClassWithDefaultCtor
    {
        public ClassWithDefaultCtor()
        {
            Value = 123;
        }

        public int Value = 234;

        void Dummy() {}
    }

    public class ClassWithNoDefaultCtor
    {
        public ClassWithNoDefaultCtor(string i) {}
        public ClassWithNoDefaultCtor(string i1, string i2) {}

        void Dummy() {}
    }

    public class ClassWithNoDefaultCtorNoMethods
    {
        public ClassWithNoDefaultCtorNoMethods(string i) {}
        public ClassWithNoDefaultCtorNoMethods(string i1, string i2) {}
    }

#   if TEST_ICALLS
    public class ClassWithCtorICall
    {
        public ClassWithCtorICall()
        {
            DoICall();
        }

        [MethodImpl((MethodImplOptions) 0x1000)]
        static extern void DoICall();
    }
#   endif // TEST_ICALLS

    public class ClassWithCtorThrow
    {
        public ClassWithCtorThrow()
        {
            throw new InvalidOperationException();
        }

        void Dummy() {}
    }

    public class EmptyClass
    {
    }

    public class ClassWithDependency
    {
        public DependentAssembly.DependentType Dummy => new DependentAssembly.DependentType();
    }
}
