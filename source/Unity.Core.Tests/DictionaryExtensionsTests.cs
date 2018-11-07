using System;
using System.Collections.Generic;
using NUnit.Framework;
using Shouldly;

namespace Unity.Core.Tests
{
    public class DictionaryExtensionsTests
    {
        [Test]
        public void OrEmpty_NonNullInput_ReturnsInput()
        {
            var dictionary = new Dictionary<int, string> {[0] = "zero" };

            dictionary.OrEmpty().ShouldBe(dictionary);
        }

        [Test]
        public void OrEmpty_NullInput_ReturnsEmpty()
        {
            IReadOnlyDictionary<string, int> dictionary = null;

            // ReSharper disable once ExpressionIsAlwaysNull
            dictionary.OrEmpty().ShouldBeEmpty();
        }

        [Test]
        public void GetValueOr_Found_ReturnsFound()
        {
            var dictionary = new Dictionary<int, string> {[1] = "one" };

            dictionary.GetValueOr(1).ShouldBe("one");
            dictionary.GetValueOr(1, "two").ShouldBe("one");
        }

        [Test]
        public void GetValueOr_NotFound_ReturnsDefault()
        {
            var dictionary = new Dictionary<string, int> {["one"] = 1 };

            dictionary.GetValueOr("two").ShouldBe(0);
            dictionary.GetValueOr("two", 2).ShouldBe(2);
        }
    }
}
