// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Kusto.Data.Common;
using SyncKusto.Core.Models;

namespace SyncKusto.Core.Abstractions;

/// <summary>
/// Service for comparing database schemas
/// </summary>
public interface ISchemaComparisonService
{
    /// <summary>
    /// Compares two database schemas and returns their differences.
    /// </summary>
    SchemaDifferenceResult CompareSchemas(
        DatabaseSchema source, 
        DatabaseSchema target);
}
