// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;
using SyncKusto.Core.Models;

namespace SyncKusto.Core.Configuration;

/// <summary>
/// Immutable configuration settings for SyncKusto
/// </summary>
public record SyncKustoSettings
{
    public required string TempCluster { get; init; }
    public required string TempDatabase { get; init; }
    public string? AADAuthority { get; init; }
    public bool KustoObjectDropWarning { get; init; } = true;
    public bool TableFieldsOnNewLine { get; init; } = false;
    public bool CreateMergeEnabled { get; init; } = false;
    public bool UseLegacyCslExtension { get; init; } = true;
    public LineEndingMode LineEndingMode { get; init; } = LineEndingMode.LeaveAsIs;
    public StoreLocation CertificateLocation { get; init; } = StoreLocation.CurrentUser;
    public string FileExtension => UseLegacyCslExtension ? "csl" : "kql";
}
