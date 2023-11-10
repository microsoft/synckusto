// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Kusto.Data.Common;

namespace SyncKusto.Kusto.DatabaseSchemaBuilder
{
    public class FileDatabaseSchemaBuilder : BaseDatabaseSchemaBuilder
    {
        private readonly string rootFolder;
        private readonly string fileExtension;

        public FileDatabaseSchemaBuilder(string rootFolder, string fileExtension)
        {
            if (string.IsNullOrWhiteSpace(rootFolder))
            {
                throw new ArgumentException($"'{nameof(rootFolder)}' cannot be null or whitespace.", nameof(rootFolder));
            }

            if (string.IsNullOrWhiteSpace(fileExtension))
            {
                throw new ArgumentException($"'{nameof(fileExtension)}' cannot be null or whitespace.", nameof(fileExtension));
            }

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
            DatabaseSchema resultSchema = null;
            var t = Task.Run(() =>
            {
                // Load Kusto Query Engine, this makes it a lot easier to deal with slightly malformed CSL files.
                using (var queryEngine = new QueryEngine())
                {

                    // Deploy all the tables and functions
                    var tableTasks = new List<Task>();
                    foreach (string table in tableFiles)
                    {
                        tableTasks.Add(queryEngine.CreateOrAlterTableAsync(File.ReadAllText(table.HandleLongFileNames()), Path.GetFileName(table), true));
                    }
                    failedObjects.AddRange(WaitAllAndGetFailedObjects(tableTasks));

                    var functionTasks = new List<Task>();
                    foreach (string function in functionFiles)
                    {
                        string csl = File.ReadAllText(function.HandleLongFileNames());
                        functionTasks.Add(queryEngine.CreateOrAlterFunctionAsync(csl, Path.GetFileName(function)));
                    }
                    failedObjects.AddRange(WaitAllAndGetFailedObjects(functionTasks));

                    if (failedObjects.Count > 0)
                    {
                        MessageBox.Show(
                            $"The following objects could not be parsed and will be ignored:\r\n{failedObjects.Aggregate((current, next) => current + "\r\n" + next)}",
                            "Failure", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }

                    // Read the functions and tables back from the locally hosted version of Kusto.
                    resultSchema = queryEngine.GetDatabaseSchema();
                }
            });
            t.Wait();

            return Task.FromResult(resultSchema);
        }
    }
}