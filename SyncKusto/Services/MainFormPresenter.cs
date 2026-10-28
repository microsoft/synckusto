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
public class MainFormPresenter : IMainFormPresenter, IDisposable
{
    private readonly ISchemaSyncService _syncService;
    private readonly ISchemaValidationService _validationService;
    private readonly SchemaRepositoryFactory _repositoryFactory;

    // Keep compared file snapshots; Kusto writes use an operation-scoped current identity.
    private ISchemaRepository? _lastSourceRepository;
    private ISchemaRepository? _lastTargetRepository;
    private SchemaSourceInfo? _lastTargetInfo;
    private bool _operationInProgress;
    private bool _disposed;
    private int _comparisonVersion;

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

        BeginOperation();
        ISchemaRepository? sourceRepository = null, targetRepository = null;
        try
        {
            ClearComparison();
            ReleaseRepositories();
            int version = _comparisonVersion;
            sourceRepository = _repositoryFactory.CreateRepository(source);
            targetRepository = _repositoryFactory.CreateRepository(target);

            var result = await _syncService.CompareAsync(
                sourceRepository, targetRepository, progress, cancellationToken);

            if (_disposed || version != _comparisonVersion)
                throw new OperationCanceledException("The comparison was cleared before it completed.");

            _lastSourceRepository = sourceRepository;
            _lastTargetRepository = targetRepository;
            _lastTargetInfo = target;
            sourceRepository = targetRepository = null;
            return result;
        }
        finally
        {
            try
            {
                DisposeRepositories(sourceRepository, targetRepository);
            }
            finally
            {
                EndOperation();
            }
        }
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

        BeginOperation();
        ISchemaRepository? targetRepository = null;
        try
        {
            if (_lastSourceRepository == null || _lastTargetRepository == null || _lastTargetInfo == null)
                throw new InvalidOperationException("Must compare before synchronizing");

            if (!HasSameLocation(_lastTargetInfo, target))
                throw new InvalidOperationException("The target location has changed. Compare schemas again before updating.");

            ValidateTargetCredentials(target);
            targetRepository = target.SourceType == SourceSelection.Kusto()
                ? _repositoryFactory.CreateRepository(target)
                : _lastTargetRepository;
            return await _syncService.SynchronizeAsync(
                _lastSourceRepository,
                targetRepository,
                selectedDifferences,
                progress,
                cancellationToken);
        }
        finally
        {
            try
            {
                if (target.SourceType == SourceSelection.Kusto())
                    (targetRepository as IDisposable)?.Dispose();
            }
            finally
            {
                EndOperation();
            }
        }
    }

    public void ClearComparison()
    {
        _comparisonVersion++;
        _lastTargetInfo = null;
        if (!_operationInProgress)
            ReleaseRepositories();
    }

    private void BeginOperation()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MainFormPresenter));
        if (_operationInProgress)
            throw new InvalidOperationException("Another schema operation is already in progress.");
        _operationInProgress = true;
    }

    private void EndOperation()
    {
        _operationInProgress = false;
        if (_lastTargetInfo == null)
            ReleaseRepositories();
    }

    private void ReleaseRepositories()
    {
        var source = _lastSourceRepository;
        var target = _lastTargetRepository;
        _lastSourceRepository = _lastTargetRepository = null;
        _lastTargetInfo = null;
        DisposeRepositories(source, target);
    }

    private static void DisposeRepositories(ISchemaRepository? source, ISchemaRepository? target)
    {
        try
        {
            (source as IDisposable)?.Dispose();
        }
        finally
        {
            if (!ReferenceEquals(source, target))
                (target as IDisposable)?.Dispose();
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            ClearComparison();
        }
        GC.SuppressFinalize(this);
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
