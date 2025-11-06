// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Kusto.Data.Common;
using SyncKusto.Core.Abstractions;
using SyncKusto.Kusto.Models;

namespace SyncKusto.Kusto.Extensions;

/// <summary>
/// Extension methods for converting between Kusto SDK types and IKustoSchema
/// </summary>
public static class KustoSchemaExtensions
{
    /// <summary>
    /// Converts a TableSchema to IKustoSchema
    /// </summary>
    public static IKustoSchema AsKustoSchema(this TableSchema schema) => new KustoTableSchema(schema);
    
    /// <summary>
    /// Converts a FunctionSchema to IKustoSchema
    /// </summary>
    public static IKustoSchema AsKustoSchema(this FunctionSchema schema) => new KustoFunctionSchema(schema);

    /// <summary>
    /// Converts a dictionary of TableSchemas to IKustoSchemas
    /// </summary>
    public static Dictionary<string, IKustoSchema> AsKustoSchema(this Dictionary<string, TableSchema> schemas) =>
        schemas.ToDictionary(x => x.Key, x => x.Value.AsKustoSchema());

    /// <summary>
    /// Converts a dictionary of FunctionSchemas to IKustoSchemas
    /// </summary>
    public static Dictionary<string, IKustoSchema> AsKustoSchema(this Dictionary<string, FunctionSchema> schemas) =>
        schemas.ToDictionary(x => x.Key, x => x.Value.AsKustoSchema());
}
