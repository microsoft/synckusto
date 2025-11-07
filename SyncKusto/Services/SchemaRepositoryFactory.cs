// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Kusto.Data;
using SyncKusto.Core.Abstractions;
using SyncKusto.Core.Configuration;
using SyncKusto.Core.Models;
using SyncKusto.FileSystem.Repositories;
using SyncKusto.Kusto.Repositories;
using SyncKusto.Kusto.Services;

namespace SyncKusto.Services;

/// <summary>
/// Factory for creating schema repositories based on source information
/// </summary>
public class SchemaRepositoryFactory
{
    private readonly SyncKustoSettings _settings;
    private readonly KustoConnectionFactory _kustoConnectionFactory;

    public SchemaRepositoryFactory(SyncKustoSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _kustoConnectionFactory = new KustoConnectionFactory();
    }

    /// <summary>
    /// Create a repository based on the source information
    /// </summary>
    public ISchemaRepository CreateRepository(SchemaSourceInfo sourceInfo)
    {
        ArgumentNullException.ThrowIfNull(sourceInfo);

        if (sourceInfo.SourceType == SourceSelection.FilePath())
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sourceInfo.FilePath, nameof(sourceInfo.FilePath));
            
            return new FileSystemSchemaRepository(
                sourceInfo.FilePath,
                _settings.FileExtension,
                _settings.TempCluster,
                _settings.TempDatabase,
                _settings.AADAuthority ?? "",
                _settings);
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
                CertificateLocation: _settings.CertificateLocation);
            
            var connectionString = (global::Kusto.Data.KustoConnectionStringBuilder)_kustoConnectionFactory.CreateConnectionString(options);
            
            return new KustoSchemaRepository(connectionString, _settings.LineEndingMode);
        }
        else
        {
            throw new InvalidOperationException($"Unknown source type: {sourceInfo.SourceType}");
        }
    }
}
