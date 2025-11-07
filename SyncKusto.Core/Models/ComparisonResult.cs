// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Kusto.Data.Common;
using SyncKusto.Core.Abstractions;

namespace SyncKusto.Core.Models;

/// <summary>
/// Result of a schema comparison operation
/// </summary>
public record ComparisonResult(
    SchemaDifferenceResult Differences,
    DatabaseSchema SourceSchema,
    DatabaseSchema TargetSchema);
