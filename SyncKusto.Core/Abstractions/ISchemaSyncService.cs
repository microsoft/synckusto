// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using SyncKusto.Core.Exceptions;
using SyncKusto.Core.Models;

namespace SyncKusto.Core.Abstractions;

/// <summary>
/// Service for orchestrating schema synchronization operations
/// </summary>
public interface ISchemaSyncService
{
    /// <summary>
    /// Compares source and target schemas.
    /// </summary>
    /// <exception cref="SchemaLoadException">Thrown when schemas cannot be loaded</exception>
    /// <exception cref="SchemaValidationException">Thrown when schema validation fails</exception>
    Task<SchemaDifferenceResult> CompareAsync(
        ISchemaRepository source,
        ISchemaRepository target,
        IProgress<SyncProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Synchronizes selected differences from source to target.
    /// </summary>
    /// <exception cref="SchemaSyncException">Thrown when synchronization fails</exception>
    Task<SyncResult> SynchronizeAsync(
        ISchemaRepository source,
        ISchemaRepository target,
        IEnumerable<SchemaDifference> selectedDifferences,
        IProgress<SyncProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
