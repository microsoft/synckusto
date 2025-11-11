// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace SyncKusto.Core.Abstractions;

/// <summary>
/// Result of comparing two database schemas
/// </summary>
public record SchemaDifferenceResult(
    IReadOnlyList<SchemaDifference> TableDifferences,
    IReadOnlyList<SchemaDifference> FunctionDifferences)
{
    public IEnumerable<SchemaDifference> AllDifferences =>
        TableDifferences.Concat(FunctionDifferences);
}
