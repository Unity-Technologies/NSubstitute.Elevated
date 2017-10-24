using System;
using NUnit.Framework;

namespace NSubstitute.Elevated.Tests
{
    [TestFixture]
    public class MockWeaverTests
    {
        [NonSerialized]
        object __mockContext;
        [NonSerialized]
        static object __mockStaticContext;

        [Test]
        public void NonParamStaticMethod()
        {
        }
    }
}
