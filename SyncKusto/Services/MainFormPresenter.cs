// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SyncKusto.Abstractions;
using SyncKusto.Core.Abstractions;
using SyncKusto.Core.Configuration;
using SyncKusto.Core.Models;
using SyncKusto.Models;
using Kusto.Data.Common;

namespace SyncKusto.Services;

/// <summary>
/// Presenter implementation for the main form
/// </summary>
public class MainFormPresenter : IMainFormPresenter
{
    private readonly ISchemaSyncService _syncService;
    private readonly ISchemaValidationService _validationService;
    private readonly SchemaRepositoryFactory _repositoryFactory;
    private readonly SyncKustoSettings _settings;
    
    // Cached for synchronization - need to use the same repository instances
    private ISchemaRepository? _lastSourceRepository;
    private ISchemaRepository? _lastTargetRepository;
    private DatabaseSchema? _lastSourceSchema;
    private DatabaseSchema? _lastTargetSchema;

    public MainFormPresenter(
        ISchemaSyncService syncService,
        ISchemaValidationService validationService,
        SchemaRepositoryFactory repositoryFactory,
        SyncKustoSettings settings)
    {
        _syncService = syncService ?? throw new ArgumentNullException(nameof(syncService));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _repositoryFactory = repositoryFactory ?? throw new ArgumentNullException(nameof(repositoryFactory));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
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

        // Create repositories
        _lastSourceRepository = _repositoryFactory.CreateRepository(source);
        _lastTargetRepository = _repositoryFactory.CreateRepository(target);
        
        // Load schemas and compare
        var differences = await _syncService.CompareAsync(
            _lastSourceRepository,
            _lastTargetRepository,
            progress,
            cancellationToken);
        
        // Also get the actual schemas for display (needed by MainForm for diff view)
        _lastSourceSchema = await _lastSourceRepository.GetSchemaAsync(cancellationToken);
        _lastTargetSchema = await _lastTargetRepository.GetSchemaAsync(cancellationToken);
        
        return new ComparisonResult(differences, _lastSourceSchema, _lastTargetSchema);
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
