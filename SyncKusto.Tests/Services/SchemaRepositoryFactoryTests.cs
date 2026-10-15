// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Kusto.Data;
using Kusto.Data.Common;
using Moq;
using NUnit.Framework;
using SyncKusto.Core.Abstractions;
using SyncKusto.Core.Configuration;
using SyncKusto.Core.Models;
using SyncKusto.Core.Services;
using SyncKusto.FileSystem.Repositories;
using SyncKusto.Kusto.Models;
using SyncKusto.Kusto.Repositories;
using SyncKusto.Kusto.Services;
using SyncKusto.Services;
using System.Reflection;

namespace SyncKusto.Tests.Services;

[TestFixture]
public class SchemaRepositoryFactoryTests
{
    private InMemorySettingsProvider _provider = null!;
    private SchemaRepositoryFactory _factory = null!;
    private string _directory = null!;

    [SetUp]
    public void SetUp()
    {
        _provider = new InMemorySettingsProvider();
        _factory = new SchemaRepositoryFactory(_provider, new KustoConnectionFactory());
        _directory = Path.Combine(Path.GetTempPath(), $"SyncKustoSettings_{Guid.NewGuid()}");
        Directory.CreateDirectory(_directory);
    }

    [TearDown]
    public void TearDown()
    {
        Directory.Delete(_directory, true);
    }

    [Test]
    public void CreateRepository_AfterFirstConfiguration_UsesSavedTemporaryDatabase()
    {
        var source = new SchemaSourceInfo(SourceSelection.FilePath(), _directory);
        Assert.Throws<ArgumentException>(() => _factory.CreateRepository(source));

        ConfigureTemporaryDatabase();

        var repository = _factory.CreateRepository(source);
        Assert.That(repository, Is.TypeOf<FileSystemSchemaRepository>());
        Assert.That(Field<string>(repository, "_tempCluster"), Is.EqualTo("https://temp.invalid"));
        Assert.That(Field<string>(repository, "_tempDatabase"), Is.EqualTo("TempDatabase"));

        _provider.SetSetting("TempCluster", "https://updated.invalid");
        _provider.SetSetting("TempDatabase", "UpdatedDatabase");
        _provider.SetSetting("AADAuthority", "updated-authority");
        var updatedRepository = _factory.CreateRepository(source);

        Assert.That(Field<string>(updatedRepository, "_tempCluster"), Is.EqualTo("https://updated.invalid"));
        Assert.That(Field<string>(updatedRepository, "_tempDatabase"), Is.EqualTo("UpdatedDatabase"));
        Assert.That(Field<string>(updatedRepository, "_authority"), Is.EqualTo("updated-authority"));
        Assert.That(Field<string>(repository, "_tempDatabase"), Is.EqualTo("TempDatabase"));
    }

    [Test]
    public async Task CreateRepository_AfterFormattingChange_ExportsWithNewSettingsAndKeepsOldSnapshot()
    {
        ConfigureTemporaryDatabase();
        var source = new SchemaSourceInfo(SourceSelection.FilePath(), _directory);
        var originalRepository = _factory.CreateRepository(source);
        _provider.SetSetting("UseLegacyCslExtension", "false");
        _provider.SetSetting("CreateMergeEnabled", "true");
        _provider.SetSetting("TableFieldsOnNewLine", "true");
        _provider.SetSetting("LineEndingMode", ((int)LineEndingMode.UnixStyle).ToString());

        var updatedRepository = _factory.CreateRepository(source);
        var table = new KustoTableSchema(new TableSchema("SettingsProbe", new[]
        {
            new ColumnSchema("Id", "System.Int32"),
            new ColumnSchema("Label", "System.String")
        }, "", ""));
        await updatedRepository.SaveSchemaAsync(new[] { table });
        await originalRepository.SaveSchemaAsync(new[] { table });

        var updatedText = File.ReadAllText(Path.Combine(_directory, "SettingsProbe.kql"));
        var originalText = File.ReadAllText(Path.Combine(_directory, "SettingsProbe.csl"));
        Assert.That(updatedText, Does.StartWith(".create-merge table"));
        Assert.That(updatedText, Does.Contain("\n    "));
        Assert.That(updatedText, Does.Not.Contain("\r"));
        Assert.That(originalText, Does.StartWith(".create table"));
        Assert.That(originalText, Does.Not.Contain("\n    "));
        Assert.That(Field<SyncKustoSettings>(updatedRepository, "_settings").LineEndingMode,
            Is.EqualTo(LineEndingMode.UnixStyle));
    }

    [Test]
    public void CreateRepository_AfterConnectionSettingsChange_ForwardsCurrentCertificateLocationAndLineEndings()
    {
        KustoConnectionOptions? capturedOptions = null;
        var connectionFactory = new Mock<IKustoConnectionFactory>();
        connectionFactory.Setup(f => f.CreateConnectionString(It.IsAny<KustoConnectionOptions>()))
            .Callback<KustoConnectionOptions>(options => capturedOptions = options)
            .Returns(new KustoConnectionStringBuilder("https://test.invalid") { InitialCatalog = "TestDatabase" });
        var factory = new SchemaRepositoryFactory(_provider, connectionFactory.Object);
        _provider.SetSetting("CertificateLocation", StoreLocation.LocalMachine.ToString());
        _provider.SetSetting("LineEndingMode", ((int)LineEndingMode.UnixStyle).ToString());
        var source = new SchemaSourceInfo(SourceSelection.Kusto(), KustoInfo: new KustoConnectionInfo(
            "https://test.invalid", "TestDatabase", AuthenticationMode.AadApplicationSni, "tenant"));

        using var repository = (KustoSchemaRepository)factory.CreateRepository(source);

        Assert.That(capturedOptions, Is.Not.Null);
        Assert.That(capturedOptions!.CertificateLocation, Is.EqualTo(StoreLocation.LocalMachine));
        var engine = Field<QueryEngine>(repository, "_queryEngine");
        Assert.That(Field<LineEndingMode>(engine, "_lineEndingMode"), Is.EqualTo(LineEndingMode.UnixStyle));
    }

    private void ConfigureTemporaryDatabase()
    {
        _provider.SetSetting("TempCluster", "https://temp.invalid");
        _provider.SetSetting("TempDatabase", "TempDatabase");
    }

    private static T Field<T>(object instance, string name) =>
        (T)instance.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(instance)!;
}
