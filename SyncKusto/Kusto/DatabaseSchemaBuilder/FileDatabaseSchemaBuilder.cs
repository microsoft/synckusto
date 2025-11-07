// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kusto.Data.Common;
using SyncKusto.FileSystem.Exceptions;
using SyncKusto.FileSystem.Extensions;

namespace SyncKusto.Kusto.DatabaseSchemaBuilder
{
    /// <summary>
    /// DEPRECATED: Use SyncKusto.FileSystem.Repositories.FileSystemSchemaRepository instead
    /// This class is kept for backward compatibility but will be removed in a future version
    /// </summary>
    [Obsolete("Use SyncKusto.FileSystem.Repositories.FileSystemSchemaRepository instead")]
    public class FileDatabaseSchemaBuilder : BaseDatabaseSchemaBuilder
    {
        private readonly string rootFolder;
        private readonly string fileExtension;

        public FileDatabaseSchemaBuilder(string rootFolder, string fileExtension)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(rootFolder);
            ArgumentException.ThrowIfNullOrWhiteSpace(fileExtension);

            this.rootFolder = rootFolder;
            this.fileExtension = fileExtension;
        }

        public override Task<DatabaseSchema> Build()
        {
            // Find all of the table and function CSL files
            if (!Directory.Exists(rootFolder))
            {
                Directory.CreateDirectory(rootFolder);
            }

            string functionFolder = Path.Combine(rootFolder, "Functions");
            if (!Directory.Exists(functionFolder))
            {
                Directory.CreateDirectory(functionFolder);
            }

            string[] functionFiles = Directory.GetFiles(functionFolder, $"*.{fileExtension}", SearchOption.AllDirectories);
            IEnumerable<string> tableFiles = Directory.GetFiles(rootFolder, $"*.{fileExtension}", SearchOption.AllDirectories).Where(f => !f.Contains("\\Functions\\"));

            var failedObjects = new List<string>();

            // This is a bit of magic. When called from the UI thread, it would deadlock on Dispose. Running in its own thread works well.
            DatabaseSchema? resultSchema = null;
            var t = Task.Run(() =>
            {
                // Load Kusto Query Engine, this makes it a lot easier to deal with slightly malformed CSL files.
                using (var queryEngine = new SyncKusto.Kusto.Services.QueryEngine(
                    SettingsWrapper.KustoClusterForTempDatabases ?? throw new InvalidOperationException("KustoClusterForTempDatabases setting is not configured"),
                    SettingsWrapper.TemporaryKustoDatabase ?? throw new InvalidOperationException("TemporaryKustoDatabase setting is not configured"),
                    SettingsWrapper.AADAuthority ?? throw new InvalidOperationException("AADAuthority setting is not configured"),
                    SettingsWrapper.LineEndingMode))
                {
                    // Deploy all the tables and functions
                    var tableTasks = new List<Task>();
                    foreach (string table in tableFiles)
                    {
                        tableTasks.Add(queryEngine.CreateOrAlterTableAsync(
                            File.ReadAllText(FileSystemSchemaExtensions.HandleLongFileNames(table)), 
                            Path.GetFileName(table), 
                            true));
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
                        // Instead of showing MessageBox, throw an exception that the UI layer can catch and display
                        throw new SchemaParseException(
                            $"Failed to parse {failedObjects.Count} schema object(s)",
                            failedObjects);
                    }

                    // Read the functions and tables back from the locally hosted version of Kusto.
                    resultSchema = queryEngine.GetDatabaseSchema();
                }
            });
            t.Wait();

            if (resultSchema == null)
            {
                throw new InvalidOperationException("Failed to build database schema from files");
            }

            return Task.FromResult(resultSchema);
        }
    }
}