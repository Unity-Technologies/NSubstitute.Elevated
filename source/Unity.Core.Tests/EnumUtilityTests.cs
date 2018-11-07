using System;
using System.Linq;
using NUnit.Framework;
using Shouldly;

namespace Unity.Core.Tests
{
    public class EnumUtilityTests
    {
        // ReSharper disable UnusedMember.Global
        // ReSharper disable UnusedMember.Local
        // ReSharper disable InconsistentNaming
        enum SampleEnum { ValueOne = 1, AnotherValue = 2 << 4, AThirdValue = 123, FourthValue = -123 }
        enum NonCaseSensitiveUniqueNames { Value, VALUE, value }
        // ReSharper restore UnusedMember.Global
        // ReSharper restore UnusedMember.Local
        // ReSharper restore InconsistentNaming

        [Test]
        public void GetNames_MatchesFrameworkCall()
        {
            var utilNames = EnumUtility.GetNames<SampleEnum>();
            var frameworkNames = Enum.GetNames(typeof(SampleEnum));

            utilNames.ShouldBe(frameworkNames);
        }

        [Test]
        public void GetLowercaseNames_WithCaseSensitiveUniqueNames_MatchesLowercasedFrameworkCall()
        {
            var utilNames = EnumUtility.GetLowercaseNames<SampleEnum>();
            var frameworkNames = Enum.GetNames(typeof(SampleEnum)).Select(n => n.ToLower());

            utilNames.ShouldBe(frameworkNames);
        }

        [Test]
        public void GetLowercaseNames_WithNonCaseSensitiveUniqueNames_Throws()
        {
            Should
                .Throw<Exception>(() => EnumUtility.GetLowercaseNames<NonCaseSensitiveUniqueNames>())
                .Message.ShouldContain("Unexpected case insensitive duplicates");
        }

        [Test]
        public void GetValues_MatchesFrameworkCall()
        {
            var utilValues = EnumUtility.GetValues<SampleEnum>();
            var frameworkValues = (SampleEnum[])Enum.GetValues(typeof(SampleEnum));

            utilValues.ShouldBe(frameworkValues);
        }
    }
}
