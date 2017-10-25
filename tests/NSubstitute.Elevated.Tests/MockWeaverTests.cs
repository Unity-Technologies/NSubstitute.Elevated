using System;
using NUnit.Framework;

#pragma warning disable 169
// ReSharper disable InconsistentNaming

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
