// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using FluentAssertions;
using NUnit.Framework;
using SyncKusto.Core.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace SyncKusto.Tests.Extensions;

[TestFixture]
public class DictionaryExtensionsEnhancedTests
{
    [Test]
    public void DifferenceFrom_WithNullSource_ThrowsArgumentNullException()
    {
        // Arrange
        Dictionary<string, string>? source = null;
        var target = new Dictionary<string, string>();

        // Act
        var act = () => source!.DifferenceFrom(target);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void DifferenceFrom_WithNullTarget_ThrowsArgumentNullException()
    {
        // Arrange
        var source = new Dictionary<string, string>();
        Dictionary<string, string>? target = null;

        // Act
        var act = () => source.DifferenceFrom(target!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void DifferenceFrom_WithBothNull_ThrowsArgumentNullException()
    {
        // Arrange
        Dictionary<string, string>? source = null;
        Dictionary<string, string>? target = null;

        // Act
        var act = () => source!.DifferenceFrom(target!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void DifferenceFrom_WithEmptyDictionaries_ReturnsEmptyDifferences()
    {
        // Arrange
        var source = new Dictionary<string, string>();
        var target = new Dictionary<string, string>();

        // Act
        var result = source.DifferenceFrom(target);

        // Assert
        result.modified.Should().BeEmpty();
        result.onlyInSource.Should().BeEmpty();
        result.onlyInTarget.Should().BeEmpty();
    }

    [Test]
    public void DifferenceFrom_WithIdenticalDictionaries_ReturnsEmptyDifferences()
    {
        // Arrange
        var source = new Dictionary<string, string>
        {
            ["key1"] = "value1",
            ["key2"] = "value2",
            ["key3"] = "value3"
        };
        var target = new Dictionary<string, string>
        {
            ["key1"] = "value1",
            ["key2"] = "value2",
            ["key3"] = "value3"
        };

        // Act
        var result = source.DifferenceFrom(target);

        // Assert
        result.modified.Should().BeEmpty();
        result.onlyInSource.Should().BeEmpty();
        result.onlyInTarget.Should().BeEmpty();
    }

    [Test]
    public void DifferenceFrom_WithOnlySourceItems_ReturnsOnlyInSource()
    {
        // Arrange
        var source = new Dictionary<string, string>
        {
            ["key1"] = "value1",
            ["key2"] = "value2"
        };
        var target = new Dictionary<string, string>();

        // Act
        var result = source.DifferenceFrom(target);

        // Assert
        result.modified.Should().BeEmpty();
        result.onlyInSource.Should().HaveCount(2);
        result.onlyInSource.Should().ContainKey("key1").WhoseValue.Should().Be("value1");
        result.onlyInSource.Should().ContainKey("key2").WhoseValue.Should().Be("value2");
        result.onlyInTarget.Should().BeEmpty();
    }

    [Test]
    public void DifferenceFrom_WithOnlyTargetItems_ReturnsOnlyInTarget()
    {
        // Arrange
        var source = new Dictionary<string, string>();
        var target = new Dictionary<string, string>
        {
            ["key1"] = "value1",
            ["key2"] = "value2"
        };

        // Act
        var result = source.DifferenceFrom(target);

        // Assert
        result.modified.Should().BeEmpty();
        result.onlyInSource.Should().BeEmpty();
        result.onlyInTarget.Should().HaveCount(2);
        result.onlyInTarget.Should().ContainKey("key1").WhoseValue.Should().Be("value1");
        result.onlyInTarget.Should().ContainKey("key2").WhoseValue.Should().Be("value2");
    }

    [Test]
    public void DifferenceFrom_WithModifiedValues_ReturnsModified()
    {
        // Arrange
        var source = new Dictionary<string, string>
        {
            ["key1"] = "sourceValue1",
            ["key2"] = "sourceValue2"
        };
        var target = new Dictionary<string, string>
        {
            ["key1"] = "targetValue1",
            ["key2"] = "targetValue2"
        };

        // Act
        var result = source.DifferenceFrom(target);

        // Assert
        result.modified.Should().HaveCount(2);
        result.modified.Should().ContainKey("key1").WhoseValue.Should().Be("sourceValue1");
        result.modified.Should().ContainKey("key2").WhoseValue.Should().Be("sourceValue2");
        result.onlyInSource.Should().BeEmpty();
        result.onlyInTarget.Should().BeEmpty();
    }

    [Test]
    public void DifferenceFrom_WithMixedDifferences_ReturnsCategorizedDifferences()
    {
        // Arrange
        var source = new Dictionary<string, string>
        {
            ["unchanged"] = "same",
            ["modified"] = "sourceValue",
            ["onlyInSource"] = "sourceOnly"
        };
        var target = new Dictionary<string, string>
        {
            ["unchanged"] = "same",
            ["modified"] = "targetValue",
            ["onlyInTarget"] = "targetOnly"
        };

        // Act
        var result = source.DifferenceFrom(target);

        // Assert
        result.modified.Should().HaveCount(1);
        result.modified.Should().ContainKey("modified");
        result.modified["modified"].Should().Be("sourceValue");
        
        result.onlyInSource.Should().HaveCount(1);
        result.onlyInSource.Should().ContainKey("onlyInSource");
        result.onlyInSource["onlyInSource"].Should().Be("sourceOnly");
        
        result.onlyInTarget.Should().HaveCount(1);
        result.onlyInTarget.Should().ContainKey("onlyInTarget");
        result.onlyInTarget["onlyInTarget"].Should().Be("targetOnly");
    }

    [Test]
    public void DifferenceFrom_WithCaseSensitiveKeys_TreatsAsDifferent()
    {
        // Arrange
        var source = new Dictionary<string, string>
        {
            ["Key"] = "value"
        };
        var target = new Dictionary<string, string>
        {
            ["key"] = "value"
        };

        // Act
        var result = source.DifferenceFrom(target);

        // Assert
        result.modified.Should().BeEmpty();
        result.onlyInSource.Should().ContainKey("Key");
        result.onlyInTarget.Should().ContainKey("key");
    }

    [Test]
    public void DifferenceFrom_WithNullValues_HandlesCorrectly()
    {
        // Arrange
        var source = new Dictionary<string, string?>
        {
            ["key1"] = null,
            ["key2"] = "value"
        };
        var target = new Dictionary<string, string?>
        {
            ["key1"] = "value",
            ["key2"] = null
        };

        // Act
        var result = source.DifferenceFrom(target);

        // Assert
        result.modified.Should().HaveCount(2);
    }

    [Test]
    public void DifferenceFrom_WithWhitespaceValues_TreatsAsDistinct()
    {
        // Arrange
        var source = new Dictionary<string, string>
        {
            ["key1"] = "value",
            ["key2"] = " value"
        };
        var target = new Dictionary<string, string>
        {
            ["key1"] = "value ",
            ["key2"] = " value"
        };

        // Act
        var result = source.DifferenceFrom(target);

        // Assert
        result.modified.Should().ContainKey("key1");
        result.onlyInSource.Should().BeEmpty();
        result.onlyInTarget.Should().BeEmpty();
    }

    [Test]
    public void DifferenceFrom_WithLargeDictionaries_CompletesEfficiently()
    {
        // Arrange
        var source = Enumerable.Range(0, 10000)
            .ToDictionary(i => $"key{i}", i => $"value{i}");
        var target = Enumerable.Range(5000, 10000)
            .ToDictionary(i => $"key{i}", i => $"value{i}");

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var result = source.DifferenceFrom(target);

        stopwatch.Stop();

        // Assert
        result.onlyInSource.Should().HaveCount(5000);
        result.onlyInTarget.Should().HaveCount(5000);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000, 
            "Dictionary difference operation should complete efficiently");
    }

    [Test]
    public void DifferenceFrom_WithComplexKeys_HandlesCorrectly()
    {
        // Arrange
        var source = new Dictionary<string, string>
        {
            ["table[column1, column2]"] = "schema1",
            ["function(arg1:string, arg2:int)"] = "body1"
        };
        var target = new Dictionary<string, string>
        {
            ["table[column1, column3]"] = "schema2",
            ["function(arg1:string, arg2:int)"] = "body2"
        };

        // Act
        var result = source.DifferenceFrom(target);

        // Assert
        result.modified.Should().ContainKey("function(arg1:string, arg2:int)");
        result.onlyInSource.Should().ContainKey("table[column1, column2]");
        result.onlyInTarget.Should().ContainKey("table[column1, column3]");
    }

    [Test]
    public void DifferenceFrom_PreservesSourceValues_InModifiedDictionary()
    {
        // Arrange
        var source = new Dictionary<string, string>
        {
            ["key"] = "sourceValue"
        };
        var target = new Dictionary<string, string>
        {
            ["key"] = "targetValue"
        };

        // Act
        var result = source.DifferenceFrom(target);

        // Assert
        result.modified["key"].Should().Be("sourceValue", 
            "Modified dictionary should contain source values, not target values");
    }
}
