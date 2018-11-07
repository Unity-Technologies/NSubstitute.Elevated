using System;
using System.Linq;
using NUnit.Framework;
using Shouldly;

// ReSharper disable AssignNullToNotNullAttribute
// ReSharper disable ExpressionIsAlwaysNull

namespace Unity.Core.Tests
{
    public class RefTypeExtensionsTests
    {
        [Test]
        public void WrapEnumerables_NonNullInput_ReturnsInputWrappedInEnumerable()
        {
            const string item = "test";

            var enumerable = item.WrapInEnumerable();
            enumerable.ShouldBe(new[] { item });

            enumerable = item.WrapInEnumerableOrEmpty();
            enumerable.ShouldBe(new[] { item });
        }

        [Test]
        public void WrapEnumerable_NullInput_ReturnsNullWrappedInEnumerable()
        {
            string item = null;
            var enumerable = item.WrapInEnumerable();
            enumerable.ShouldBe(new[] { item });
        }

        [Test]
        public void WrapEnumerableOrEmpty_NullInput_ReturnsEmptyEnumerable()
        {
            string item = null;
            var enumerable = item.WrapInEnumerableOrEmpty();
            enumerable.ShouldBe(Enumerable.Empty<string>());
        }
    }

    public class ComparableExtensionsTests
    {
        [Test]
        public void Clamp_BadRange_ShouldThrow()
        {
            Should.Throw<Exception>(() => 1.Clamp (2, 1));
            Should.Throw<Exception>(() => 'a'.Clamp('z', 'y'));
        }

        [Test]
        public void Clamp_InBounds_ReturnsValue()
        {
            5.Clamp (2, 10).ShouldBe(5);
            3.14.Clamp(3, 6).ShouldBe(3.14);
            'b'.Clamp('a', 'z').ShouldBe('b');
            "abc".Clamp("a", "b").ShouldBe("abc");
        }

        [Test]
        public void Clamp_OutOfBounds_ReturnsClampedValue()
        {
            15.Clamp (3, 12).ShouldBe(12);
            (-5).Clamp(-2, 4).ShouldBe(-2);

            3.14.Clamp(3.2, 4.3).ShouldBe(3.2);
            (-3.24).Clamp(-2.1, 1.5).ShouldBe(-2.1);

            'b'.Clamp('d', 'z').ShouldBe('d');
            'f'.Clamp('a', 'c').ShouldBe('c');

            "abc".Clamp("bde", "cde").ShouldBe("bde");
            "hi".Clamp("abc", "foo").ShouldBe("foo");
        }

        [Test]
        public void Clamp_Integer_ReturnsInclusiveClampedValue()
        {
            5.Clamp (0, 5).ShouldBe(5);
            5.Clamp (0, 5).ShouldNotBe(4);
        }
    }
}
