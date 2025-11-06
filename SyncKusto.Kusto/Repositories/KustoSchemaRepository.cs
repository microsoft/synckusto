// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Kusto.Data;
using Kusto.Data.Common;
using SyncKusto.Core.Abstractions;
using SyncKusto.Core.Exceptions;
using SyncKusto.Kusto.Models;
using SyncKusto.Kusto.Services;

namespace SyncKusto.Kusto.Repositories;

/// <summary>
/// Repository for accessing Kusto database schemas
/// </summary>
public class KustoSchemaRepository : ISchemaRepository, IDisposable
{
    private readonly QueryEngine _queryEngine;
    private bool _disposed;

    /// <summary>
    /// Creates a new Kusto schema repository
    /// </summary>
    /// <param name="connectionString">The Kusto connection string builder</param>
    /// <param name="lineEndingMode">The line ending mode for function bodies</param>
    public KustoSchemaRepository(KustoConnectionStringBuilder connectionString, Core.Models.LineEndingMode lineEndingMode)
    {
        ArgumentNullException.ThrowIfNull(connectionString);
        _queryEngine = new QueryEngine(connectionString, lineEndingMode);
    }

    /// <summary>
    /// Gets the database schema from Kusto
    /// </summary>
    /// <exception cref="SchemaLoadException">Thrown when schema cannot be loaded</exception>
    public Task<DatabaseSchema> GetSchemaAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return Task.Run(() => _queryEngine.GetDatabaseSchema(), cancellationToken);
        }
        catch (Exception ex) when (ex is not SchemaLoadException)
        {
            throw new SchemaLoadException("Failed to load schema from Kusto", ex);
        }
    }

    /// <summary>
    /// Saves schemas to Kusto
    /// </summary>
    /// <exception cref="SchemaSyncException">Thrown when schemas cannot be saved</exception>
    public async Task SaveSchemaAsync(
        IEnumerable<IKustoSchema> schemas, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(schemas);

        try
        {
            foreach (var schema in schemas)
            {
                cancellationToken.ThrowIfCancellationRequested();

                switch (schema)
                {
                    case KustoTableSchema tableSchema:
                        await _queryEngine.CreateOrAlterTableAsync(
                            FormattedCslCommandGenerator.GenerateTableCreateCommand(tableSchema.Value, forceNormalizeColumnName: false),
                            tableSchema.Name).ConfigureAwait(false);
                        break;

                    case KustoFunctionSchema functionSchema:
                        await _queryEngine.CreateOrAlterFunctionAsync(
                            CslCommandGenerator.GenerateCreateOrAlterFunctionCommand(functionSchema.Value, true),
                            functionSchema.Name).ConfigureAwait(false);
                        break;

                    default:
                        throw new InvalidOperationException($"Unknown schema type: {schema.GetType().Name}");
                }
            }
        }
        catch (Exception ex) when (ex is not SchemaSyncException and not OperationCanceledException)
        {
            throw new SchemaSyncException("Failed to save schemas to Kusto", ex);
        }
    }

    /// <summary>
    /// Deletes schemas from Kusto
    /// </summary>
    /// <exception cref="SchemaSyncException">Thrown when schemas cannot be deleted</exception>
    public Task DeleteSchemaAsync(
        IEnumerable<IKustoSchema> schemas, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(schemas);

        try
        {
            return Task.Run(() =>
            {
                foreach (var schema in schemas)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    switch (schema)
                    {
                        case KustoTableSchema tableSchema:
                            _queryEngine.DropTable(tableSchema.Name);
                            break;

                        case KustoFunctionSchema functionSchema:
                            _queryEngine.DropFunction(functionSchema.Value);
                            break;

                        default:
                            throw new InvalidOperationException($"Unknown schema type: {schema.GetType().Name}");
                    }
                }
            }, cancellationToken);
        }
        catch (Exception ex) when (ex is not SchemaSyncException and not OperationCanceledException)
        {
            throw new SchemaSyncException("Failed to delete schemas from Kusto", ex);
        }
    }

    /// <summary>
    /// Disposes the repository and its resources
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _queryEngine?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
