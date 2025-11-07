// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using SyncKusto.Core.Abstractions;
using SyncKusto.Core.Models;
using Kusto.Data.Common;

namespace SyncKusto.Models;

/// <summary>
/// Result of a comparison operation
/// </summary>
public record ComparisonResult(
    SchemaDifferenceResult Differences,
    DatabaseSchema SourceSchema,
    DatabaseSchema TargetSchema);
