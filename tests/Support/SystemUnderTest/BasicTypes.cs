using System;
using System.Runtime.CompilerServices;

#pragma warning disable 169
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InconsistentNaming
// ReSharper disable MemberInitializerValueIgnored
// ReSharper disable PublicConstructorInAbstractClass
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMember.Global
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

        public void Dummy() {}
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

    public class ClassWithCtorICall
    {
        public ClassWithCtorICall()
        {
            DoICall();
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        static extern void DoICall();
    }

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
        public DependentAssembly.DependentType Dummy()
        {
            return new DependentAssembly.DependentType();
        }
    }

    public class SimpleClass
    {
        public int Modified;

        public void VoidMethod() => ++Modified;
        public int ReturnMethod() => ++Modified;
        public void VoidMethodWithParam(int count) => Modified += count;
        public int ReturnMethodWithParam(int count) => Modified += count;
    }
}
