// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using SyncKusto.Core.Abstractions;
using SyncKusto.Core.Models;
using SyncKusto.FileSystem.Repositories;
using SyncKusto.Kusto.Repositories;
using System;

namespace SyncKusto.Services;

/// <summary>
/// Factory for creating schema repositories based on source information
/// </summary>
public class SchemaRepositoryFactory
{
    private readonly ISettingsProvider _settingsProvider;
    private readonly IKustoConnectionFactory _kustoConnectionFactory;

    public SchemaRepositoryFactory(ISettingsProvider settingsProvider, IKustoConnectionFactory kustoConnectionFactory)
    {
        _settingsProvider = settingsProvider ?? throw new ArgumentNullException(nameof(settingsProvider));
        _kustoConnectionFactory = kustoConnectionFactory ?? throw new ArgumentNullException(nameof(kustoConnectionFactory));
    }

    /// <summary>
    /// Create a repository based on the source information
    /// </summary>
    public virtual ISchemaRepository CreateRepository(SchemaSourceInfo sourceInfo)
    {
        ArgumentNullException.ThrowIfNull(sourceInfo);
        var settings = _settingsProvider.GetSyncKustoSettings();

        if (sourceInfo.SourceType == SourceSelection.FilePath())
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sourceInfo.FilePath, nameof(sourceInfo.FilePath));

            return new FileSystemSchemaRepository(
                sourceInfo.FilePath,
                settings.FileExtension,
                settings.TempCluster,
                settings.TempDatabase,
                settings.AADAuthority ?? "",
                settings);
        }
        else if (sourceInfo.SourceType == SourceSelection.Kusto())
        {
            ArgumentNullException.ThrowIfNull(sourceInfo.KustoInfo, nameof(sourceInfo.KustoInfo));

            var options = new KustoConnectionOptions(
                Cluster: sourceInfo.KustoInfo.Cluster,
                Database: sourceInfo.KustoInfo.Database,
                AuthMode: sourceInfo.KustoInfo.AuthMode,
                Authority: sourceInfo.KustoInfo.Authority,
                AppId: sourceInfo.KustoInfo.AppId,
                AppKey: sourceInfo.KustoInfo.AppKey,
                CertificateThumbprint: sourceInfo.KustoInfo.CertificateThumbprint,
                CertificateLocation: settings.CertificateLocation);

            var connectionString = _kustoConnectionFactory.CreateConnectionString(options);

            return new KustoSchemaRepository(connectionString, settings.LineEndingMode);
        }
        else
        {
            throw new InvalidOperationException($"Unknown source type: {sourceInfo.SourceType}");
        }
    }
}
