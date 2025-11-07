// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using NUnit.Framework;
using SyncKusto.Core.Services;
using System;
using System.Linq;

namespace SyncKusto.Tests.Services;

[TestFixture]
public class InMemorySettingsProviderTests
{
    private InMemorySettingsProvider _provider = null!;

    [SetUp]
    public void SetUp()
    {
        _provider = new InMemorySettingsProvider();
    }

    [Test]
    public void GetSetting_WhenKeyNotSet_ReturnsNull()
    {
        // Act
        var result = _provider.GetSetting("NonExistentKey");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void SetSetting_ThenGetSetting_ReturnsSameValue()
    {
        // Arrange
        const string key = "TestKey";
        const string value = "TestValue";

        // Act
        _provider.SetSetting(key, value);
        var result = _provider.GetSetting(key);

        // Assert
        Assert.That(result, Is.EqualTo(value));
    }

    [Test]
    public void SetSetting_WithNullKey_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _provider.SetSetting(null!, "value"));
    }

    [Test]
    public void SetSetting_WithEmptyKey_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _provider.SetSetting("", "value"));
    }

    [Test]
    public void SetSetting_WithNullValue_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _provider.SetSetting("key", null!));
    }

    [Test]
    public void GetRecentValues_WhenKeyNotSet_ReturnsEmptyList()
    {
        // Act
        var result = _provider.GetRecentValues("NonExistentKey");

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void AddRecentValue_ThenGetRecentValues_ReturnsValue()
    {
        // Arrange
        const string key = "RecentClusters";
        const string value = "cluster1.kusto.windows.net";

        // Act
        _provider.AddRecentValue(key, value);
        var result = _provider.GetRecentValues(key);

        // Assert
        Assert.That(result, Contains.Item(value));
        Assert.That(result.Count(), Is.EqualTo(1));
    }

    [Test]
    public void AddRecentValue_MultipleTimes_MaintainsOrder()
    {
        // Arrange
        const string key = "RecentClusters";

        // Act
        _provider.AddRecentValue(key, "cluster1");
        _provider.AddRecentValue(key, "cluster2");
        _provider.AddRecentValue(key, "cluster3");
        var result = _provider.GetRecentValues(key).ToList();

        // Assert
        Assert.That(result[0], Is.EqualTo("cluster3")); // Most recent first
        Assert.That(result[1], Is.EqualTo("cluster2"));
        Assert.That(result[2], Is.EqualTo("cluster1"));
    }

    [Test]
    public void AddRecentValue_DuplicateValue_MovesToTop()
    {
        // Arrange
        const string key = "RecentClusters";

        // Act
        _provider.AddRecentValue(key, "cluster1");
        _provider.AddRecentValue(key, "cluster2");
        _provider.AddRecentValue(key, "cluster3");
        _provider.AddRecentValue(key, "cluster1"); // Add duplicate
        var result = _provider.GetRecentValues(key).ToList();

        // Assert
        Assert.That(result[0], Is.EqualTo("cluster1")); // Moved to top
        Assert.That(result.Count, Is.EqualTo(3)); // No duplicates
    }

    [Test]
    public void AddRecentValue_MoreThan10Items_KeepsOnly10()
    {
        // Arrange
        const string key = "RecentClusters";

        // Act
        for (int i = 1; i <= 15; i++)
        {
            _provider.AddRecentValue(key, $"cluster{i}");
        }
        var result = _provider.GetRecentValues(key).ToList();

        // Assert
        Assert.That(result.Count, Is.EqualTo(10));
        Assert.That(result[0], Is.EqualTo("cluster15")); // Most recent
        Assert.That(result[9], Is.EqualTo("cluster6")); // 10th most recent
    }

    [Test]
    public void AddRecentValue_WithWhitespace_TrimsValue()
    {
        // Arrange
        const string key = "RecentClusters";

        // Act
        _provider.AddRecentValue(key, "  cluster1  ");
        var result = _provider.GetRecentValues(key).First();

        // Assert
        Assert.That(result, Is.EqualTo("cluster1"));
    }

    [Test]
    public void AddRecentValue_WithEmptyString_DoesNotAdd()
    {
        // Arrange
        const string key = "RecentClusters";

        // Act
        _provider.AddRecentValue(key, "");
        _provider.AddRecentValue(key, "   ");
        var result = _provider.GetRecentValues(key);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Clear_RemovesAllSettings()
    {
        // Arrange
        _provider.SetSetting("Key1", "Value1");
        _provider.SetSetting("Key2", "Value2");
        _provider.AddRecentValue("RecentKey", "value");

        // Act
        _provider.Clear();

        // Assert
        Assert.That(_provider.GetSetting("Key1"), Is.Null);
        Assert.That(_provider.GetSetting("Key2"), Is.Null);
        Assert.That(_provider.GetRecentValues("RecentKey"), Is.Empty);
    }
}
