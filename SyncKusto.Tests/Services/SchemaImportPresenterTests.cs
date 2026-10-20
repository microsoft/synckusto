// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Kusto.Data.Common;
using Moq;
using NUnit.Framework;
using SyncKusto.Core.Abstractions;
using SyncKusto.Core.Exceptions;
using SyncKusto.Core.Models;
using SyncKusto.Services;

namespace SyncKusto.Tests.Services;

[TestFixture]
[Category("SchemaImport")]
public class SchemaImportPresenterTests
{
    [TestCase(false)]
    [TestCase(true)]
    public async Task FailureInvalidatesPreviousSynchronizationAndCorrectionAllowsRetry(bool withSkippedFiles)
    {
        var sourceInfo = new SchemaSourceInfo(SourceSelection.FilePath(), "source");
        var targetInfo = new SchemaSourceInfo(SourceSelection.FilePath(), "target");
        var source = new Mock<ISchemaRepository>(MockBehavior.Strict);
        var target = new Mock<ISchemaRepository>(MockBehavior.Strict);
        var factory = new Mock<SchemaRepositoryFactory>(
            Mock.Of<ISettingsProvider>(), Mock.Of<IKustoConnectionFactory>());
        factory.Setup(f => f.CreateRepository(sourceInfo)).Returns(source.Object);
        factory.Setup(f => f.CreateRepository(targetInfo)).Returns(target.Object);
        var sync = new Mock<ISchemaSyncService>(MockBehavior.Strict);
        var result = new ComparisonResult(
            new SchemaDifferenceResult(Array.Empty<SchemaDifference>(), Array.Empty<SchemaDifference>()),
            new DatabaseSchema("source", ""), new DatabaseSchema("target", ""))
        {
            SkippedFiles = withSkippedFiles
                ? new[] { new SkippedSchemaFile("Source", "unsupported.kql", "is unsupported", "Use a supported definition.") }
                : Array.Empty<SkippedSchemaFile>()
        };
        var error = new SchemaInputException("Target", @"Functions\f.kql", "must contain one definition",
            "Remove other commands and compare again.", new InvalidOperationException("private_payload"));
        sync.SetupSequence(s => s.CompareAsync(source.Object, target.Object, null, default))
            .ReturnsAsync(result).ThrowsAsync(error).ReturnsAsync(result);
        sync.Setup(s => s.SynchronizeAsync(source.Object, target.Object,
            It.IsAny<IEnumerable<SchemaDifference>>(), null, default)).ReturnsAsync(SyncResult.Successful(0));
        var presenter = new MainFormPresenter(sync.Object, Mock.Of<ISchemaValidationService>(), factory.Object);

        Assert.That(await presenter.CompareAsync(sourceInfo, targetInfo), Is.SameAs(result));
        Assert.ThrowsAsync<SchemaInputException>(() => presenter.CompareAsync(sourceInfo, targetInfo));
        Assert.ThrowsAsync<InvalidOperationException>(() => presenter.SynchronizeAsync(Array.Empty<SchemaDifference>(), targetInfo));
        sync.Verify(s => s.SynchronizeAsync(source.Object, target.Object,
            It.IsAny<IEnumerable<SchemaDifference>>(), null, default), Times.Never);
        Assert.That(await presenter.CompareAsync(sourceInfo, targetInfo), Is.SameAs(result));
        await presenter.SynchronizeAsync(Array.Empty<SchemaDifference>(), targetInfo);
        source.VerifyNoOtherCalls();
        target.VerifyNoOtherCalls();
        sync.Verify(s => s.SynchronizeAsync(source.Object, target.Object,
            It.IsAny<IEnumerable<SchemaDifference>>(), null, default), Times.Once);
    }

    [TestCase(AuthenticationMode.AadApplication, false)]
    [TestCase(AuthenticationMode.AadApplicationSni, false)]
    [TestCase(AuthenticationMode.AadFederated, false)]
    [TestCase(AuthenticationMode.AadApplicationSni, true)]
    public async Task Update_UsesFreshCurrentTargetAndComparedSourceWithoutReloading_AndDisposesOnFailure(
        AuthenticationMode mode, bool fail)
    {
        var sourceInfo = new SchemaSourceInfo(SourceSelection.FilePath(), "source");
        var comparedInfo = new SchemaSourceInfo(SourceSelection.Kusto(), KustoInfo: new(
            "https://target.invalid", "db", AuthenticationMode.AadFederated, ""));
        var current = comparedInfo with { KustoInfo = comparedInfo.KustoInfo! with
        {
            AuthMode = mode, Authority = "current-tenant", AppId = "current-app", AppKey = "offline-key",
            CertificateThumbprint = "offline-thumbprint", CertificateLocation = StoreLocation.LocalMachine
        }};
        var source = new Mock<ISchemaRepository>(MockBehavior.Strict);
        var comparedTarget = new Mock<ISchemaRepository>(MockBehavior.Strict);
        var currentTarget = new Mock<ISchemaRepository>(MockBehavior.Strict);
        bool disposed = false;
        currentTarget.As<IDisposable>().Setup(d => d.Dispose()).Callback(() => disposed = true);
        var factory = new Mock<SchemaRepositoryFactory>(Mock.Of<ISettingsProvider>(), Mock.Of<IKustoConnectionFactory>());
        factory.Setup(f => f.CreateRepository(sourceInfo)).Returns(source.Object);
        factory.SetupSequence(f => f.CreateRepository(It.Is<SchemaSourceInfo>(s => s.SourceType == SourceSelection.Kusto())))
            .Returns(comparedTarget.Object).Returns(currentTarget.Object);
        var sync = new Mock<ISchemaSyncService>(MockBehavior.Strict);
        var comparison = new ComparisonResult(
            new SchemaDifferenceResult(Array.Empty<SchemaDifference>(), Array.Empty<SchemaDifference>()),
            new DatabaseSchema("source", ""), new DatabaseSchema("target", ""));
        sync.Setup(s => s.CompareAsync(source.Object, comparedTarget.Object, null, default)).ReturnsAsync(comparison);
        var differences = new[] { new TableSchemaDifference(new OnlyInSource(), new Core.TestKustoSchema("SelectedTable")) };
        var setup = sync.Setup(s => s.SynchronizeAsync(source.Object, currentTarget.Object, differences, null, default))
            .Callback(() => Assert.That(disposed, Is.False));
        if (fail)
            setup.ThrowsAsync(new SchemaSyncException("offline failure"));
        else
            setup.ReturnsAsync(SyncResult.Successful(1));
        var presenter = new MainFormPresenter(sync.Object, Mock.Of<ISchemaValidationService>(), factory.Object);

        await presenter.CompareAsync(sourceInfo, comparedInfo);
        if (fail)
            Assert.ThrowsAsync<SchemaSyncException>(() => presenter.SynchronizeAsync(differences, current));
        else
            Assert.That((await presenter.SynchronizeAsync(differences, current)).Success, Is.True);

        factory.Verify(f => f.CreateRepository(current), Times.Once);
        factory.Verify(f => f.CreateRepository(sourceInfo), Times.Once);
        sync.Verify(s => s.CompareAsync(source.Object, comparedTarget.Object, null, default), Times.Once);
        sync.Verify(s => s.SynchronizeAsync(source.Object, currentTarget.Object, differences, null, default), Times.Once);
        currentTarget.As<IDisposable>().Verify(d => d.Dispose(), Times.Once);
        source.VerifyNoOtherCalls();
        comparedTarget.VerifyNoOtherCalls();
        Assert.That(disposed, Is.True);
    }

    [TestCase("authority")]
    [TestCase("appId")]
    [TestCase("key")]
    [TestCase("certificate")]
    [TestCase("mode")]
    public async Task Update_InvalidCredentialsNeverCreateClientOrWrite_AndAllowCorrectionWithoutRecompare(string missing)
    {
        var sourceInfo = new SchemaSourceInfo(SourceSelection.FilePath(), "source");
        var targetInfo = new SchemaSourceInfo(SourceSelection.Kusto(), KustoInfo: new(
            "https://target.invalid", "db", AuthenticationMode.AadApplication, "tenant", "app", "offline-key"));
        var invalid = targetInfo with { KustoInfo = missing switch
        {
            "authority" => targetInfo.KustoInfo! with { Authority = "" },
            "appId" => targetInfo.KustoInfo! with { AppId = "" },
            "key" => targetInfo.KustoInfo! with { AppKey = "" },
            "certificate" => targetInfo.KustoInfo! with { AuthMode = AuthenticationMode.AadApplicationSni, CertificateThumbprint = "" },
            _ => targetInfo.KustoInfo! with { AuthMode = (AuthenticationMode)999 }
        }};
        var factory = new Mock<SchemaRepositoryFactory>(Mock.Of<ISettingsProvider>(), Mock.Of<IKustoConnectionFactory>());
        factory.Setup(f => f.CreateRepository(It.IsAny<SchemaSourceInfo>())).Returns(Mock.Of<ISchemaRepository>());
        var sync = new Mock<ISchemaSyncService>();
        sync.Setup(s => s.SynchronizeAsync(It.IsAny<ISchemaRepository>(), It.IsAny<ISchemaRepository>(),
            It.IsAny<IEnumerable<SchemaDifference>>(), null, default)).ReturnsAsync(SyncResult.Successful(0));
        var presenter = new MainFormPresenter(sync.Object, Mock.Of<ISchemaValidationService>(), factory.Object);
        await presenter.CompareAsync(sourceInfo, targetInfo);

        Assert.ThrowsAsync<KustoSettingsException>(() => presenter.SynchronizeAsync(Array.Empty<SchemaDifference>(), invalid));

        factory.Verify(f => f.CreateRepository(It.IsAny<SchemaSourceInfo>()), Times.Exactly(2));
        sync.Verify(s => s.SynchronizeAsync(It.IsAny<ISchemaRepository>(), It.IsAny<ISchemaRepository>(),
            It.IsAny<IEnumerable<SchemaDifference>>(), null, default), Times.Never);
        await presenter.SynchronizeAsync(Array.Empty<SchemaDifference>(), targetInfo);
        sync.Verify(s => s.CompareAsync(It.IsAny<ISchemaRepository>(), It.IsAny<ISchemaRepository>(), null, default), Times.Once);
        sync.Verify(s => s.SynchronizeAsync(It.IsAny<ISchemaRepository>(), It.IsAny<ISchemaRepository>(),
            It.IsAny<IEnumerable<SchemaDifference>>(), null, default), Times.Once);
    }

    [TestCase("type")]
    [TestCase("cluster")]
    [TestCase("database")]
    [TestCase("folder")]
    public async Task Update_RejectsChangedTargetIdentityEvenIfCallerBypassesPickerInvalidation(string change)
    {
        var source = new SchemaSourceInfo(SourceSelection.FilePath(), "source");
        var compared = change == "folder" ? new SchemaSourceInfo(SourceSelection.FilePath(), "target") :
            new SchemaSourceInfo(SourceSelection.Kusto(), KustoInfo: new(
                "https://target.invalid", "db", AuthenticationMode.AadFederated, ""));
        var current = change switch
        {
            "type" => new SchemaSourceInfo(SourceSelection.FilePath(), "target"),
            "cluster" => compared with { KustoInfo = compared.KustoInfo! with { Cluster = "https://other.invalid" } },
            "database" => compared with { KustoInfo = compared.KustoInfo! with { Database = "other" } },
            _ => compared with { FilePath = "other" }
        };
        var factory = new Mock<SchemaRepositoryFactory>(Mock.Of<ISettingsProvider>(), Mock.Of<IKustoConnectionFactory>());
        factory.Setup(f => f.CreateRepository(It.IsAny<SchemaSourceInfo>())).Returns(Mock.Of<ISchemaRepository>());
        var sync = new Mock<ISchemaSyncService>();
        var presenter = new MainFormPresenter(sync.Object, Mock.Of<ISchemaValidationService>(), factory.Object);
        await presenter.CompareAsync(source, compared);

        var error = Assert.ThrowsAsync<InvalidOperationException>(() =>
            presenter.SynchronizeAsync(Array.Empty<SchemaDifference>(), current));

        Assert.That(error!.Message, Does.Contain("target location has changed"));
        factory.Verify(f => f.CreateRepository(It.IsAny<SchemaSourceInfo>()), Times.Exactly(2));
        sync.Verify(s => s.SynchronizeAsync(It.IsAny<ISchemaRepository>(), It.IsAny<ISchemaRepository>(),
            It.IsAny<IEnumerable<SchemaDifference>>(), null, default), Times.Never);
    }
}
