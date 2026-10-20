// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using SyncKusto.Abstractions;
using SyncKusto.Core.Abstractions;
using SyncKusto.Core.Models;
using SyncKusto.Core.Exceptions;
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

    // Keep compared file snapshots; Kusto writes use an operation-scoped current identity.
    private ISchemaRepository? _lastSourceRepository;
    private ISchemaRepository? _lastTargetRepository;
    private SchemaSourceInfo? _lastTargetInfo;

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
        _lastTargetInfo = null;

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
        _lastTargetInfo = target;
        return result;
    }

    /// <summary>
    /// Synchronize selected differences
    /// </summary>
    public async Task<SyncResult> SynchronizeAsync(
        IEnumerable<SchemaDifference> selectedDifferences,
        SchemaSourceInfo target,
        IProgress<SyncProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(selectedDifferences);
        ArgumentNullException.ThrowIfNull(target);

        if (_lastSourceRepository == null || _lastTargetRepository == null || _lastTargetInfo == null)
        {
            throw new InvalidOperationException("Must compare before synchronizing");
        }

        if (!HasSameLocation(_lastTargetInfo, target))
            throw new InvalidOperationException("The target location has changed. Compare schemas again before updating.");

        ValidateTargetCredentials(target);
        var targetRepository = target.SourceType == SourceSelection.Kusto()
            ? _repositoryFactory.CreateRepository(target)
            : _lastTargetRepository;
        try
        {
            return await _syncService.SynchronizeAsync(
                _lastSourceRepository,
                targetRepository,
                selectedDifferences,
                progress,
                cancellationToken);
        }
        finally
        {
            if (target.SourceType == SourceSelection.Kusto())
                (targetRepository as IDisposable)?.Dispose();
        }
    }

    private static bool HasSameLocation(SchemaSourceInfo compared, SchemaSourceInfo current) =>
        compared.SourceType == current.SourceType &&
        (current.SourceType == SourceSelection.FilePath()
            ? string.Equals(compared.FilePath, current.FilePath, StringComparison.OrdinalIgnoreCase)
            : string.Equals(compared.KustoInfo?.Cluster, current.KustoInfo?.Cluster, StringComparison.OrdinalIgnoreCase) &&
              string.Equals(compared.KustoInfo?.Database, current.KustoInfo?.Database, StringComparison.Ordinal));

    private static void ValidateTargetCredentials(SchemaSourceInfo target)
    {
        if (target.SourceType != SourceSelection.Kusto())
            return;

        var info = target.KustoInfo;
        if (info == null || string.IsNullOrWhiteSpace(info.Cluster) || string.IsNullOrWhiteSpace(info.Database))
            throw new KustoSettingsException("Specify the target Kusto cluster and database.");
        if (info.AuthMode == AuthenticationMode.AadFederated)
            return;
        if (string.IsNullOrWhiteSpace(info.Authority))
            throw new KustoSettingsException("Application authentication requires an Entra ID authority in Settings.");
        if (string.IsNullOrWhiteSpace(info.AppId))
            throw new KustoSettingsException("Specify the target application ID.");
        if (info.AuthMode == AuthenticationMode.AadApplication)
        {
            if (string.IsNullOrWhiteSpace(info.AppKey))
                throw new KustoSettingsException("Specify the target application key.");
        }
        else if (info.AuthMode == AuthenticationMode.AadApplicationSni)
        {
            if (string.IsNullOrWhiteSpace(info.CertificateThumbprint))
                throw new KustoSettingsException("Select the target application certificate.");
        }
        else
        {
            throw new KustoSettingsException("Select a supported target authentication mode.");
        }
    }

    /// <summary>
    /// Validate that settings are configured properly
    /// </summary>
    public ValidationResult ValidateSettings(SchemaSourceInfo source, SchemaSourceInfo target)
    {
        return _validationService.ValidateSettings(source, target);
    }
}
