// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Kusto.Data.Common;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using SyncKusto.Abstractions;
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
        using var presenter = new MainFormPresenter(sync.Object, Mock.Of<ISchemaValidationService>(), factory.Object);

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
        using var presenter = new MainFormPresenter(sync.Object, Mock.Of<ISchemaValidationService>(), factory.Object);

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
        using var presenter = new MainFormPresenter(sync.Object, Mock.Of<ISchemaValidationService>(), factory.Object);
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
        using var presenter = new MainFormPresenter(sync.Object, Mock.Of<ISchemaValidationService>(), factory.Object);
        await presenter.CompareAsync(source, compared);

        var error = Assert.ThrowsAsync<InvalidOperationException>(() =>
            presenter.SynchronizeAsync(Array.Empty<SchemaDifference>(), current));

        Assert.That(error!.Message, Does.Contain("target location has changed"));
        factory.Verify(f => f.CreateRepository(It.IsAny<SchemaSourceInfo>()), Times.Exactly(2));
        sync.Verify(s => s.SynchronizeAsync(It.IsAny<ISchemaRepository>(), It.IsAny<ISchemaRepository>(),
            It.IsAny<IEnumerable<SchemaDifference>>(), null, default), Times.Never);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ClearComparisonAndDispose_ReleaseEachOwnedRepositoryOnce(bool shared)
    {
        var source = DisposableRepository();
        var target = shared ? source : DisposableRepository();
        using var presenter = Presenter(source, target, out _, out _);
        await presenter.CompareAsync(Source, Target);
        VerifyDisposal(source, Times.Never());
        VerifyDisposal(target, Times.Never());

        presenter.ClearComparison();
        presenter.ClearComparison();
        presenter.Dispose();

        VerifyDisposal(source, Times.Once());
        VerifyDisposal(target, Times.Once());
        Assert.ThrowsAsync<ObjectDisposedException>(() => presenter.CompareAsync(Source, Target));
    }

    [Test]
    public async Task NewComparison_ReleasesPreviousRepositoriesButRetainsNewOnes()
    {
        var oldSource = DisposableRepository();
        var oldTarget = DisposableRepository();
        var source = DisposableRepository();
        var target = DisposableRepository();
        using var presenter = Presenter(oldSource, oldTarget, out var factory, out _);
        await presenter.CompareAsync(Source, Target);
        factory.Setup(f => f.CreateRepository(Source)).Returns(() =>
        {
            VerifyDisposal(oldSource, Times.Once());
            VerifyDisposal(oldTarget, Times.Once());
            return source.Object;
        });
        factory.Setup(f => f.CreateRepository(Target)).Returns(target.Object);

        await presenter.CompareAsync(Source, Target);

        VerifyDisposal(source, Times.Never());
        VerifyDisposal(target, Times.Never());
        presenter.Dispose();
        VerifyDisposal(source, Times.Once());
        VerifyDisposal(target, Times.Once());
    }

    [Test]
    public async Task TargetConstructionFailure_ReleasesSourceAndAllowsRetry()
    {
        var source = DisposableRepository();
        var target = DisposableRepository();
        using var presenter = Presenter(source, target, out var factory, out _);
        factory.Setup(f => f.CreateRepository(Target)).Throws(new InvalidOperationException("construction failed"));

        Assert.ThrowsAsync<InvalidOperationException>(() => presenter.CompareAsync(Source, Target));
        VerifyDisposal(source, Times.Once());
        VerifyDisposal(target, Times.Never());
        Assert.ThrowsAsync<InvalidOperationException>(() =>
            presenter.SynchronizeAsync(Array.Empty<SchemaDifference>(), Target));

        var retrySource = DisposableRepository();
        factory.Setup(f => f.CreateRepository(Source)).Returns(retrySource.Object);
        factory.Setup(f => f.CreateRepository(Target)).Returns(target.Object);
        await presenter.CompareAsync(Source, Target);
        VerifyDisposal(retrySource, Times.Never());
    }

    [TestCase(false)]
    [TestCase(true)]
    public void FailedOrCanceledComparison_ReleasesBothRepositories(bool canceled)
    {
        var source = DisposableRepository();
        var target = DisposableRepository();
        using var presenter = Presenter(source, target, out _, out var sync);
        Exception failure = canceled ? new OperationCanceledException() : new SchemaLoadException("load failed");
        sync.Setup(s => s.CompareAsync(source.Object, target.Object, null, default)).ThrowsAsync(failure);

        Assert.That(Assert.CatchAsync<Exception>(() => presenter.CompareAsync(Source, Target)), Is.SameAs(failure));

        VerifyDisposal(source, Times.Once());
        VerifyDisposal(target, Times.Once());
        Assert.ThrowsAsync<InvalidOperationException>(() =>
            presenter.SynchronizeAsync(Array.Empty<SchemaDifference>(), Target));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ClearOrDisposeDuringCompare_DefersCleanupAndNeverPublishesStaleState(bool dispose)
    {
        var source = DisposableRepository();
        var target = DisposableRepository();
        using var presenter = Presenter(source, target, out _, out var sync);
        var completion = new TaskCompletionSource<ComparisonResult>();
        sync.Setup(s => s.CompareAsync(source.Object, target.Object, null, default)).Returns(completion.Task);
        var pending = presenter.CompareAsync(Source, Target);

        if (dispose)
            presenter.Dispose();
        else
            presenter.ClearComparison();
        VerifyDisposal(source, Times.Never());
        VerifyDisposal(target, Times.Never());
        completion.SetResult(EmptyComparison());
        Assert.CatchAsync<OperationCanceledException>(() => pending);

        VerifyDisposal(source, Times.Once());
        VerifyDisposal(target, Times.Once());
        Assert.CatchAsync<InvalidOperationException>(() =>
            presenter.SynchronizeAsync(Array.Empty<SchemaDifference>(), Target));
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public async Task ClearOrDisposeDuringUpdate_KeepsRepositoriesAliveUntilCompletion(bool dispose, bool kusto)
    {
        var source = DisposableRepository();
        var comparedTarget = DisposableRepository();
        var freshTarget = DisposableRepository();
        var targetInfo = kusto ? new SchemaSourceInfo(SourceSelection.Kusto(), KustoInfo: new(
            "https://target.invalid", "db", AuthenticationMode.AadFederated, "")) : Target;
        using var presenter = Presenter(source, comparedTarget, out var factory, out var sync, targetInfo);
        await presenter.CompareAsync(Source, targetInfo);
        if (kusto)
            factory.Setup(f => f.CreateRepository(targetInfo)).Returns(freshTarget.Object);
        var completion = new TaskCompletionSource<SyncResult>();
        sync.Setup(s => s.SynchronizeAsync(source.Object,
                kusto ? freshTarget.Object : comparedTarget.Object,
                It.IsAny<IEnumerable<SchemaDifference>>(), null, default)).Returns(completion.Task);
        var pending = presenter.SynchronizeAsync(Array.Empty<SchemaDifference>(), targetInfo);

        if (dispose)
            presenter.Dispose();
        else
            presenter.ClearComparison();
        VerifyDisposal(source, Times.Never());
        VerifyDisposal(comparedTarget, Times.Never());
        VerifyDisposal(freshTarget, Times.Never());
        completion.SetResult(SyncResult.Successful(0));
        await pending;

        VerifyDisposal(source, Times.Once());
        VerifyDisposal(comparedTarget, Times.Once());
        VerifyDisposal(freshTarget, kusto ? Times.Once() : Times.Never());
    }

    [Test]
    public async Task OverlappingOperation_IsRejectedWithoutInvalidatingActiveComparison()
    {
        var source = DisposableRepository();
        var target = DisposableRepository();
        using var presenter = Presenter(source, target, out _, out var sync);
        var completion = new TaskCompletionSource<ComparisonResult>();
        sync.Setup(s => s.CompareAsync(source.Object, target.Object, null, default)).Returns(completion.Task);
        var pending = presenter.CompareAsync(Source, Target);

        Assert.ThrowsAsync<InvalidOperationException>(() => presenter.CompareAsync(Source, Target));
        VerifyDisposal(source, Times.Never());
        completion.SetResult(EmptyComparison());
        await pending;
        VerifyDisposal(source, Times.Never());
    }

    [Test]
    public async Task ContainerShutdown_DisposesResolvedPresenterAndItsRepositories()
    {
        var source = DisposableRepository();
        var target = DisposableRepository();
        var factory = new Mock<SchemaRepositoryFactory>(Mock.Of<ISettingsProvider>(), Mock.Of<IKustoConnectionFactory>());
        factory.Setup(f => f.CreateRepository(Source)).Returns(source.Object);
        factory.Setup(f => f.CreateRepository(Target)).Returns(target.Object);
        var sync = new Mock<ISchemaSyncService>();
        sync.Setup(s => s.CompareAsync(source.Object, target.Object, null, default)).ReturnsAsync(EmptyComparison());
        using var provider = new ServiceCollection()
            .AddSingleton(factory.Object)
            .AddSingleton(sync.Object)
            .AddSingleton(Mock.Of<ISchemaValidationService>())
            .AddSingleton<IMainFormPresenter, MainFormPresenter>()
            .BuildServiceProvider();
        var presenter = provider.GetRequiredService<IMainFormPresenter>();
        await presenter.CompareAsync(Source, Target);

        provider.Dispose();

        VerifyDisposal(source, Times.Once());
        VerifyDisposal(target, Times.Once());
    }

    private static readonly SchemaSourceInfo Source = new(SourceSelection.FilePath(), "owned-source");
    private static readonly SchemaSourceInfo Target = new(SourceSelection.FilePath(), "owned-target");

    private static ComparisonResult EmptyComparison() => new(
        new SchemaDifferenceResult(Array.Empty<SchemaDifference>(), Array.Empty<SchemaDifference>()),
        new DatabaseSchema("source", ""), new DatabaseSchema("target", ""));

    private static Mock<ISchemaRepository> DisposableRepository()
    {
        var repository = new Mock<ISchemaRepository>(MockBehavior.Strict);
        repository.As<IDisposable>().Setup(d => d.Dispose());
        return repository;
    }

    private static void VerifyDisposal(Mock<ISchemaRepository> repository, Times times) =>
        repository.As<IDisposable>().Verify(d => d.Dispose(), times);

    private static MainFormPresenter Presenter(
        Mock<ISchemaRepository> source, Mock<ISchemaRepository> target,
        out Mock<SchemaRepositoryFactory> factory, out Mock<ISchemaSyncService> sync,
        SchemaSourceInfo? targetInfo = null)
    {
        factory = new Mock<SchemaRepositoryFactory>(Mock.Of<ISettingsProvider>(), Mock.Of<IKustoConnectionFactory>());
        factory.Setup(f => f.CreateRepository(Source)).Returns(source.Object);
        factory.Setup(f => f.CreateRepository(targetInfo ?? Target)).Returns(target.Object);
        sync = new Mock<ISchemaSyncService>(MockBehavior.Strict);
        sync.Setup(s => s.CompareAsync(It.IsAny<ISchemaRepository>(), It.IsAny<ISchemaRepository>(), null, default))
            .ReturnsAsync(EmptyComparison());
        return new MainFormPresenter(sync.Object, Mock.Of<ISchemaValidationService>(), factory.Object);
    }
}
