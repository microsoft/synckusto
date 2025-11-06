// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kusto.Data.Common;
using SyncKusto.Core.Abstractions;
using SyncKusto.Core.Exceptions;
using SyncKusto.FileSystem.Exceptions;
using SyncKusto.FileSystem.Extensions;
using SyncKusto.Kusto.Services;
using SyncKusto.Kusto.Exceptions;
using SyncKusto.Kusto.Models;

namespace SyncKusto.FileSystem.Repositories;

/// <summary>
/// Repository for accessing database schemas from the file system
/// </summary>
public class FileSystemSchemaRepository : ISchemaRepository
{
    private readonly string _rootFolder;
    private readonly string _fileExtension;
    private readonly string _tempCluster;
    private readonly string _tempDatabase;
    private readonly string _authority;

    /// <summary>
    /// Initializes a new instance of the FileSystemSchemaRepository
    /// </summary>
    /// <param name="rootFolder">The root folder containing the schema files</param>
    /// <param name="fileExtension">The file extension to use (kql or csl)</param>
    /// <param name="tempCluster">The temporary Kusto cluster for schema loading</param>
    /// <param name="tempDatabase">The temporary database for schema loading</param>
    /// <param name="authority">The AAD authority for authentication</param>
    public FileSystemSchemaRepository(
        string rootFolder, 
        string fileExtension,
        string tempCluster,
        string tempDatabase,
        string authority)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootFolder);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileExtension);
        ArgumentException.ThrowIfNullOrWhiteSpace(tempCluster);
        ArgumentException.ThrowIfNullOrWhiteSpace(tempDatabase);

        _rootFolder = rootFolder;
        _fileExtension = fileExtension;
        _tempCluster = tempCluster;
        _tempDatabase = tempDatabase;
        _authority = authority;
    }

    /// <summary>
    /// Gets the database schema by loading files from the file system
    /// </summary>
    public async Task<DatabaseSchema> GetSchemaAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Find all of the table and function CSL files
            if (!Directory.Exists(_rootFolder))
            {
                Directory.CreateDirectory(_rootFolder);
            }

            string functionFolder = Path.Combine(_rootFolder, "Functions");
            if (!Directory.Exists(functionFolder))
            {
                Directory.CreateDirectory(functionFolder);
            }

            string[] functionFiles = Directory.GetFiles(functionFolder, $"*.{_fileExtension}", SearchOption.AllDirectories);
            IEnumerable<string> tableFiles = Directory.GetFiles(_rootFolder, $"*.{_fileExtension}", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\Functions\\"));

            var failedObjects = new List<string>();

            // Load Kusto Query Engine to parse and validate CSL files
            // This makes it easier to deal with slightly malformed CSL files
            DatabaseSchema? resultSchema = null;
            
            await Task.Run(() =>
            {
                using (var queryEngine = new QueryEngine(_tempCluster, _tempDatabase, _authority))
                {
                    // Deploy all the tables and functions
                    var tableTasks = new List<Task>();
                    foreach (string table in tableFiles)
                    {
                        string tableContent = File.ReadAllText(FileSystemSchemaExtensions.HandleLongFileNames(table));
                        tableTasks.Add(queryEngine.CreateOrAlterTableAsync(tableContent, Path.GetFileName(table), true));
                    }
                    failedObjects.AddRange(WaitAllAndGetFailedObjects(tableTasks));

                    var functionTasks = new List<Task>();
                    foreach (string function in functionFiles)
                    {
                        string csl = File.ReadAllText(FileSystemSchemaExtensions.HandleLongFileNames(function));
                        functionTasks.Add(queryEngine.CreateOrAlterFunctionAsync(csl, Path.GetFileName(function)));
                    }
                    failedObjects.AddRange(WaitAllAndGetFailedObjects(functionTasks));

                    if (failedObjects.Count > 0)
                    {
                        throw new SchemaParseException(
                            $"Failed to parse {failedObjects.Count} schema object(s)",
                            failedObjects);
                    }

                    // Read the functions and tables back from the locally hosted version of Kusto
                    resultSchema = queryEngine.GetDatabaseSchema();
                }
            }, cancellationToken);

            if (resultSchema == null)
            {
                throw new SchemaLoadException("Failed to build database schema from files");
            }

            return resultSchema;
        }
        catch (Exception ex) when (ex is not SyncKustoException)
        {
            throw new SchemaLoadException($"Failed to load schema from file system at '{_rootFolder}'", ex);
        }
    }

    /// <summary>
    /// Saves schemas to the file system
    /// </summary>
    public Task SaveSchemaAsync(
        IEnumerable<IKustoSchema> schemas, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(schemas);

        try
        {
            foreach (var schema in schemas)
            {
                // Unwrap the schema to get the actual Kusto SDK types
                if (schema is KustoTableSchema kustoTable)
                {
                    // TODO: Get these settings from configuration once Phase 5 is implemented
                    kustoTable.Value.WriteToFile(
                        _rootFolder, 
                        _fileExtension,
                        createMergeEnabled: false,
                        tableFieldsOnNewLine: false,
                        Core.Models.LineEndingMode.WindowsStyle);
                }
                else if (schema is KustoFunctionSchema kustoFunction)
                {
                    kustoFunction.Value.WriteToFile(_rootFolder, _fileExtension);
                }
                else
                {
                    throw new SchemaSyncException($"Unknown schema type: {schema.GetType().Name}");
                }
            }

            return Task.CompletedTask;
        }
        catch (Exception ex) when (ex is not SyncKustoException)
        {
            throw new SchemaSyncException($"Failed to save schemas to file system at '{_rootFolder}'", ex);
        }
    }

    /// <summary>
    /// Deletes schemas from the file system
    /// </summary>
    public Task DeleteSchemaAsync(
        IEnumerable<IKustoSchema> schemas, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(schemas);

        try
        {
            foreach (var schema in schemas)
            {
                // Unwrap the schema to get the actual Kusto SDK types
                if (schema is KustoTableSchema kustoTable)
                {
                    kustoTable.Value.DeleteFromFolder(_rootFolder, _fileExtension);
                }
                else if (schema is KustoFunctionSchema kustoFunction)
                {
                    kustoFunction.Value.DeleteFromFolder(_rootFolder, _fileExtension);
                }
                else
                {
                    throw new SchemaSyncException($"Unknown schema type: {schema.GetType().Name}");
                }
            }

            return Task.CompletedTask;
        }
        catch (Exception ex) when (ex is not SyncKustoException)
        {
            throw new SchemaSyncException($"Failed to delete schemas from file system at '{_rootFolder}'", ex);
        }
    }

    /// <summary>
    /// Wait for all tasks and collect failed object names
    /// </summary>
    private static List<string> WaitAllAndGetFailedObjects(List<Task> createOrAlterTasks)
    {
        var failedObjects = new List<string>();
        try
        {
            Task.WaitAll(createOrAlterTasks.ToArray());
        }
        catch (AggregateException ex)
        {
            AggregateException flattenedException = ex.Flatten();
            foreach (Exception exception in flattenedException.InnerExceptions)
            {
                if (exception is CreateOrAlterException createOrAlterEx)
                {
                    failedObjects.Add(createOrAlterEx.FailedEntityName);
                }
                else
                {
                    failedObjects.Add(exception.Message);
                }
            }
        }
        return failedObjects;
    }
}
