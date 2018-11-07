using System;
using NiceIO;
using NUnit.Framework;

namespace NSubstitute.Elevated.Tests.Utilities
{
    public abstract class TestFileSystemFixture
    {
        protected NPath BaseDir { private set; get; }

        [OneTimeSetUp]
        public void InitFixture()
        {
            var testDir = new NPath(TestContext.CurrentContext.TestDirectory);
            BaseDir = testDir.Combine("testfs_" + GetType().Name);
            CreateTestFileSystem();
        }

        [OneTimeTearDown]
        public void TearDownFixture() => DeleteTestFileSystem();

        protected void CreateTestFileSystem()
        {
            DeleteTestFileSystem();
            BaseDir.CreateDirectory();
        }

        protected void DeleteTestFileSystem() => BaseDir.DeleteIfExists();
    }
}
