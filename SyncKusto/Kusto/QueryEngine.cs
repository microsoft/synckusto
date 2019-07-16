// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;
using Newtonsoft.Json;

namespace SyncKusto.Kusto
{
    /// <summary>
    /// Central location for interacting with a Kusto cluster
    /// </summary>
    public class QueryEngine : IDisposable
    {
        private readonly ICslAdminProvider _adminClient;
        private readonly ICslQueryProvider _queryClient;
        private readonly string _databaseName;
        private readonly string _cluster;
        private readonly bool _tempDatabaseUsed = false;

        /// <summary>
        /// Constructor which gets ready to make queries to Kusto
        /// </summary>
        /// <param name="kustoConnectionStringBuilder">The connection string builder to connect to Kusto</param>
        public QueryEngine(KustoConnectionStringBuilder kustoConnectionStringBuilder)
        {
            _adminClient = KustoClientFactory.CreateCslAdminProvider(kustoConnectionStringBuilder);
            _queryClient = KustoClientFactory.CreateCslQueryProvider(kustoConnectionStringBuilder);
            _databaseName = kustoConnectionStringBuilder.InitialCatalog;
            _cluster = kustoConnectionStringBuilder.DataSource;
        }

        /// <summary>
        /// Constructor which creates a connection to the workspace cluster and uses the workspace database to temporarily load the schema
        /// </summary>
        public QueryEngine()
        {
            if (string.IsNullOrEmpty(SettingsWrapper.KustoClusterForTempDatabases)) throw new ArgumentNullException(nameof(SettingsWrapper.KustoClusterForTempDatabases));

            _databaseName = SettingsWrapper.TemporaryKustoDatabase;
            var connString = new KustoConnectionStringBuilder(SettingsWrapper.KustoClusterForTempDatabases)
            {
                FederatedSecurity = true,
                InitialCatalog = _databaseName,
                Authority = SettingsWrapper.AADAuthority
            };

            _adminClient = KustoClientFactory.CreateCslAdminProvider(connString);
            _queryClient = KustoClientFactory.CreateCslQueryProvider(connString);
            _tempDatabaseUsed = true;
            _cluster = connString.DataSource;

            CleanDatabase();
        }

        /// <summary>
        /// Remove any functions and tables that are present in the database. Note that this should only be called when
        /// connecting to the temporary database
        /// </summary>
        public void CleanDatabase()
        {
            if (!_tempDatabaseUsed)
            {
                throw new Exception("CleanDatabase() was called on something other than the temporary database. This method will wipe out the entire database schema and data.");
            }

            var schema = GetDatabaseSchema();
            foreach (var function in schema.Functions)
            {
                string command = CslCommandGenerator.GenerateFunctionDropCommand(function.Value.Name, true);
                _adminClient.ExecuteControlCommand(command);
            }

            foreach (var table in schema.Tables)
            {
                string command = CslCommandGenerator.GenerateTableDropCommand(table.Value.Name, true);
                _adminClient.ExecuteControlCommand(command);
            }
        }

        /// <summary>
        /// Get the full database schema
        /// </summary>
        /// <returns></returns>
        public DatabaseSchema GetDatabaseSchema()
        {
            DatabaseSchema result = null;
            string csl = $@".show database ['{_databaseName}'] schema as json";
            using (IDataReader reader = _adminClient.ExecuteControlCommand(_databaseName, csl))
            {
                reader.Read();
                string json = reader[0].ToString();
                ClusterSchema clusterSchema = JsonConvert.DeserializeObject<ClusterSchema>(json);
                result = clusterSchema.Databases.First().Value;
            }
            foreach (var function in result.Functions.Values)
            {
                if (function.Folder == null)
                {
                    function.Folder = "";
                }
                if (function.DocString == null)
                {
                    function.DocString = "";
                }
            }
            return result;
        }

        /// <summary>
        /// Create or alter the function definition to match what is specified in the command
        /// </summary>
        /// <param name="functionCommand">A create-or-alter function command</param>
        /// <param name="functionName">The name of the function</param>
        public Task CreateOrAlterFunctionAsync(string functionCommand, string functionName)
        {
            return Task.Run(async () =>
            {
                try
                {
                    // If the CSL files on disk were written with an older version of the tool and did not have the skipvalidation paramter, they will fail.
                    // This code will insert the parameter into the script.
                    string skipValidationRegEx = @"skipvalidation[\s]*[=]+[\s""@']*true";
                    if (!Regex.IsMatch(functionCommand, skipValidationRegEx))
                    {
                        string searchString = "(";
                        string replaceString = searchString + "skipvalidation = @'true',";
                        int firstIndexOf = functionCommand.IndexOf(searchString);
                        functionCommand = functionCommand.Substring(0, firstIndexOf) + replaceString + functionCommand.Substring(firstIndexOf + searchString.Length);
                    }
                    await _adminClient.ExecuteControlCommandAsync(_databaseName, functionCommand).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    throw new CreateOrAlterException("Failed to create or alter a function", ex, functionName);
                }
            });
        }

        /// <summary>
        /// Run the create table command. If the table exists, it's schema will be altered. This might result in data loss.
        /// </summary>
        /// <param name="tableCommand">A .create table command string</param>
        /// <param name="tableName">The name of the table</param>
        public Task CreateOrAlterTableAsync(string tableCommand, string tableName)
        {
            return Task.Run(async () =>
            {
                try
                {
                    try
                    {
                        string alterCommand = tableCommand.Replace(".create", ".alter");
                        await _adminClient.ExecuteControlCommandAsync(_databaseName, alterCommand).ConfigureAwait(false);
                    }
                    catch
                    {
                        await _adminClient.ExecuteControlCommandAsync(_databaseName, tableCommand).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    throw new CreateOrAlterException("Failed to create or alter a table", ex, tableName);
                }
            });
        }

        /// <summary>
        /// Remove the specified function
        /// </summary>
        /// <param name="functionSchema">The function to drop</param>
        public void DropFunction(FunctionSchema functionSchema)
        {
            _adminClient.ExecuteControlCommand(_databaseName, $".drop function ['{functionSchema.Name}']");
        }

        /// <summary>
        /// Remove the specified table
        /// </summary>
        /// <param name="tableName">The table to drop</param>
        public void DropTable(string tableName)
        {
            _adminClient.ExecuteControlCommand(_databaseName, $".drop table ['{tableName}']");
        }

        /// <summary>
        /// Dispose of all the resources we created. Note that if this hangs and you're calling from a UI thread, you might
        /// have better luck spinning up the engine in a spearate thread.
        /// </summary>
        public void Dispose()
        {
            _queryClient?.Dispose();
            _adminClient?.Dispose();
        }
    }
}
