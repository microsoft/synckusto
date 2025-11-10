// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using NUnit.Framework;
using SyncKusto.Core.Abstractions;
using SyncKusto.Core.Configuration;
using SyncKusto.Core.Models;
using SyncKusto.Core.Services;

namespace SyncKusto.Tests.Configuration;

[TestFixture]
public class SyncKustoSettingsFactoryTests
{
    private InMemorySettingsProvider _provider = null!;

    [SetUp]
    public void SetUp()
    {
        _provider = new InMemorySettingsProvider();
    }

    [Test]
    public void CreateFromProvider_WithAllSettings_ReturnsCompleteConfiguration()
    {
        // Arrange
        _provider.SetSetting("TempCluster", "https://test.kusto.windows.net");
        _provider.SetSetting("TempDatabase", "TestDB");
        _provider.SetSetting("AADAuthority", "https://login.microsoftonline.com");
        _provider.SetSetting("KustoObjectDropWarning", "false");
        _provider.SetSetting("TableFieldsOnNewLine", "true");
        _provider.SetSetting("CreateMergeEnabled", "true");
        _provider.SetSetting("UseLegacyCslExtension", "false");
        _provider.SetSetting("LineEndingMode", "1"); // Windows
        _provider.SetSetting("CertificateLocation", "LocalMachine");

        // Act
        var settings = SyncKustoSettingsFactory.CreateFromProvider(_provider);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(settings.TempCluster, Is.EqualTo("https://test.kusto.windows.net"));
            Assert.That(settings.TempDatabase, Is.EqualTo("TestDB"));
            Assert.That(settings.AADAuthority, Is.EqualTo("https://login.microsoftonline.com"));
            Assert.That(settings.KustoObjectDropWarning, Is.False);
            Assert.That(settings.TableFieldsOnNewLine, Is.True);
            Assert.That(settings.CreateMergeEnabled, Is.True);
            Assert.That(settings.UseLegacyCslExtension, Is.False);
            Assert.That(settings.LineEndingMode, Is.EqualTo(LineEndingMode.WindowsStyle));
            Assert.That(settings.CertificateLocation, Is.EqualTo(StoreLocation.LocalMachine));
            Assert.That(settings.FileExtension, Is.EqualTo("kql"));
        });
    }

    [Test]
    public void CreateFromProvider_WithDefaults_ReturnsDefaultConfiguration()
    {
        // Act
        var settings = SyncKustoSettingsFactory.CreateFromProvider(_provider);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(settings.TempCluster, Is.EqualTo(string.Empty));
            Assert.That(settings.TempDatabase, Is.EqualTo(string.Empty));
            Assert.That(settings.AADAuthority, Is.Null);
            Assert.That(settings.KustoObjectDropWarning, Is.True);
            Assert.That(settings.TableFieldsOnNewLine, Is.False);
            Assert.That(settings.CreateMergeEnabled, Is.False);
            Assert.That(settings.UseLegacyCslExtension, Is.True);
            Assert.That(settings.LineEndingMode, Is.EqualTo(LineEndingMode.LeaveAsIs));
            Assert.That(settings.CertificateLocation, Is.EqualTo(StoreLocation.CurrentUser));
            Assert.That(settings.FileExtension, Is.EqualTo("csl"));
        });
    }

    [Test]
    public void CreateFromProvider_WithInvalidBooleans_UsesDefaults()
    {
        // Arrange
        _provider.SetSetting("TempCluster", "test");
        _provider.SetSetting("TempDatabase", "test");
        _provider.SetSetting("KustoObjectDropWarning", "invalid");
        _provider.SetSetting("TableFieldsOnNewLine", "not-a-bool");
        _provider.SetSetting("CreateMergeEnabled", "xyz");

        // Act
        var settings = SyncKustoSettingsFactory.CreateFromProvider(_provider);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(settings.KustoObjectDropWarning, Is.True); // Default
            Assert.That(settings.TableFieldsOnNewLine, Is.False); // Default
            Assert.That(settings.CreateMergeEnabled, Is.False); // Default
        });
    }

    [Test]
    public void CreateFromProvider_WithInvalidLineEndingMode_UsesDefault()
    {
        // Arrange
        _provider.SetSetting("TempCluster", "test");
        _provider.SetSetting("TempDatabase", "test");
        _provider.SetSetting("LineEndingMode", "999");

        // Act
        var settings = SyncKustoSettingsFactory.CreateFromProvider(_provider);

        // Assert
        Assert.That(settings.LineEndingMode, Is.EqualTo(LineEndingMode.LeaveAsIs));
    }

    [Test]
    public void CreateFromProvider_WithInvalidStoreLocation_UsesDefault()
    {
        // Arrange
        _provider.SetSetting("TempCluster", "test");
        _provider.SetSetting("TempDatabase", "test");
        _provider.SetSetting("CertificateLocation", "InvalidLocation");

        // Act
        var settings = SyncKustoSettingsFactory.CreateFromProvider(_provider);

        // Assert
        Assert.That(settings.CertificateLocation, Is.EqualTo(StoreLocation.CurrentUser));
    }

    [Test]
    public void CreateFromProvider_FileExtension_ReflectsUseLegacyCslExtension()
    {
        // Arrange
        _provider.SetSetting("TempCluster", "test");
        _provider.SetSetting("TempDatabase", "test");

        // Act - Test with legacy enabled (default)
        var settingsLegacy = SyncKustoSettingsFactory.CreateFromProvider(_provider);

        // Change to new extension
        _provider.SetSetting("UseLegacyCslExtension", "false");
        var settingsNew = SyncKustoSettingsFactory.CreateFromProvider(_provider);

        // Assert
        Assert.That(settingsLegacy.FileExtension, Is.EqualTo("csl"));
        Assert.That(settingsNew.FileExtension, Is.EqualTo("kql"));
    }

    [Test]
    public void CreateFromProvider_WithNullProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            SyncKustoSettingsFactory.CreateFromProvider(null!));
    }

    [Test]
    public void GetSyncKustoSettings_ExtensionMethod_Works()
    {
        // Arrange
        _provider.SetSetting("TempCluster", "https://test.kusto.windows.net");
        _provider.SetSetting("TempDatabase", "TestDB");

        // Act
        var settings = _provider.GetSyncKustoSettings();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(settings.TempCluster, Is.EqualTo("https://test.kusto.windows.net"));
            Assert.That(settings.TempDatabase, Is.EqualTo("TestDB"));
        });
    }
}
