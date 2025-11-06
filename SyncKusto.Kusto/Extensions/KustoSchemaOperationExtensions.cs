// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using Kusto.Data.Common;
using SyncKusto.Kusto.Services;

namespace SyncKusto.Kusto.Extensions;

/// <summary>
/// Extension methods for Kusto schema operations
/// NOTE: File system operations have been moved to SyncKusto.FileSystem.Extensions.FileSystemSchemaExtensions
/// </summary>
public static class KustoSchemaOperationExtensions
{
    /// <summary>
    /// Write a function to Kusto
    /// </summary>
    /// <param name="functionSchema">The function to write</param>
    /// <param name="kustoQueryEngine">An initialized query engine for issuing the Kusto command</param>
    public static void WriteToKusto(this FunctionSchema functionSchema, QueryEngine kustoQueryEngine)
    {
        ArgumentNullException.ThrowIfNull(functionSchema);
        ArgumentNullException.ThrowIfNull(kustoQueryEngine);
        
        kustoQueryEngine.CreateOrAlterFunctionAsync(
            CslCommandGenerator.GenerateCreateOrAlterFunctionCommand(functionSchema, true), 
            functionSchema.Name).Wait();
    }

    /// <summary>
    /// Delete a function from Kusto
    /// </summary>
    /// <param name="functionSchema">The function to remove</param>
    /// <param name="kustoQueryEngine">An initialized query engine for issuing the Kusto command</param>
    public static void DeleteFromKusto(this FunctionSchema functionSchema, QueryEngine kustoQueryEngine)
    {
        ArgumentNullException.ThrowIfNull(functionSchema);
        ArgumentNullException.ThrowIfNull(kustoQueryEngine);
        
        kustoQueryEngine.DropFunction(functionSchema);
    }

    /// <summary>
    /// Write a table to Kusto
    /// </summary>
    /// <param name="tableSchema">The table to write</param>
    /// <param name="kustoQueryEngine">An initialized query engine for issuing the Kusto command</param>
    /// <param name="createMergeEnabled">Whether to use create-merge command</param>
    /// <param name="tableFieldsOnNewLine">Whether to put each field on a new line</param>
    /// <param name="lineEndingMode">The line ending mode to use</param>
    public static void WriteToKusto(
        this TableSchema tableSchema, 
        QueryEngine kustoQueryEngine,
        bool createMergeEnabled = false,
        bool tableFieldsOnNewLine = false,
        Core.Models.LineEndingMode lineEndingMode = Core.Models.LineEndingMode.WindowsStyle)
    {
        ArgumentNullException.ThrowIfNull(tableSchema);
        ArgumentNullException.ThrowIfNull(kustoQueryEngine);
        
        kustoQueryEngine.CreateOrAlterTableAsync(
            FormattedCslCommandGenerator.GenerateTableCreateCommand(
                tableSchema, 
                forceNormalizeColumnName: false,
                createMergeEnabled: createMergeEnabled,
                tableFieldsOnNewLine: tableFieldsOnNewLine,
                lineEndingMode: lineEndingMode), 
            tableSchema.Name).Wait();
    }

    /// <summary>
    /// Delete a table from Kusto
    /// </summary>
    /// <param name="tableSchema">The table to remove</param>
    /// <param name="kustoQueryEngine">An initialized query engine for issuing the Kusto command</param>
    public static void DeleteFromKusto(this TableSchema tableSchema, QueryEngine kustoQueryEngine)
    {
        ArgumentNullException.ThrowIfNull(tableSchema);
        ArgumentNullException.ThrowIfNull(kustoQueryEngine);
        
        kustoQueryEngine.DropTable(tableSchema.Name);
    }
}
