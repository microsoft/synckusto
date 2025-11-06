// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using Kusto.Data.Common;
using SyncKusto.ChangeModel;
using SyncKusto.Core.Abstractions;
using SyncKusto.Extensions;

namespace SyncKusto.Services;

/// <summary>
/// Service for comparing database schemas and identifying differences
/// </summary>
public class SchemaComparisonService : ISchemaComparisonService
{
    /// <summary>
    /// Compares two database schemas and returns their differences.
    /// </summary>
    /// <param name="source">The source schema</param>
    /// <param name="target">The target schema</param>
    /// <returns>A result containing all differences between the schemas</returns>
    public SchemaDifferenceResult CompareSchemas(DatabaseSchema source, DatabaseSchema target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);

        // Compare tables
        var tableDifferences = new KustoSchemaDifferenceMapper(() =>
                source.Tables.AsKustoSchema().DifferenceFrom(target.Tables.AsKustoSchema()))
            .GetDifferences()
            .ToList();

        // Compare functions
        var functionDifferences = new KustoSchemaDifferenceMapper(() =>
                source.Functions.AsKustoSchema().DifferenceFrom(target.Functions.AsKustoSchema()))
            .GetDifferences()
            .ToList();

        return new SchemaDifferenceResult(tableDifferences, functionDifferences);
    }
}
