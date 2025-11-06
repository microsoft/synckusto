// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Kusto.Data.Common;
using SyncKusto.Core.Exceptions;

namespace SyncKusto.Core.Abstractions;

/// <summary>
/// Repository pattern for accessing database schemas from various sources
/// </summary>
public interface ISchemaRepository
{
    /// <summary>
    /// Gets the database schema.
    /// </summary>
    /// <exception cref="SchemaLoadException">Thrown when schema cannot be loaded</exception>
    Task<DatabaseSchema> GetSchemaAsync(
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Saves schemas to the repository.
    /// </summary>
    /// <exception cref="SchemaSyncException">Thrown when schemas cannot be saved</exception>
    Task SaveSchemaAsync(
        IEnumerable<IKustoSchema> schemas, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes schemas from the repository.
    /// </summary>
    /// <exception cref="SchemaSyncException">Thrown when schemas cannot be deleted</exception>
    Task DeleteSchemaAsync(
        IEnumerable<IKustoSchema> schemas, 
        CancellationToken cancellationToken = default);
}
