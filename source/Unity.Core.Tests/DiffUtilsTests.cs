using System.Linq;
using NUnit.Framework;
using Shouldly;
using Unity.Core;

namespace Unity.Core.Tests
{
    [TestFixture]
    public class DiffUtilsTests
    {
        [Test]
        public void IsDiff_ValidLfDiff_ReturnsTrue()
        {
            var diffText = new[]
            {
                "--- a/cppupdatr/Refactor/MoveFile.cs",
                "+++ b/cppupdatr/Refactor/MoveFile.cs",
                "@@ -1,6 +1,7 @@",
            }.StringJoin('\n');

            DiffUtils.IsDiff(diffText).ShouldBeTrue();
        }

        [Test]
        public void IsDiff_ValidCrLfDiff_ReturnsTrue()
        {
            var diffText = new[]
            {
                "--- a/cppupdatr/Refactor/MoveFile.cs",
                "+++ b/cppupdatr/Refactor/MoveFile.cs",
                "@@ -1,6 +1,7 @@",
            }.StringJoin("\r\n");

            DiffUtils.IsDiff(diffText).ShouldBeTrue();
        }

        [Test]
        public void IsDiff_EmptyDiff_ReturnsFalse()
        {
            DiffUtils.IsDiff("").ShouldBeFalse();
        }

        [Test]
        public void IsDiff_BrokenDiff_ReturnsFalse()
        {
            var diffText = new[]
            {
                "--- a/cppupdatr/Refactor/MoveFile.cs",
                " +++ b/cppupdatr/Refactor/MoveFile.cs",
                "@@ -1,6 +1,7 @@"
            }.StringJoin('\n');

            DiffUtils.IsDiff(diffText).ShouldBeFalse();
        }

        [Test]
        public void IsDiff_IncompleteDiff_ReturnsFalse()
        {
            var diffText = new[]
            {
                "--- a/cppupdatr/Refactor/MoveFile.cs",
                "+++ b/cppupdatr/Refactor/MoveFile.cs",
            }.StringJoin('\n');

            DiffUtils.IsDiff(diffText).ShouldBeFalse();
        }
    }
}
