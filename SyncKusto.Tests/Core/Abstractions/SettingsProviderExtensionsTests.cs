// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using NUnit.Framework;
using SyncKusto.Core.Abstractions;
using SyncKusto.Core.Models;
using SyncKusto.Core.Services;

namespace SyncKusto.Tests.Core.Abstractions;

[TestFixture]
public class SettingsProviderExtensionsTests
{
    private InMemorySettingsProvider _provider = null!;

    [SetUp]
    public void SetUp()
    {
        _provider = new InMemorySettingsProvider();
    }

    [Test]
    public void GetSyncKustoSettings_WithDefaults_ReturnsDefaultSettings()
    {
        // Act
        var settings = _provider.GetSyncKustoSettings();

        // Assert
        Assert.That(settings, Is.Not.Null);
        Assert.That(settings.TempCluster, Is.EqualTo(string.Empty));
        Assert.That(settings.TempDatabase, Is.EqualTo(string.Empty));
        Assert.That(settings.KustoObjectDropWarning, Is.True);
    }

    [Test]
    public void GetSyncKustoSettings_WithConfiguredSettings_ReturnsConfiguredValues()
    {
        // Arrange
        _provider.SetSetting("TempCluster", "https://test.kusto.windows.net");
        _provider.SetSetting("TempDatabase", "TestDB");
        _provider.SetSetting("AADAuthority", "https://login.microsoftonline.com");
        _provider.SetSetting("KustoObjectDropWarning", "false");

        // Act
        var settings = _provider.GetSyncKustoSettings();

        // Assert
        Assert.That(settings.TempCluster, Is.EqualTo("https://test.kusto.windows.net"));
        Assert.That(settings.TempDatabase, Is.EqualTo("TestDB"));
        Assert.That(settings.AADAuthority, Is.EqualTo("https://login.microsoftonline.com"));
        Assert.That(settings.KustoObjectDropWarning, Is.False);
    }

    [Test]
    public void GetSyncKustoSettings_WithTableFieldsSettings_ReturnsCorrectValues()
    {
        // Arrange
        _provider.SetSetting("TableFieldsOnNewLine", "true");
        _provider.SetSetting("CreateMergeEnabled", "true");

        // Act
        var settings = _provider.GetSyncKustoSettings();

        // Assert
        Assert.That(settings.TableFieldsOnNewLine, Is.True);
        Assert.That(settings.CreateMergeEnabled, Is.True);
    }

    [Test]
    public void GetSyncKustoSettings_WithFileExtensionSettings_ReturnsCorrectExtension()
    {
        // Arrange
        _provider.SetSetting("UseLegacyCslExtension", "false");

        // Act
        var settings = _provider.GetSyncKustoSettings();

        // Assert
        Assert.That(settings.FileExtension, Is.EqualTo("kql"));
    }

    [Test]
    public void GetSyncKustoSettings_WithLineEndingMode_ReturnsCorrectMode()
    {
        // Arrange
        _provider.SetSetting("LineEndingMode", "1"); // Windows style

        // Act
        var settings = _provider.GetSyncKustoSettings();

        // Assert
        Assert.That(settings.LineEndingMode, Is.EqualTo(LineEndingMode.WindowsStyle));
    }

    [Test]
    public void GetSyncKustoSettings_WithCertificateLocation_ReturnsCorrectLocation()
    {
        // Arrange
        _provider.SetSetting("CertificateLocation", "LocalMachine");

        // Act
        var settings = _provider.GetSyncKustoSettings();

        // Assert
        Assert.That(settings.CertificateLocation, Is.EqualTo(StoreLocation.LocalMachine));
    }

    [Test]
    public void GetSyncKustoSettings_CalledMultipleTimes_ReturnsConsistentResults()
    {
        // Arrange
        _provider.SetSetting("TempCluster", "https://test.kusto.windows.net");

        // Act
        var settings1 = _provider.GetSyncKustoSettings();
        var settings2 = _provider.GetSyncKustoSettings();

        // Assert
        Assert.That(settings1.TempCluster, Is.EqualTo(settings2.TempCluster));
        Assert.That(settings1.KustoObjectDropWarning, Is.EqualTo(settings2.KustoObjectDropWarning));
    }

    [Test]
    public void GetSyncKustoSettings_AfterSettingChange_ReflectsNewValues()
    {
        // Arrange
        _provider.SetSetting("TempCluster", "https://original.kusto.windows.net");
        var settings1 = _provider.GetSyncKustoSettings();

        // Act
        _provider.SetSetting("TempCluster", "https://updated.kusto.windows.net");
        var settings2 = _provider.GetSyncKustoSettings();

        // Assert
        Assert.That(settings1.TempCluster, Is.EqualTo("https://original.kusto.windows.net"));
        Assert.That(settings2.TempCluster, Is.EqualTo("https://updated.kusto.windows.net"));
    }
}
