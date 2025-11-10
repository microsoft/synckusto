// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using FluentAssertions;
using NUnit.Framework;
using SyncKusto.Core.Models;
using SyncKusto.Services;

namespace SyncKusto.Tests.Integration;

/// <summary>
/// Integration tests for file system operations
/// These tests verify end-to-end functionality with actual file system interactions
/// </summary>
[TestFixture]
[Category("Integration")]
public class FileSystemIntegrationTests
{
    private string _testDirectory = null!;
    private JsonFileSettingsProvider _settingsProvider = null!;

    [SetUp]
    public void SetUp()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"SyncKustoTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);

        var settingsPath = Path.Combine(_testDirectory, "settings.json");
        _settingsProvider = new JsonFileSettingsProvider(settingsPath);
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }

    [Test]
    public void SettingsProvider_CompleteWorkflow_PersistsAndLoadsData()
    {
        // Arrange & Act - Set multiple settings
        _settingsProvider.SetSetting("TempCluster", "https://test.kusto.windows.net");
        _settingsProvider.SetSetting("TempDatabase", "TestDB");
        _settingsProvider.SetSetting("KustoObjectDropWarning", "false");
        _settingsProvider.AddRecentValue("RecentClusters", "cluster1.kusto.windows.net");
        _settingsProvider.AddRecentValue("RecentClusters", "cluster2.kusto.windows.net");

        // Create new provider instance to verify persistence
        var settingsPath = Path.Combine(_testDirectory, "settings.json");
        var newProvider = new JsonFileSettingsProvider(settingsPath);

        // Assert - All data should be persisted
        newProvider.GetSetting("TempCluster").Should().Be("https://test.kusto.windows.net");
        newProvider.GetSetting("TempDatabase").Should().Be("TestDB");
        // Boolean values may be stored as "False" or "false" depending on JSON serialization
        newProvider.GetSetting("KustoObjectDropWarning").Should().BeOneOf("false", "False");

        var recentClusters = newProvider.GetRecentValues("RecentClusters").ToList();
        recentClusters.Should().Contain("cluster1.kusto.windows.net");
        recentClusters.Should().Contain("cluster2.kusto.windows.net");
    }

    [Test]
    public void SettingsProvider_MultipleInstances_DoNotInterfere()
    {
        // Arrange
        var settings1Path = Path.Combine(_testDirectory, "settings1.json");
        var settings2Path = Path.Combine(_testDirectory, "settings2.json");

        var provider1 = new JsonFileSettingsProvider(settings1Path);
        var provider2 = new JsonFileSettingsProvider(settings2Path);

        // Act
        provider1.SetSetting("TempCluster", "cluster1");
        provider2.SetSetting("TempCluster", "cluster2");

        // Assert
        provider1.GetSetting("TempCluster").Should().Be("cluster1");
        provider2.GetSetting("TempCluster").Should().Be("cluster2");
    }

    [Test]
    public void SettingsProvider_NestedDirectory_CreatesHierarchy()
    {
        // Arrange
        var nestedPath = Path.Combine(_testDirectory, "level1", "level2", "level3", "settings.json");
        var provider = new JsonFileSettingsProvider(nestedPath);

        // Act
        provider.SetSetting("TempCluster", "test");

        // Assert
        File.Exists(nestedPath).Should().BeTrue();
        provider.GetSetting("TempCluster").Should().Be("test");
    }

    [Test]
    public void SettingsProvider_UpdateExistingValue_Overwrites()
    {
        // Act
        _settingsProvider.SetSetting("TempCluster", "cluster1");
        _settingsProvider.SetSetting("TempCluster", "cluster2");
        _settingsProvider.SetSetting("TempCluster", "cluster3");

        // Assert
        _settingsProvider.GetSetting("TempCluster").Should().Be("cluster3");
    }

    [Test]
    public void SettingsProvider_RecentValues_MaintainsOrderAndLimit()
    {
        // Arrange - Add more than the limit (10)
        for (int i = 1; i <= 15; i++)
        {
            _settingsProvider.AddRecentValue("RecentClusters", $"cluster{i}");
        }

        // Act
        var recent = _settingsProvider.GetRecentValues("RecentClusters").ToList();

        // Assert
        recent.Should().HaveCount(10);
        recent[0].Should().Be("cluster15"); // Most recent
        recent[9].Should().Be("cluster6"); // 10th most recent
    }

    [Test]
    public void SettingsProvider_SpecialCharactersInValues_PersistsCorrectly()
    {
        // Arrange
        var specialCluster = "https://cluster.kusto.windows.net?param=value&other=\"quoted\"";
        var specialPath = @"C:\Path\With\Backslashes\And Space";

        // Act
        _settingsProvider.SetSetting("TempCluster", specialCluster);
        _settingsProvider.SetSetting("PreviousFilePath", specialPath);

        // Reload from file
        var settingsPath = Path.Combine(_testDirectory, "settings.json");
        var newProvider = new JsonFileSettingsProvider(settingsPath);

        // Assert
        newProvider.GetSetting("TempCluster").Should().Be(specialCluster);
        newProvider.GetSetting("PreviousFilePath").Should().Be(specialPath);
    }

    [Test]
    public void SettingsProvider_GetSettings_ReturnsImmutableSnapshot()
    {
        // Arrange
        _settingsProvider.SetSetting("TempCluster", "cluster1");
        _settingsProvider.SetSetting("TempDatabase", "db1");

        // Act
        var settings1 = _settingsProvider.GetSettings();

        // Modify provider
        _settingsProvider.SetSetting("TempCluster", "cluster2");

        var settings2 = _settingsProvider.GetSettings();

        // Assert
        settings1.TempCluster.Should().Be("cluster1");
        settings2.TempCluster.Should().Be("cluster2");
    }

    [Test]
    public void SettingsProvider_FileSystemFullPath_HandlesLongPaths()
    {
        // Arrange - Create a very long path (but within OS limits)
        var longDirName = new string('a', 100);
        var longPath = Path.Combine(_testDirectory, longDirName, "settings.json");

        // Act
        var provider = new JsonFileSettingsProvider(longPath);
        provider.SetSetting("TempCluster", "test");

        // Assert
        File.Exists(longPath).Should().BeTrue();
        provider.GetSetting("TempCluster").Should().Be("test");
    }

    [Test]
    public void SettingsProvider_ConcurrentReads_ReturnConsistentData()
    {
        // Arrange
        _settingsProvider.SetSetting("TempCluster", "test-cluster");
        var settingsPath = Path.Combine(_testDirectory, "settings.json");

        // Act - Simulate concurrent reads
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() =>
            {
                var provider = new JsonFileSettingsProvider(settingsPath);
                return provider.GetSetting("TempCluster");
            }))
            .ToArray();

        Task.WaitAll(tasks);

        // Assert - All reads should return the same value
        tasks.Should().OnlyContain(t => t.Result == "test-cluster");
    }

    [Test]
    public void SettingsProvider_EmptyStringValues_PersistsCorrectly()
    {
        // Act
        _settingsProvider.SetSetting("AADAuthority", string.Empty);

        // Assert
        _settingsProvider.GetSetting("AADAuthority").Should().Be(string.Empty);
    }

    [Test]
    public void SettingsProvider_BooleanConversions_WorkCorrectly()
    {
        // Act
        _settingsProvider.SetSetting("KustoObjectDropWarning", "true");
        _settingsProvider.SetSetting("TableFieldsOnNewLine", "false");

        var settings = _settingsProvider.GetSettings();

        // Assert
        settings.KustoObjectDropWarning.Should().BeTrue();
        settings.TableFieldsOnNewLine.Should().BeFalse();
    }

    [Test]
    public void SettingsProvider_EnumConversions_WorkCorrectly()
    {
        // Act
        _settingsProvider.SetSetting("LineEndingMode", "1"); // WindowsStyle
        _settingsProvider.SetSetting("CertificateLocation", "LocalMachine");

        var settings = _settingsProvider.GetSettings();

        // Assert
        settings.LineEndingMode.Should().Be(LineEndingMode.WindowsStyle);
        settings.CertificateLocation.Should().Be(StoreLocation.LocalMachine);
    }
}
