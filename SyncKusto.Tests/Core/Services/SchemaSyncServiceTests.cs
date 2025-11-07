// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Kusto.Data.Common;
using NUnit.Framework;
using SyncKusto.Core.Abstractions;
using SyncKusto.Core.Exceptions;
using SyncKusto.Core.Models;
using SyncKusto.Core.Services;
using SyncKusto.Tests.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SyncKusto.Tests.Core.Services;

[TestFixture]
public class SchemaSyncServiceTests
{
    private TestSchemaComparisonService _comparisonService = null!;
    private SchemaSyncService _schemaSyncService = null!;

    [SetUp]
    public void SetUp()
    {
        _comparisonService = new TestSchemaComparisonService();
        _schemaSyncService = new SchemaSyncService(_comparisonService);
    }

    [Test]
    public void Constructor_WithNullComparisonService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SchemaSyncService(null!));
    }

    [Test]
    public async Task CompareAsync_WithValidRepositories_ReturnsComparisonResult()
    {
        // Arrange
        var sourceRepo = new TestSchemaRepository();
        var targetRepo = new TestSchemaRepository();

        // Act
        var result = await _schemaSyncService.CompareAsync(sourceRepo, targetRepo);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(_comparisonService.CompareSchemasCalled, Is.True);
    }

    [Test]
    public void CompareAsync_WithNullSource_ThrowsArgumentNullException()
    {
        // Arrange
        var targetRepo = new TestSchemaRepository();

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _schemaSyncService.CompareAsync(null!, targetRepo));
    }

    [Test]
    public void CompareAsync_WithNullTarget_ThrowsArgumentNullException()
    {
        // Arrange
        var sourceRepo = new TestSchemaRepository();

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _schemaSyncService.CompareAsync(sourceRepo, null!));
    }

    [Test]
    public async Task CompareAsync_ReportsProgressStages()
    {
        // Arrange
        var sourceRepo = new TestSchemaRepository();
        var targetRepo = new TestSchemaRepository();
        var progressReports = new List<SyncProgress>();
        
        // Use a custom progress implementation that captures synchronously
        var progress = new SynchronousProgress<SyncProgress>(p => progressReports.Add(p));

        // Act
        await _schemaSyncService.CompareAsync(sourceRepo, targetRepo, progress);

        // Assert
        Assert.That(progressReports.Count, Is.GreaterThan(0));
        Assert.That(progressReports.Any(p => p.Stage == SyncProgressStage.LoadingSourceSchema), Is.True);
        Assert.That(progressReports.Any(p => p.Stage == SyncProgressStage.LoadingTargetSchema), Is.True);
        Assert.That(progressReports.Any(p => p.Stage == SyncProgressStage.ComparingSchemas), Is.True);
        Assert.That(progressReports.Any(p => p.Stage == SyncProgressStage.Complete), Is.True);
    }

    [Test]
    public async Task CompareAsync_WithCancellationToken_PassesToRepository()
    {
        // Arrange
        var sourceRepo = new TestSchemaRepository();
        var targetRepo = new TestSchemaRepository();
        var cts = new CancellationTokenSource();

        // Act
        await _schemaSyncService.CompareAsync(sourceRepo, targetRepo, cancellationToken: cts.Token);

        // Assert
        Assert.That(sourceRepo.CancellationTokenReceived, Is.EqualTo(cts.Token));
        Assert.That(targetRepo.CancellationTokenReceived, Is.EqualTo(cts.Token));
    }

    [Test]
    public void CompareAsync_WhenSourceThrowsSchemaLoadException_Rethrows()
    {
        // Arrange
        var sourceRepo = new TestSchemaRepository { ThrowSchemaLoadException = true };
        var targetRepo = new TestSchemaRepository();

        // Act & Assert
        Assert.ThrowsAsync<SchemaLoadException>(async () =>
            await _schemaSyncService.CompareAsync(sourceRepo, targetRepo));
    }

    [Test]
    public void CompareAsync_WhenSourceThrowsGenericException_WrapsInSchemaLoadException()
    {
        // Arrange
        var sourceRepo = new TestSchemaRepository { ThrowGenericException = true };
        var targetRepo = new TestSchemaRepository();

        // Act & Assert
        var ex = Assert.ThrowsAsync<SchemaLoadException>(async () =>
            await _schemaSyncService.CompareAsync(sourceRepo, targetRepo));
        Assert.That(ex!.InnerException, Is.Not.Null);
    }

    [Test]
    public async Task SynchronizeAsync_WithValidDifferences_ReturnsSyncResult()
    {
        // Arrange
        var sourceRepo = new TestSchemaRepository();
        var targetRepo = new TestSchemaRepository();
        var differences = new List<SchemaDifference>
        {
            new TableSchemaDifference(new OnlyInSource(), new TestKustoSchema("Table1"))
        };

        // Act
        var result = await _schemaSyncService.SynchronizeAsync(sourceRepo, targetRepo, differences);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Success, Is.True);
        Assert.That(result.ItemsSynchronized, Is.EqualTo(1));
    }

    [Test]
    public void SynchronizeAsync_WithNullSource_ThrowsArgumentNullException()
    {
        // Arrange
        var targetRepo = new TestSchemaRepository();
        var differences = new List<SchemaDifference>();

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _schemaSyncService.SynchronizeAsync(null!, targetRepo, differences));
    }

    [Test]
    public void SynchronizeAsync_WithNullTarget_ThrowsArgumentNullException()
    {
        // Arrange
        var sourceRepo = new TestSchemaRepository();
        var differences = new List<SchemaDifference>();

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _schemaSyncService.SynchronizeAsync(sourceRepo, null!, differences));
    }

    [Test]
    public void SynchronizeAsync_WithNullDifferences_ThrowsArgumentNullException()
    {
        // Arrange
        var sourceRepo = new TestSchemaRepository();
        var targetRepo = new TestSchemaRepository();

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _schemaSyncService.SynchronizeAsync(sourceRepo, targetRepo, null!));
    }

    [Test]
    public async Task SynchronizeAsync_WithEmptyDifferences_ReturnsSuccessWithZeroItems()
    {
        // Arrange
        var sourceRepo = new TestSchemaRepository();
        var targetRepo = new TestSchemaRepository();
        var differences = new List<SchemaDifference>();

        // Act
        var result = await _schemaSyncService.SynchronizeAsync(sourceRepo, targetRepo, differences);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.ItemsSynchronized, Is.EqualTo(0));
    }

    [Test]
    public async Task SynchronizeAsync_WithOnlyInSourceDifferences_CallsSaveSchema()
    {
        // Arrange
        var sourceRepo = new TestSchemaRepository();
        var targetRepo = new TestSchemaRepository();
        var differences = new List<SchemaDifference>
        {
            new TableSchemaDifference(new OnlyInSource(), new TestKustoSchema("Table1")),
            new FunctionSchemaDifference(new OnlyInSource(), new TestKustoSchema("Function1"))
        };

        // Act
        var result = await _schemaSyncService.SynchronizeAsync(sourceRepo, targetRepo, differences);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.ItemsSynchronized, Is.EqualTo(2));
        Assert.That(targetRepo.SavedSchemas.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task SynchronizeAsync_WithOnlyInTargetDifferences_CallsDeleteSchema()
    {
        // Arrange
        var sourceRepo = new TestSchemaRepository();
        var targetRepo = new TestSchemaRepository();
        var differences = new List<SchemaDifference>
        {
            new TableSchemaDifference(new OnlyInTarget(), new TestKustoSchema("Table1"))
        };

        // Act
        var result = await _schemaSyncService.SynchronizeAsync(sourceRepo, targetRepo, differences);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.ItemsSynchronized, Is.EqualTo(1));
        Assert.That(targetRepo.DeletedSchemas.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task SynchronizeAsync_WithModifiedDifferences_CallsSaveSchema()
    {
        // Arrange
        var sourceRepo = new TestSchemaRepository();
        var targetRepo = new TestSchemaRepository();
        var differences = new List<SchemaDifference>
        {
            new TableSchemaDifference(new Modified(), new TestKustoSchema("Table1"))
        };

        // Act
        var result = await _schemaSyncService.SynchronizeAsync(sourceRepo, targetRepo, differences);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(targetRepo.SavedSchemas.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task SynchronizeAsync_ReportsProgressDuringSynchronization()
    {
        // Arrange
        var sourceRepo = new TestSchemaRepository();
        var targetRepo = new TestSchemaRepository();
        var differences = new List<SchemaDifference>
        {
            new TableSchemaDifference(new OnlyInSource(), new TestKustoSchema("Table1"))
        };
        var progressReports = new List<SyncProgress>();
        
        // Use a custom progress implementation that captures synchronously
        var progress = new SynchronousProgress<SyncProgress>(p => progressReports.Add(p));

        // Act
        await _schemaSyncService.SynchronizeAsync(sourceRepo, targetRepo, differences, progress);

        // Assert
        Assert.That(progressReports.Count, Is.GreaterThan(0));
        Assert.That(progressReports.Any(p => p.Stage == SyncProgressStage.SynchronizingSchemas), Is.True);
        Assert.That(progressReports.Any(p => p.Stage == SyncProgressStage.Complete), Is.True);
    }

    [Test]
    public async Task SynchronizeAsync_WhenSaveFails_ReturnsFailedResult()
    {
        // Arrange
        var sourceRepo = new TestSchemaRepository();
        var targetRepo = new TestSchemaRepository { ThrowOnSave = true };
        var differences = new List<SchemaDifference>
        {
            new TableSchemaDifference(new OnlyInSource(), new TestKustoSchema("Table1"))
        };

        // Act
        var result = await _schemaSyncService.SynchronizeAsync(sourceRepo, targetRepo, differences);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Errors.Count, Is.GreaterThan(0));
    }

    [Test]
    public async Task SynchronizeAsync_WhenDeleteFails_ReturnsFailedResult()
    {
        // Arrange
        var sourceRepo = new TestSchemaRepository();
        var targetRepo = new TestSchemaRepository { ThrowOnDelete = true };
        var differences = new List<SchemaDifference>
        {
            new TableSchemaDifference(new OnlyInTarget(), new TestKustoSchema("Table1"))
        };

        // Act
        var result = await _schemaSyncService.SynchronizeAsync(sourceRepo, targetRepo, differences);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Errors.Count, Is.GreaterThan(0));
    }

    // Test helper classes
    
    /// <summary>
    /// Synchronous progress reporter for testing that invokes callbacks immediately
    /// instead of posting to the synchronization context
    /// </summary>
    private class SynchronousProgress<T> : IProgress<T>
    {
        private readonly Action<T> _handler;

        public SynchronousProgress(Action<T> handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public void Report(T value)
        {
            _handler(value);
        }
    }
    
    private class TestSchemaComparisonService : ISchemaComparisonService
    {
        public bool CompareSchemasCalled { get; private set; }

        public SchemaDifferenceResult CompareSchemas(DatabaseSchema source, DatabaseSchema target)
        {
            CompareSchemasCalled = true;
            return new SchemaDifferenceResult(
                new List<SchemaDifference>(),
                new List<SchemaDifference>());
        }
    }

    private class TestSchemaRepository : ISchemaRepository
    {
        public bool ThrowSchemaLoadException { get; set; }
        public bool ThrowGenericException { get; set; }
        public bool ThrowOnSave { get; set; }
        public bool ThrowOnDelete { get; set; }
        public CancellationToken CancellationTokenReceived { get; private set; }
        public List<IKustoSchema> SavedSchemas { get; } = new();
        public List<IKustoSchema> DeletedSchemas { get; } = new();

        public Task<DatabaseSchema> GetSchemaAsync(CancellationToken cancellationToken = default)
        {
            CancellationTokenReceived = cancellationToken;

            if (ThrowSchemaLoadException)
                throw new SchemaLoadException("Test exception");

            if (ThrowGenericException)
                throw new InvalidOperationException("Test generic exception");

            // Return empty DatabaseSchema - constructor signature may vary by Kusto SDK version
            // Using default to handle complex constructor requirements
            var schema = default(DatabaseSchema)!;
            return Task.FromResult(schema);
        }

        public Task SaveSchemaAsync(IEnumerable<IKustoSchema> schemas, CancellationToken cancellationToken = default)
        {
            if (ThrowOnSave)
                throw new InvalidOperationException("Save failed");

            SavedSchemas.AddRange(schemas);
            return Task.CompletedTask;
        }

        public Task DeleteSchemaAsync(IEnumerable<IKustoSchema> schemas, CancellationToken cancellationToken = default)
        {
            if (ThrowOnDelete)
                throw new InvalidOperationException("Delete failed");

            DeletedSchemas.AddRange(schemas);
            return Task.CompletedTask;
        }
    }

    private class TestKustoSchema : IKustoSchema
    {
        public TestKustoSchema(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
