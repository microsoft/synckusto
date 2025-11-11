// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace SyncKusto.Core.Models;

/// <summary>
/// Information about a schema source (file or Kusto)
/// </summary>
public record SchemaSourceInfo(
    SourceSelection SourceType,
    string? FilePath = null,
    KustoConnectionInfo? KustoInfo = null);

/// <summary>
/// Connection information for a Kusto database
/// </summary>
public record KustoConnectionInfo(
    string Cluster,
    string Database,
    AuthenticationMode AuthMode,
    string Authority,
    string? AppId = null,
    string? AppKey = null,
    string? CertificateThumbprint = null,
    StoreLocation CertificateLocation = StoreLocation.CurrentUser);
