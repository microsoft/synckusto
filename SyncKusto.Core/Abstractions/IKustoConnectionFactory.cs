// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using SyncKusto.Core.Models;

namespace SyncKusto.Core.Abstractions;

/// <summary>
/// Factory for creating Kusto connection strings
/// </summary>
public interface IKustoConnectionFactory
{
    /// <summary>
    /// Creates a Kusto connection string based on the provided options
    /// </summary>
    object CreateConnectionString(KustoConnectionOptions options);
}

/// <summary>
/// Options for creating a Kusto connection
/// </summary>
public record KustoConnectionOptions(
    string Cluster,
    string Database,
    AuthenticationMode AuthMode,
    string Authority,
    string? AppId = null,
    string? AppKey = null,
    string? CertificateThumbprint = null,
    StoreLocation CertificateLocation = StoreLocation.CurrentUser);
