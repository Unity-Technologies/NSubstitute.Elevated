using System;
using System.Text;
using NUnit.Framework;
using Shouldly;

namespace Unity.Core.Tests
{
    public class StringExtensionsTests
    {
        [Test]
        public void Left_InBounds_ReturnsSubstring()
        {
            "".Left(0).ShouldBe("");
            "abc".Left(2).ShouldBe("ab");
            "abc".Left(0).ShouldBe("");
        }

        [Test]
        public void Left_OutOfBounds_ClampsProperly()
        {
            "".Left(10).ShouldBe("");
            "abc".Left(10).ShouldBe("abc");
        }

        [Test]
        public void Left_BadInput_Throws()
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            Should.Throw<Exception>(() => ((string)null).Left(1));
            Should.Throw<Exception>(() => "abc".Left(-1));
        }

        [Test]
        public void Mid_InBounds_ReturnsSubstring()
        {
            "".Mid(0, 0).ShouldBe("");
            "abc".Mid(0, 3).ShouldBe("abc");
            "abc".Mid(0).ShouldBe("abc");
            "abc".Mid(0, -2).ShouldBe("abc");
            "abc".Mid(1, 1).ShouldBe("b");
            "abc".Mid(3, 0).ShouldBe("");
            "abc".Mid(0, 0).ShouldBe("");
        }

        [Test]
        public void Mid_OutOfBounds_ClampsProperly()
        {
            "".Mid(10, 5).ShouldBe("");
            "abc".Mid(0, 10).ShouldBe("abc");
            "abc".Mid(1, 10).ShouldBe("bc");
            "abc".Mid(10, 5).ShouldBe("");
        }

        [Test]
        public void Mid_BadInput_Throws()
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            Should.Throw<Exception>(() => ((string)null).Mid(1, 2));
            Should.Throw<Exception>(() => "abc".Mid(-1));
        }

        [Test]
        public void Right_InBounds_ReturnsSubstring()
        {
            "".Right(0).ShouldBe("");
            "abc".Right(2).ShouldBe("bc");
            "abc".Right(0).ShouldBe("");
        }

        [Test]
        public void Right_OutOfBounds_ClampsProperly()
        {
            "".Right(10).ShouldBe("");
            "abc".Right(10).ShouldBe("abc");
        }

        [Test]
        public void Right_BadInput_Throws()
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            Should.Throw<Exception>(() => ((string)null).Right(1));
            Should.Throw<Exception>(() => "abc".Right(-1));
        }

        [Test]
        public void Truncate_ThatDoesNotShorten_ReturnsSameInstance()
        {
            const string text = "abc def";
            ReferenceEquals(text, text.Truncate(100)).ShouldBeTrue();
            ReferenceEquals(text, text.Truncate(10)).ShouldBeTrue();
            ReferenceEquals(text, text.Truncate(text.Length)).ShouldBeTrue();
        }

        [Test]
        public void Truncate_ThatShortens_TruncatesAndAddsTrailer()
        {
            "abc def".Truncate(6).ShouldBe("abc...");
            "abc def".Truncate(5).ShouldBe("ab...");
            "abc def".Truncate(4).ShouldBe("a...");
            "abc def".Truncate(3).ShouldBe("...");

            "abc def".Truncate(6, "ghi").ShouldBe("abcghi");
            "abc def".Truncate(5, "ghi").ShouldBe("abghi");
            "abc def".Truncate(4, "ghi").ShouldBe("aghi");
            "abc def".Truncate(3, "ghi").ShouldBe("ghi");
        }

        [Test]
        public void Truncate_WithTooBigTrailer_ShouldThrow()
        {
            Should.Throw<ArgumentException>(() => "abc def".Truncate(2));
            Should.Throw<ArgumentException>(() => "abc def".Truncate(5, "123456"));
        }

        [Test]
        public void Truncate_WithUnderflow_ShouldThrow()
        {
            Should.Throw<ArgumentException>(() => "abc def".Truncate(-2));
            Should.Throw<ArgumentException>(() => "abc def".Truncate(0, "ghi"));
        }

        [Test]
        public void StringJoin_WithEmpty_ReturnsEmptyString()
        {
            var enumerable = new object[0];

            enumerable.StringJoin(", ").ShouldBe("");
            enumerable.StringJoin(';').ShouldBe("");
            enumerable.StringJoin(o => o, ", ").ShouldBe("");
            enumerable.StringJoin(o => o, ';').ShouldBe("");
        }

        [Test]
        public void StringJoin_WithSingle_ReturnsNoSeparators()
        {
            var enumerable = new[] { "abc" };

            enumerable.StringJoin(", ").ShouldBe("abc");
            enumerable.StringJoin(';').ShouldBe("abc");
            enumerable.StringJoin(o => o, ", ").ShouldBe("abc");
            enumerable.StringJoin(o => o, ';').ShouldBe("abc");
        }

        [Test]
        public void StringJoin_WithMultiple_ReturnsJoined()
        {
            var enumerable = new object[] { "abc", 0b111001, -14, 'z' };

            enumerable.StringJoin(" ==> ").ShouldBe("abc ==> 57 ==> -14 ==> z");
            enumerable.StringJoin('\n').ShouldBe("abc\n57\n-14\nz");
            enumerable.StringJoin(o => o, " <> ").ShouldBe("abc <> 57 <> -14 <> z");
            enumerable.StringJoin(o => o, ';').ShouldBe("abc;57;-14;z");
        }

        [Test]
        public void StringJoin_WithSelectorAndSimpleEnumerable_ReturnsSelectedJoined()
        {
            var enumerable = new[] { "hi", "there", "this", "", "is", "some", "stuff" };

            int Selector(string value) { return value.Length; }

            enumerable.StringJoin(Selector, ", ").ShouldBe("2, 5, 4, 0, 2, 4, 5");
            enumerable.StringJoin(Selector, ';').ShouldBe("2;5;4;0;2;4;5");
        }

        [Test]
        public void StringJoin_WithSelectorAndComplexEnumerable_ReturnsSelectedJoined()
        {
            var enumerable = new object[] { "abc", 123, null, ("hi", 1.23) };

            string Selector(object value) {  return value?.GetType().Name ?? "(null)"; }

            enumerable.StringJoin(Selector, " ** ").ShouldBe("String ** Int32 ** (null) ** ValueTuple`2");
            enumerable.StringJoin(Selector, '?').ShouldBe("String?Int32?(null)?ValueTuple`2");
        }

        [Test]
        public void ExpandTabs_WithEmpty_Returns_Empty()
        {
            "".ExpandTabs(4).ShouldBeEmpty();
        }

        [Test]
        public void ExpandTabs_WithNoTabs_ReturnsSameInstance() // i.e. no allocs
        {
            const string text = "abc def ghijkl";
            ReferenceEquals(text, text.ExpandTabs(4)).ShouldBeTrue();
        }

        [Test]
        public void ExpandTabs_WithInvalidTabWidth_Throws()
        {
            Should.Throw<ArgumentException>(() => "".ExpandTabs(-123));
            Should.Throw<ArgumentException>(() => "abc".ExpandTabs(-1));
        }

        [Test]
        public void ExpandTabs_BasicScenarios_ExpandsProperly()
        {
            "a\tbc\t\td".ExpandTabs(4).ShouldBe("a   bc      d");
            "a\tbc\t\td".ExpandTabs(3).ShouldBe("a  bc    d");
            "a\tbc\t\td".ExpandTabs(2).ShouldBe("a bc    d");
            "a\tbc\t\td".ExpandTabs(1).ShouldBe("a bc  d");
            "a\tbc\t\td".ExpandTabs(0).ShouldBe("abcd");
        }

        [Test]
        public void ExpandTabs_WithUnnecessaryBuffer_DoesNotUseBuffer()
        {
            var buffer = new StringBuilder { Capacity = 0 };

            var expandeda = "\ta".ExpandTabs(0, buffer);
            expandeda.ShouldBe("a");
            buffer.Capacity.ShouldBe(0);

            var expandedb = "\tb".ExpandTabs(1, buffer);
            expandedb.ShouldBe(" b");
            buffer.Capacity.ShouldBe(0);

            var expandedc = "\tc".ExpandTabs(2, buffer);
            expandedc.ShouldBe("  c");
            buffer.Capacity.ShouldNotBe(0);
        }

        [Test]
        public void ExpandTabs_WithReusedBuffer_DoesNotReusePreviousResults()
        {
            // this is a bugfix test. note tab width of 2 to avoid early-out that doesn't use the string builder.

            var buffer = new StringBuilder();
            "\ta".ExpandTabs(2, buffer).ShouldBe("  a");
            buffer.Length.ShouldBe(0); // no leftovers
            "\tb".ExpandTabs(2, buffer).ShouldBe("  b"); // not "  a b"
            buffer.Length.ShouldBe(0);
        }
    }
}
