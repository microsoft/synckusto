// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using NUnit.Framework;
using SyncKusto.Core.Models;
using SyncKusto.Core.Services;

namespace SyncKusto.Tests.Core.Services;

[TestFixture]
public class SchemaValidationServiceTests
{
    private InMemorySettingsProvider _settingsProvider = null!;

    [SetUp]
    public void SetUp()
    {
        _settingsProvider = new InMemorySettingsProvider();
    }

    [Test]
    public void Constructor_WithNullSettingsProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SchemaValidationService(null!));
    }

    [Test]
    public void ValidateSettings_WithNullSource_ThrowsArgumentNullException()
    {
        // Arrange
        var service = new SchemaValidationService(_settingsProvider);
        var target = new SchemaSourceInfo(SourceSelection.Kusto());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.ValidateSettings(null!, target));
    }

    [Test]
    public void ValidateSettings_WithNullTarget_ThrowsArgumentNullException()
    {
        // Arrange
        var service = new SchemaValidationService(_settingsProvider);
        var source = new SchemaSourceInfo(SourceSelection.Kusto());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.ValidateSettings(source, null!));
    }

    [Test]
    public void ValidateSettings_WithKustoToKusto_ReturnsSuccess()
    {
        // Arrange
        var service = new SchemaValidationService(_settingsProvider);
        var source = new SchemaSourceInfo(SourceSelection.Kusto());
        var target = new SchemaSourceInfo(SourceSelection.Kusto());

        // Act
        var result = service.ValidateSettings(source, target);

        // Assert
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.ErrorMessage, Is.Null);
    }

    [Test]
    public void ValidateSettings_WithFilePathSourceAndNoTempCluster_ReturnsFailure()
    {
        // Arrange
        var service = new SchemaValidationService(_settingsProvider);
        var source = new SchemaSourceInfo(SourceSelection.FilePath(), FilePath: "/some/path");
        var target = new SchemaSourceInfo(SourceSelection.Kusto());

        // Act
        var result = service.ValidateSettings(source, target);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("temp cluster"));
    }

    [Test]
    public void ValidateSettings_WithFilePathTargetAndNoTempCluster_ReturnsFailure()
    {
        // Arrange
        var service = new SchemaValidationService(_settingsProvider);
        var source = new SchemaSourceInfo(SourceSelection.Kusto());
        var target = new SchemaSourceInfo(SourceSelection.FilePath(), FilePath: "/some/path");

        // Act
        var result = service.ValidateSettings(source, target);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("temp cluster"));
    }

    [Test]
    public void ValidateSettings_WithFilePathSourceAndTempCluster_ReturnsSuccess()
    {
        // Arrange
        _settingsProvider.SetSetting("TempCluster", "https://temp.kusto.windows.net");
        _settingsProvider.SetSetting("TempDatabase", "TempDatabase");
        var service = new SchemaValidationService(_settingsProvider);
        var source = new SchemaSourceInfo(SourceSelection.FilePath(), FilePath: "/some/path");
        var target = new SchemaSourceInfo(SourceSelection.Kusto());

        // Act
        var result = service.ValidateSettings(source, target);

        // Assert
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.ErrorMessage, Is.Null);
    }

    [Test]
    public void ValidateSettings_WithFilePathTargetAndTempCluster_ReturnsSuccess()
    {
        // Arrange
        _settingsProvider.SetSetting("TempCluster", "https://temp.kusto.windows.net");
        _settingsProvider.SetSetting("TempDatabase", "TempDatabase");
        var service = new SchemaValidationService(_settingsProvider);
        var source = new SchemaSourceInfo(SourceSelection.Kusto());
        var target = new SchemaSourceInfo(SourceSelection.FilePath(), FilePath: "/some/path");

        // Act
        var result = service.ValidateSettings(source, target);

        // Assert
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.ErrorMessage, Is.Null);
    }

    [Test]
    public void ValidateSettings_WithFilePathToFilePathAndTempCluster_ReturnsSuccess()
    {
        // Arrange
        _settingsProvider.SetSetting("TempCluster", "https://temp.kusto.windows.net");
        _settingsProvider.SetSetting("TempDatabase", "TempDatabase");
        var service = new SchemaValidationService(_settingsProvider);
        var source = new SchemaSourceInfo(SourceSelection.FilePath(), FilePath: "/source/path");
        var target = new SchemaSourceInfo(SourceSelection.FilePath(), FilePath: "/target/path");

        // Act
        var result = service.ValidateSettings(source, target);

        // Assert
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.ErrorMessage, Is.Null);
    }

    [Test]
    public void ValidateSettings_WithEmptyTempClusterAndFilePath_ReturnsFailure()
    {
        // Arrange
        _settingsProvider.SetSetting("TempCluster", "   ");
        var service = new SchemaValidationService(_settingsProvider);
        var source = new SchemaSourceInfo(SourceSelection.FilePath(), FilePath: "/some/path");
        var target = new SchemaSourceInfo(SourceSelection.Kusto());

        // Act
        var result = service.ValidateSettings(source, target);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("temp cluster"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void ValidateSettings_WithMissingTempDatabase_ReturnsFailure(string? database)
    {
        _settingsProvider.SetSetting("TempCluster", "https://temp.kusto.windows.net");
        if (database != null)
        {
            _settingsProvider.SetSetting("TempDatabase", database);
        }
        var service = new SchemaValidationService(_settingsProvider);
        var source = new SchemaSourceInfo(SourceSelection.FilePath(), FilePath: @"C:\schemas");

        var result = service.ValidateSettings(source, source);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("temp database"));
    }

    [Test]
    public void ValidateSettings_AfterSettingsChange_UsesCurrentValuesWithoutRecreatingService()
    {
        var service = new SchemaValidationService(_settingsProvider);
        var source = new SchemaSourceInfo(SourceSelection.FilePath(), FilePath: @"C:\schemas");

        Assert.That(service.ValidateSettings(source, source).IsValid, Is.False);

        _settingsProvider.SetSetting("TempCluster", "https://temp.kusto.windows.net");
        Assert.That(service.ValidateSettings(source, source).IsValid, Is.False);

        _settingsProvider.SetSetting("TempDatabase", "TempDatabase");
        Assert.That(service.ValidateSettings(source, source).IsValid, Is.True);

        _settingsProvider.SetSetting("TempCluster", "");
        Assert.That(service.ValidateSettings(source, source).IsValid, Is.False);
    }
}
