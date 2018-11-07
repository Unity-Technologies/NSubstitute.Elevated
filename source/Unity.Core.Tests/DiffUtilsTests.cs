using System;
using NUnit.Framework;
using Shouldly;

namespace Unity.Core.Tests
{
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
