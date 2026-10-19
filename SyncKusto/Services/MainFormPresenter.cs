// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using SyncKusto.Abstractions;
using SyncKusto.Core.Abstractions;
using SyncKusto.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SyncKusto.Services;

/// <summary>
/// Presenter implementation for the main form
/// </summary>
public class MainFormPresenter : IMainFormPresenter
{
    private readonly ISchemaSyncService _syncService;
    private readonly ISchemaValidationService _validationService;
    private readonly SchemaRepositoryFactory _repositoryFactory;

    // Cached for synchronization - need to use the same repository instances
    private ISchemaRepository? _lastSourceRepository;
    private ISchemaRepository? _lastTargetRepository;

    public MainFormPresenter(
        ISchemaSyncService syncService,
        ISchemaValidationService validationService,
        SchemaRepositoryFactory repositoryFactory)
    {
        _syncService = syncService ?? throw new ArgumentNullException(nameof(syncService));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _repositoryFactory = repositoryFactory ?? throw new ArgumentNullException(nameof(repositoryFactory));
    }

    /// <summary>
    /// Compare source and target schemas
    /// </summary>
    public async Task<ComparisonResult> CompareAsync(
        SchemaSourceInfo source,
        SchemaSourceInfo target,
        IProgress<SyncProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);

        _lastSourceRepository = null;
        _lastTargetRepository = null;

        // Create repositories
        var sourceRepository = _repositoryFactory.CreateRepository(source);
        var targetRepository = _repositoryFactory.CreateRepository(target);

        // Load schemas and compare
        var result = await _syncService.CompareAsync(
            sourceRepository,
            targetRepository,
            progress,
            cancellationToken);

        _lastSourceRepository = sourceRepository;
        _lastTargetRepository = targetRepository;
        return result;
    }

    /// <summary>
    /// Synchronize selected differences
    /// </summary>
    public async Task<SyncResult> SynchronizeAsync(
        IEnumerable<SchemaDifference> selectedDifferences,
        IProgress<SyncProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(selectedDifferences);

        if (_lastSourceRepository == null || _lastTargetRepository == null)
        {
            throw new InvalidOperationException("Must compare before synchronizing");
        }

        return await _syncService.SynchronizeAsync(
            _lastSourceRepository,
            _lastTargetRepository,
            selectedDifferences,
            progress,
            cancellationToken);
    }

    /// <summary>
    /// Validate that settings are configured properly
    /// </summary>
    public ValidationResult ValidateSettings(SchemaSourceInfo source, SchemaSourceInfo target)
    {
        return _validationService.ValidateSettings(source, target);
    }
}
