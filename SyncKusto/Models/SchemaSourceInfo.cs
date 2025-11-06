// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using SyncKusto.Core.Abstractions;
using SyncKusto.Core.Models;
using SyncKusto.SyncSources;
using Kusto.Data.Common;

namespace SyncKusto.Models;

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
    string? CertificateThumbprint = null);

/// <summary>
/// Result of a comparison operation
/// </summary>
public record ComparisonResult(
    SchemaDifferenceResult Differences,
    DatabaseSchema SourceSchema,
    DatabaseSchema TargetSchema);

/// <summary>
/// Result of a validation operation
/// </summary>
public record ValidationResult(
    bool IsValid,
    string? ErrorMessage = null)
{
    public static ValidationResult Success() => new(true);
    public static ValidationResult Failure(string errorMessage) => new(false, errorMessage);
}
