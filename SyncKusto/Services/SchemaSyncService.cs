// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SyncKusto.Core.Abstractions;
using SyncKusto.Core.Exceptions;
using SyncKusto.Core.Models;

namespace SyncKusto.Services;

/// <summary>
/// Service for orchestrating schema synchronization operations
/// </summary>
public class SchemaSyncService : ISchemaSyncService
{
    private readonly ISchemaComparisonService _comparisonService;

    public SchemaSyncService(ISchemaComparisonService comparisonService)
    {
        _comparisonService = comparisonService ?? throw new ArgumentNullException(nameof(comparisonService));
    }

    /// <summary>
    /// Compares source and target schemas.
    /// </summary>
    /// <exception cref="SchemaLoadException">Thrown when schemas cannot be loaded</exception>
    /// <exception cref="SchemaValidationException">Thrown when schema validation fails</exception>
    public async Task<SchemaDifferenceResult> CompareAsync(
        ISchemaRepository source,
        ISchemaRepository target,
        IProgress<SyncProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);

        try
        {
            // Report progress: Loading source schema
            progress?.Report(new SyncProgress(
                "Loading source schema...",
                10,
                SyncProgressStage.LoadingSourceSchema));

            var sourceSchema = await source.GetSchemaAsync(cancellationToken);

            // Report progress: Loading target schema
            progress?.Report(new SyncProgress(
                "Loading target schema...",
                50,
                SyncProgressStage.LoadingTargetSchema));

            var targetSchema = await target.GetSchemaAsync(cancellationToken);

            // Report progress: Comparing schemas
            progress?.Report(new SyncProgress(
                "Comparing schemas...",
                75,
                SyncProgressStage.ComparingSchemas));

            var result = _comparisonService.CompareSchemas(sourceSchema, targetSchema);

            // Report progress: Complete
            progress?.Report(new SyncProgress(
                "Comparison complete",
                100,
                SyncProgressStage.Complete));

            return result;
        }
        catch (SchemaLoadException)
        {
            throw; // Re-throw schema-specific exceptions as-is
        }
        catch (Exception ex)
        {
            throw new SchemaLoadException("Failed to load and compare schemas", ex);
        }
    }

    /// <summary>
    /// Synchronizes selected differences from source to target.
    /// </summary>
    /// <exception cref="SchemaSyncException">Thrown when synchronization fails</exception>
    public async Task<SyncResult> SynchronizeAsync(
        ISchemaRepository source,
        ISchemaRepository target,
        IEnumerable<SchemaDifference> selectedDifferences,
        IProgress<SyncProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(selectedDifferences);

        var differencesList = selectedDifferences.ToList();
        
        if (differencesList.Count == 0)
        {
            return SyncResult.Successful(0);
        }

        try
        {
            progress?.Report(new SyncProgress(
                "Starting synchronization...",
                0,
                SyncProgressStage.SynchronizingSchemas));

            var schemasToSave = new List<IKustoSchema>();
            var schemasToDelete = new List<IKustoSchema>();
            var errors = new List<string>();
            int processedCount = 0;

            // Categorize differences into save vs delete operations
            foreach (var difference in differencesList)
            {
                switch (difference.Difference)
                {
                    case OnlyInSource:
                    case Modified:
                        schemasToSave.Add(difference.Schema);
                        break;

                    case OnlyInTarget:
                        schemasToDelete.Add(difference.Schema);
                        break;

                    default:
                        errors.Add($"Unknown difference type for {difference.Name}");
                        break;
                }

                processedCount++;
                var percent = (int)((double)processedCount / differencesList.Count * 100);
                progress?.Report(new SyncProgress(
                    $"Processing {difference.Name}...",
                    percent,
                    SyncProgressStage.SynchronizingSchemas));
            }

            // Perform save operations
            if (schemasToSave.Any())
            {
                progress?.Report(new SyncProgress(
                    $"Saving {schemasToSave.Count} schema(s) to target...",
                    50,
                    SyncProgressStage.SynchronizingSchemas));

                try
                {
                    await target.SaveSchemaAsync(schemasToSave, cancellationToken);
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to save schemas: {ex.Message}");
                }
            }

            // Perform delete operations
            if (schemasToDelete.Any())
            {
                progress?.Report(new SyncProgress(
                    $"Deleting {schemasToDelete.Count} schema(s) from target...",
                    75,
                    SyncProgressStage.SynchronizingSchemas));

                try
                {
                    await target.DeleteSchemaAsync(schemasToDelete, cancellationToken);
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to delete schemas: {ex.Message}");
                }
            }

            progress?.Report(new SyncProgress(
                "Synchronization complete",
                100,
                SyncProgressStage.Complete));

            if (errors.Any())
            {
                return SyncResult.Failed(errors);
            }

            return SyncResult.Successful(differencesList.Count);
        }
        catch (SchemaSyncException)
        {
            throw; // Re-throw schema-specific exceptions as-is
        }
        catch (Exception ex)
        {
            throw new SchemaSyncException("Failed to synchronize schemas", ex);
        }
    }
}
