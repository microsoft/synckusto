// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using NUnit.Framework;
using SyncKusto.Core.Models;
using SyncKusto.Services;
using System;
using System.IO;

namespace SyncKusto.Tests.Services;

[TestFixture]
public class JsonFileSettingsProviderTests
{
    private string _testFilePath = null!;
    private JsonFileSettingsProvider _provider = null!;

    [SetUp]
    public void SetUp()
    {
        // Create a temporary file path for testing
        _testFilePath = Path.Combine(Path.GetTempPath(), $"test_settings_{Guid.NewGuid()}.json");
        _provider = new JsonFileSettingsProvider(_testFilePath);
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up test file
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }
    }

    [Test]
    public void GetSetting_WhenKeyNotSet_ReturnsNull()
    {
        // Act
        var result = _provider.GetSetting("PreviousFilePath");

        // Assert - Should return null for unset string values
        Assert.That(result, Is.Null.Or.Empty);
    }

    [Test]
    public void SetSetting_ThenGetSetting_ReturnsSameValue()
    {
        // Arrange
        const string value = "C:\\test\\path";

        // Act
        _provider.SetSetting("PreviousFilePath", value);
        var result = _provider.GetSetting("PreviousFilePath");

        // Assert
        Assert.That(result, Is.EqualTo(value));
    }

    [Test]
    public void SetSetting_PersistsToFile()
    {
        // Arrange
        const string value = "https://mycluster.kusto.windows.net";

        // Act
        _provider.SetSetting("TempCluster", value);

        // Create a new provider instance to verify persistence
        var newProvider = new JsonFileSettingsProvider(_testFilePath);
        var result = newProvider.GetSetting("TempCluster");

        // Assert
        Assert.That(result, Is.EqualTo(value));
    }

    [Test]
    public void GetSetting_SupportsMultipleKeyAliases()
    {
        // Arrange
        const string value = "https://mycluster.kusto.windows.net";

        // Act
        _provider.SetSetting("TempCluster", value);

        // Assert - Both key names should work
        Assert.That(_provider.GetSetting("TempCluster"), Is.EqualTo(value));
        Assert.That(_provider.GetSetting("KustoClusterForTempDatabases"), Is.EqualTo(value));
    }

    [Test]
    public void GetSetting_FileExtension_ReturnsCslWhenLegacyEnabled()
    {
        // Arrange
        _provider.SetSetting("UseLegacyCslExtension", "true");

        // Act
        var result = _provider.GetSetting("FileExtension");

        // Assert
        Assert.That(result, Is.EqualTo("csl"));
    }

    [Test]
    public void GetSetting_FileExtension_ReturnsKqlWhenLegacyDisabled()
    {
        // Arrange
        _provider.SetSetting("UseLegacyCslExtension", "false");

        // Act
        var result = _provider.GetSetting("FileExtension");

        // Assert
        Assert.That(result, Is.EqualTo("kql"));
    }

    [Test]
    public void AddRecentValue_ThenGetRecentValues_ReturnsValue()
    {
        // Arrange
        const string value = "cluster1.kusto.windows.net";

        // Act
        _provider.AddRecentValue("RecentClusters", value);
        var result = _provider.GetRecentValues("RecentClusters");

        // Assert
        Assert.That(result, Contains.Item(value));
    }

    [Test]
    public void AddRecentValue_PersistsToFile()
    {
        // Arrange
        const string value = "cluster1.kusto.windows.net";

        // Act
        _provider.AddRecentValue("RecentClusters", value);

        // Create a new provider instance to verify persistence
        var newProvider = new JsonFileSettingsProvider(_testFilePath);
        var result = newProvider.GetRecentValues("RecentClusters");

        // Assert
        Assert.That(result, Contains.Item(value));
    }

    [Test]
    public void GetSettings_ReturnsImmutableConfiguration()
    {
        // Arrange
        _provider.SetSetting("TempCluster", "https://test.kusto.windows.net");
        _provider.SetSetting("TempDatabase", "TestDB");
        _provider.SetSetting("AADAuthority", "https://login.microsoftonline.com");
        _provider.SetSetting("KustoObjectDropWarning", "true");
        _provider.SetSetting("TableFieldsOnNewLine", "true");
        _provider.SetSetting("CreateMergeEnabled", "false");
        _provider.SetSetting("UseLegacyCslExtension", "false");

        // Act
        var settings = _provider.GetSettings();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(settings.TempCluster, Is.EqualTo("https://test.kusto.windows.net"));
            Assert.That(settings.TempDatabase, Is.EqualTo("TestDB"));
            Assert.That(settings.AADAuthority, Is.EqualTo("https://login.microsoftonline.com"));
            Assert.That(settings.KustoObjectDropWarning, Is.True);
            Assert.That(settings.TableFieldsOnNewLine, Is.True);
            Assert.That(settings.CreateMergeEnabled, Is.False);
            Assert.That(settings.UseLegacyCslExtension, Is.False);
            Assert.That(settings.FileExtension, Is.EqualTo("kql"));
            Assert.That(settings.LineEndingMode, Is.EqualTo(LineEndingMode.LeaveAsIs));
            Assert.That(settings.CertificateLocation, Is.EqualTo(StoreLocation.CurrentUser));
        });
    }

    [Test]
    public void GetSettings_WithDefaults_ReturnsExpectedDefaults()
    {
        // Act
        var settings = _provider.GetSettings();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(settings.KustoObjectDropWarning, Is.True);
            Assert.That(settings.TableFieldsOnNewLine, Is.False);
            Assert.That(settings.CreateMergeEnabled, Is.False);
            Assert.That(settings.UseLegacyCslExtension, Is.True);
            Assert.That(settings.FileExtension, Is.EqualTo("csl"));
            Assert.That(settings.LineEndingMode, Is.EqualTo(LineEndingMode.LeaveAsIs));
            Assert.That(settings.CertificateLocation, Is.EqualTo(StoreLocation.CurrentUser));
        });
    }

    [Test]
    public void Constructor_CreatesDirectoryIfNotExists()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"test_dir_{Guid.NewGuid()}");
        var settingsPath = Path.Combine(tempDir, "settings.json");

        try
        {
            // Act
            var provider = new JsonFileSettingsProvider(settingsPath);
            provider.SetSetting("TempCluster", "test");

            // Assert
            Assert.That(Directory.Exists(tempDir), Is.True);
            Assert.That(File.Exists(settingsPath), Is.True);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Test]
    public void GetSetting_WithInvalidKey_ReturnsNull()
    {
        // Act
        var result = _provider.GetSetting("InvalidKey");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void SetSetting_WithInvalidKey_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _provider.SetSetting("InvalidKey", "value"));
    }
}
