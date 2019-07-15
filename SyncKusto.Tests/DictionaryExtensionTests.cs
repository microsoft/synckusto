// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Bogus;
using NUnit.Framework;
using SyncKusto.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace SyncKusto.Tests
{
    public class DictionaryExtensionTests
    {
        private static Faker Fake => new Faker();

        private static string RandomWord => Fake.Lorem.Word();

        public static string CommonWord { get; } = Fake.Commerce.Color();

        public static Dictionary<string, string> CommonKeyCommonValue() =>
            new Dictionary<string, string>() {{CommonWord, CommonWord}};

        public static Dictionary<string, string> CommonKeyRandomValue() =>
            new Dictionary<string, string>() {{CommonWord, RandomWord}};

        public static Dictionary<string, string> RandomKeyRandomValue() =>
            new Dictionary<string, string>() {{RandomWord, RandomWord}};

        public static Dictionary<string, string> CommonKeyCommonValueWithRandomKeyRandomValue() =>
            CommonKeyCommonValue().Concat(RandomKeyRandomValue()).ToDictionary(x => x.Key, x => x.Value);

        public static Dictionary<string, string> EmptyDictionary() => new Dictionary<string, string>() { };

        private static IEnumerable<object[]> NoDifferencesTestData() =>
            new[]
            {
                new object[] {(source: CommonKeyCommonValue(),
                    target: CommonKeyCommonValue())},

                new object[] {(source: EmptyDictionary(),
                    target: EmptyDictionary())}
            };

        private static IEnumerable<object[]> DifferencesTestData() =>
            new[]
            {
                new object[] {(source: CommonKeyCommonValue(),
                    target: CommonKeyRandomValue())},

                new object[] {(source: CommonKeyCommonValue(),
                    target: EmptyDictionary())},

                new object[] {(source: EmptyDictionary(),
                    target: CommonKeyCommonValue())},

                new object[] {(source: CommonKeyRandomValue().Concat(RandomKeyRandomValue()).ToDictionary(x => x.Key, x => x.Value),
                    target: RandomKeyRandomValue().Concat(RandomKeyRandomValue()).ToDictionary(x => x.Key, x => x.Value))}
            };

        [Test]
        public void Difference_Finds_Only_In_Target_Differences()
        {
            var source = CommonKeyCommonValue();
            var target = CommonKeyCommonValueWithRandomKeyRandomValue();

            var results = source.DifferenceFrom(target);

            Assert.Multiple(() =>
            {
                Assert.That(Differences(results));
                Assert.That(results.onlyInTarget, Has.Exactly(1).Items);
                Assert.That(results.onlyInTarget.ContainsKey(results.onlyInTarget.First().Key));
            });
        }

        [Test]
        public void Difference_Finds_Only_In_Source_Differences()
        {
            var source = CommonKeyCommonValueWithRandomKeyRandomValue();
            var target = CommonKeyCommonValue();

            var results = source.DifferenceFrom(target);

            Assert.Multiple(() =>
            {
                Assert.That(Differences(results));
                Assert.That(results.onlyInSource, Has.Exactly(1).Items);
                Assert.That(results.onlyInSource.ContainsKey(results.onlyInSource.First().Key));
            });
        }

        [Test]
        public void Difference_Finds_And_Keeps_Source_Content_When_Modified_Found()
        {
            var source = new Dictionary<string, string>() { { CommonWord, CommonWord } };
            var target = new Dictionary<string, string>() { { CommonWord, RandomWord } };

            var results = source.DifferenceFrom(target);

            Assert.Multiple(() =>
            {
                Assert.That(Differences(results));
                Assert.That(results.modified, Has.Exactly(1).Items);
                Assert.That(results.modified, Is.EqualTo(source));
            });
        }

        [Test]
        [TestCaseSource(nameof(NoDifferencesTestData))]
        public void Difference_Finds_No_Differences(
            (Dictionary<string, string> source, Dictionary<string, string> target) data) =>
            Assert.That(NoDifferences(data.source.DifferenceFrom(data.target)));

        [Test]
        [TestCaseSource(nameof(DifferencesTestData))]
        public void Difference_Finds_Differences(
            (Dictionary<string, string> source, Dictionary<string, string> target) data) =>
            Assert.That(Differences(data.source.DifferenceFrom(data.target)));

        public bool Differences(
            (IDictionary<string, string> modified, IDictionary<string, string> onlyInSource, IDictionary<string, string>
                onlyInTarget) differences) =>
            differences.modified.Any() || differences.onlyInSource.Any() || differences.onlyInTarget.Any();

        public bool NoDifferences(
            (IDictionary<string, string> modified, IDictionary<string, string> onlyInSource, IDictionary<string, string>
                onlyInTarget) differences) =>
            !differences.modified.Any() && !differences.onlyInSource.Any() && !differences.onlyInTarget.Any();
    }
}