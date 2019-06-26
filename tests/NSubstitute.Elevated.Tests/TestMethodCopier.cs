using System;
using System.Reflection;
using System.Reflection.Emit;
using NUnit.Framework;

namespace NSubstitute.Elevated.Tests
{
    public class TestMethodCopier
    {
        public static int SimpleMethod(int a, int b)
        {
            return a + b;
        }

        public static int WithBranches(bool val, int a, int b)
        {
            return val ? a : b;
        }

        public static int WithDeepBranches(bool val, bool val2, int a, int b, int c, int d)
        {
            if (val)
                return val2 ? a : b;
            return val2 ? c : d;
        }
        
        public static int WithTryCatch(bool throws, bool noFinally)
        {
            var blocksExecuted = 0;
            try
            {
                blocksExecuted |= 1 << 0;
                if (throws)
                    throw new Exception();
                blocksExecuted |= 1 << 1;
            }
            catch (Exception)
            {
                blocksExecuted |= 1 << 2;
            }
            finally
            {
                if(!noFinally)
                    blocksExecuted |= 1 << 3;
                else
                    blocksExecuted |= 1 << 4;
            }

            return blocksExecuted;
        }
        
        [Test]
        public void CanCopySimpleMethod()
        {
            var methodInfo = GetType().GetMethod(nameof(SimpleMethod));
            var copy = MethodCopier.CopyMethod(methodInfo, $"{methodInfo.Name}_{methodInfo.MethodHandle.Value}");
            
            AssertMethodSignature(methodInfo, copy);

            var copyDelegate = copy.CreateDelegate(typeof(Func<int, int, int>));
            
            /*
             // Unfortunately we cannot do this, info.GetMethodBody() throws. So I have to execute the bastard instead.
            var methodBody = methodInfo.GetMethodBody();
            var info = copyDelegate.GetMethodInfo();
            var copyMethodBody = info.GetMethodBody();
            
            CollectionAssert.AreEqual(methodBody.GetILAsByteArray(), copyMethodBody.GetILAsByteArray());
            */
            
            Assert.AreEqual(SimpleMethod(1, 2), copyDelegate.DynamicInvoke(1, 2));
        }
        
        [Test]
        public void CanCopyMethodWithBranches()
        {
            var methodInfo = GetType().GetMethod(nameof(WithBranches));
            var copy = MethodCopier.CopyMethod(methodInfo, $"{methodInfo.Name}_{methodInfo.MethodHandle.Value}");
            
            AssertMethodSignature(methodInfo, copy);

            var copyDelegate = copy.CreateDelegate(typeof(Func<bool, int, int, int>));
            
            
            Assert.AreEqual(WithBranches(true, 1, 2), copyDelegate.DynamicInvoke(true, 1, 2));
            Assert.AreEqual(WithBranches(false, 1, 2), copyDelegate.DynamicInvoke(false, 1, 2));
        }
        
        [Test]
        public void CanCopyMethodWithDeepBranches()
        {
            var methodInfo = GetType().GetMethod(nameof(WithDeepBranches));
            var copy = MethodCopier.CopyMethod(methodInfo, $"{methodInfo.Name}_{methodInfo.MethodHandle.Value}");
            
            AssertMethodSignature(methodInfo, copy);

            var copyDelegate = copy.CreateDelegate(typeof(Func<bool, bool, int, int, int, int, int>));

            Assert.AreEqual(WithDeepBranches(true, true, 1, 2, 3, 4), copyDelegate.DynamicInvoke(true, true, 1, 2, 3, 4));
            Assert.AreEqual(WithDeepBranches(true, false, 1, 2, 3, 4), copyDelegate.DynamicInvoke(true, false, 1, 2, 3, 4));
            Assert.AreEqual(WithDeepBranches(false, true, 1, 2, 3, 4), copyDelegate.DynamicInvoke(false, true, 1, 2, 3, 4));
            Assert.AreEqual(WithDeepBranches(false, false, 1, 2, 3, 4), copyDelegate.DynamicInvoke(false, false, 1, 2, 3, 4));
        }

        [Test]
        public void CanCopyMethodWithExceptions()
        {
            var methodInfo = GetType().GetMethod(nameof(WithTryCatch));
            var copy = MethodCopier.CopyMethod(methodInfo, $"{methodInfo.Name}_{methodInfo.MethodHandle.Value}");
            
            AssertMethodSignature(methodInfo, copy);

            var copyDelegate = copy.CreateDelegate(typeof(Func<bool, bool, int>));
            
            Assert.AreEqual(WithTryCatch(true, true), copyDelegate.DynamicInvoke(true, true));
            Assert.AreEqual(WithTryCatch(true, false), copyDelegate.DynamicInvoke(true, false));
            Assert.AreEqual(WithTryCatch(false, true), copyDelegate.DynamicInvoke(false, true));
            Assert.AreEqual(WithTryCatch(false, false), copyDelegate.DynamicInvoke(false, false));
        }

        static void AssertMethodSignature(MethodInfo methodInfo, DynamicMethod copy)
        {
            Assert.AreEqual(methodInfo.Attributes, copy.Attributes);
            Assert.AreEqual(methodInfo.ReturnType, copy.ReturnType);
            Assert.AreEqual(methodInfo.IsStatic, copy.IsStatic);

            var parameters = methodInfo.GetParameters();
            var copyParameters = copy.GetParameters();

            Assert.AreEqual(parameters.Length, copyParameters.Length);

            for (var i = 0; i < parameters.Length; i++)
            {
                Assert.AreEqual(parameters[i].ParameterType, copyParameters[i].ParameterType);
                Assert.AreEqual(parameters[i].Attributes, copyParameters[i].Attributes);
            }
        }
    }
}
