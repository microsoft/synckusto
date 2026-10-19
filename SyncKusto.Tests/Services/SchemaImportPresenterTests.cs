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
        Assert.ThrowsAsync<InvalidOperationException>(() => presenter.SynchronizeAsync(Array.Empty<SchemaDifference>()));
        sync.Verify(s => s.SynchronizeAsync(source.Object, target.Object,
            It.IsAny<IEnumerable<SchemaDifference>>(), null, default), Times.Never);
        Assert.That(await presenter.CompareAsync(sourceInfo, targetInfo), Is.SameAs(result));
        await presenter.SynchronizeAsync(Array.Empty<SchemaDifference>());
        source.VerifyNoOtherCalls();
        target.VerifyNoOtherCalls();
        sync.Verify(s => s.SynchronizeAsync(source.Object, target.Object,
            It.IsAny<IEnumerable<SchemaDifference>>(), null, default), Times.Once);
    }
}
