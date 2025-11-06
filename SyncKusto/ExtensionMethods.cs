// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// This file is kept for backward compatibility but the actual implementations
// are now split between:
// - SyncKusto.Kusto.Extensions.KustoSchemaOperationExtensions (Kusto operations)
// - SyncKusto.FileSystem.Extensions.FileSystemSchemaExtensions (File operations)
// Consider using the new namespaces in new code.

using System;
using Kusto.Data.Common;
using SyncKusto.Kusto;
using SyncKusto.Kusto.Extensions;
using SyncKusto.FileSystem.Extensions;

namespace SyncKusto
{
    [Obsolete("Use SyncKusto.Kusto.Extensions.KustoSchemaOperationExtensions or SyncKusto.FileSystem.Extensions.FileSystemSchemaExtensions instead")]
    public static class ExtensionMethods
    {
        /// <summary>
        /// Write the function to the file system.
        /// </summary>
        public static void WriteToFile(this FunctionSchema functionSchema, string rootFolder, string fileExtension) =>
            FileSystemSchemaExtensions.WriteToFile(functionSchema, rootFolder, fileExtension);

        /// <summary>
        /// Write a function to Kusto
        /// </summary>
        public static void WriteToKusto(this FunctionSchema functionSchema, QueryEngine kustoQueryEngine) =>
            KustoSchemaOperationExtensions.WriteToKusto(functionSchema, (SyncKusto.Kusto.Services.QueryEngine)(object)kustoQueryEngine);

        /// <summary>
        /// Delete a function from the file system
        /// </summary>
        public static void DeleteFromFolder(this FunctionSchema functionSchema, string rootFolder, string fileExtension) =>
            FileSystemSchemaExtensions.DeleteFromFolder(functionSchema, rootFolder, fileExtension);

        /// <summary>
        /// Delete a function from Kusto
        /// </summary>
        public static void DeleteFromKusto(this FunctionSchema functionSchema, QueryEngine kustoQueryEngine) =>
            KustoSchemaOperationExtensions.DeleteFromKusto(functionSchema, (SyncKusto.Kusto.Services.QueryEngine)(object)kustoQueryEngine);

        /// <summary>
        /// Write a table to the file system
        /// </summary>
        public static void WriteToFile(this TableSchema tableSchema, string rootFolder, string fileExtension) =>
            FileSystemSchemaExtensions.WriteToFile(
                tableSchema, 
                rootFolder, 
                fileExtension,
                SettingsWrapper.CreateMergeEnabled ?? false,
                SettingsWrapper.TableFieldsOnNewLine ?? false,
                SettingsWrapper.LineEndingMode);

        /// <summary>
        /// Write a table to Kusto
        /// </summary>
        public static void WriteToKusto(this TableSchema tableSchema, QueryEngine kustoQueryEngine) =>
            KustoSchemaOperationExtensions.WriteToKusto(
                tableSchema, 
                (SyncKusto.Kusto.Services.QueryEngine)(object)kustoQueryEngine,
                SettingsWrapper.CreateMergeEnabled ?? false,
                SettingsWrapper.TableFieldsOnNewLine ?? false,
                SettingsWrapper.LineEndingMode);

        /// <summary>
        /// Delete a table from the file system
        /// </summary>
        public static void DeleteFromFolder(this TableSchema tableSchema, string rootFolder, string fileExtension) =>
            FileSystemSchemaExtensions.DeleteFromFolder(tableSchema, rootFolder, fileExtension);

        /// <summary>
        /// Delete a table from Kusto
        /// </summary>
        public static void DeleteFromKusto(this TableSchema tableSchema, QueryEngine kustoQueryEngine) =>
            KustoSchemaOperationExtensions.DeleteFromKusto(tableSchema, (SyncKusto.Kusto.Services.QueryEngine)(object)kustoQueryEngine);

        /// <summary>
        /// Convert to long path to avoid issues with long file names
        /// https://learn.microsoft.com/en-us/windows/win32/fileio/naming-a-file#win32-file-namespaces
        /// </summary>
        public static string HandleLongFileNames(this string filename) =>
            FileSystemSchemaExtensions.HandleLongFileNames(filename);
    }
}
