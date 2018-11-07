using System.IO;
using NSubstitute.Elevated.Tests.Utilities;
using NSubstitute.Elevated.Weaver;
using NUnit.Framework;
using Shouldly;

namespace NSubstitute.Elevated.Tests
{
    public class PeVerifyTests : TestFileSystemFixture
    {
        [Test]
        public void ExePath_CanBeFound()
        {
            var pePath = PeVerify.ExePath;

            pePath.ShouldNotBeNull();
            File.Exists(pePath).ShouldBeTrue();
        }

        [Test]
        public void ValidDll_VerifiesClean()
        {
            var dllPath = typeof(TestAttribute).Assembly.Location; // pick nunit, why not

            File.Exists(dllPath).ShouldBeTrue();
            Should.NotThrow(() => PeVerify.Verify(dllPath));
        }

        [Test]
        public void MissingDll_Throws()
        {
            var badPath = GetType().Assembly.Location + ".xyzzy";

            File.Exists(badPath).ShouldBeFalse();
            Should.Throw<PeVerifyException>(() => PeVerify.Verify(badPath));
        }

        [Test]
        public void InvalidDll_Throws()
        {
            var path = BaseDir.Combine("test.txt").WriteAllText("this is definitely not a valid dll");

            File.Exists(path).ShouldBeTrue();
            Should.Throw<PeVerifyException>(() => PeVerify.Verify(path));
        }
    }
}
