// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using NUnit.Framework;
using SyncKusto.Core.Configuration;
using SyncKusto.Core.Models;
using SyncKusto.Core.Services;
using System;

namespace SyncKusto.Tests.Core.Services;

[TestFixture]
public class SchemaValidationServiceTests
{
    private InMemorySettingsProvider _settingsProvider = null!;
    private SyncKustoSettings _settings = null!;

    [SetUp]
    public void SetUp()
    {
        _settingsProvider = new InMemorySettingsProvider();
    }

    [Test]
    public void Constructor_WithNullSettings_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SchemaValidationService(null!));
    }

    [Test]
    public void ValidateSettings_WithNullSource_ThrowsArgumentNullException()
    {
        // Arrange
        _settings = SyncKustoSettingsFactory.CreateFromProvider(_settingsProvider);
        var service = new SchemaValidationService(_settings);
        var target = new SchemaSourceInfo(SourceSelection.Kusto());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.ValidateSettings(null!, target));
    }

    [Test]
    public void ValidateSettings_WithNullTarget_ThrowsArgumentNullException()
    {
        // Arrange
        _settings = SyncKustoSettingsFactory.CreateFromProvider(_settingsProvider);
        var service = new SchemaValidationService(_settings);
        var source = new SchemaSourceInfo(SourceSelection.Kusto());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.ValidateSettings(source, null!));
    }

    [Test]
    public void ValidateSettings_WithKustoToKusto_ReturnsSuccess()
    {
        // Arrange
        _settings = SyncKustoSettingsFactory.CreateFromProvider(_settingsProvider);
        var service = new SchemaValidationService(_settings);
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
        _settings = SyncKustoSettingsFactory.CreateFromProvider(_settingsProvider);
        var service = new SchemaValidationService(_settings);
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
        _settings = SyncKustoSettingsFactory.CreateFromProvider(_settingsProvider);
        var service = new SchemaValidationService(_settings);
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
        _settings = SyncKustoSettingsFactory.CreateFromProvider(_settingsProvider);
        var service = new SchemaValidationService(_settings);
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
        _settings = SyncKustoSettingsFactory.CreateFromProvider(_settingsProvider);
        var service = new SchemaValidationService(_settings);
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
        _settings = SyncKustoSettingsFactory.CreateFromProvider(_settingsProvider);
        var service = new SchemaValidationService(_settings);
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
        _settings = SyncKustoSettingsFactory.CreateFromProvider(_settingsProvider);
        var service = new SchemaValidationService(_settings);
        var source = new SchemaSourceInfo(SourceSelection.FilePath(), FilePath: "/some/path");
        var target = new SchemaSourceInfo(SourceSelection.Kusto());

        // Act
        var result = service.ValidateSettings(source, target);

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("temp cluster"));
    }
}
