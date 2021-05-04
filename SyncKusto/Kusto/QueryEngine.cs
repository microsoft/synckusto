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
                throw new Exception("CleanDatabase() was called on something other than the temporary database.");
            }

            var schema = GetDatabaseSchema();

            if (schema.Functions.Count > 0)
            {
                _adminClient.ExecuteControlCommand(
                    CslCommandGenerator.GenerateFunctionsDropCommand(
                        schema.Functions.Select(f => f.Value.Name), true));
            }

            if (schema.Tables.Count > 0)
            {
                _adminClient.ExecuteControlCommand(
                    CslCommandGenerator.GenerateTablesDropCommand(
                        schema.Tables.Select(f => f.Value.Name), true));
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
        ///     Run the create table command. If the table exists, it's schema will be altered. This might result in
        ///     data loss.
        /// </summary>
        /// <param name="tableCommand">A .create table command string</param>
        /// <param name="tableName">The name of the table</param>
        /// <param name="createOnly">
        ///     When this is true, the caller is saying that the table doesn't exist yet so we should skip the alter attempt
        /// </param>
        public Task CreateOrAlterTableAsync(string tableCommand, string tableName, bool createOnly = false)
        {
            return Task.Run(async () =>
            {
                string createCommand = tableCommand;
                string alterCommand = tableCommand.Replace(".create", ".alter");

                try
                {
                    if (createOnly)
                    {
                        await _adminClient.ExecuteControlCommandAsync(_databaseName, createCommand).ConfigureAwait(false);
                    }
                    else
                    {
                        try
                        {
                            await _adminClient.ExecuteControlCommandAsync(_databaseName, alterCommand).ConfigureAwait(false);
                        }
                        catch
                        {
                            await _adminClient.ExecuteControlCommandAsync(_databaseName, createCommand).ConfigureAwait(false);
                        }
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

        /// <summary>
        /// Create a new connection string builder based on the input parameters
        /// </summary>
        /// <param name="cluster">Either the full cluster name or the short one</param>
        /// <param name="database">The name of the database to connect to</param>
        /// <param name="aadClientId">Optionally connect with AAD client app</param>
        /// <param name="aadClientKey">Optional key for AAD client app</param>
        /// <returns>A connection string for accessing Kusto</returns>
        public static KustoConnectionStringBuilder GetKustoConnectionStringBuilder(string cluster, string database, string aadClientId = null, string aadClientKey = null)
        {
            if (string.IsNullOrEmpty(aadClientId) != string.IsNullOrEmpty(aadClientKey))
            {
                throw new ArgumentException("If either aadClientId or aadClientKey are specified, they must both be specified.");
            }

            cluster = NormalizeClusterName(cluster);

            var kcsb = new KustoConnectionStringBuilder(cluster)
            {
                FederatedSecurity = true,
                InitialCatalog = database,
                Authority = SettingsWrapper.AADAuthority
            };
            if (!string.IsNullOrWhiteSpace(aadClientId) && !string.IsNullOrWhiteSpace(aadClientKey))
            {
                kcsb.ApplicationKey = aadClientKey;
                kcsb.ApplicationClientId = aadClientId;
            }

            return kcsb;
        }

        /// <summary>
        /// Allow users to specify cluster.eastus2, cluster.eastus2.kusto.windows.net, or https://cluster.eastus2.kusto.windows.net 
        /// </summary>
        /// <param name="cluster">Input cluster name</param>
        /// <returns>Normalized cluster name e.g. https://cluster.eastus2.kusto.windows.net</returns>
        public static string NormalizeClusterName(string cluster)
        {
            if (cluster.StartsWith("https://"))
            {
                // If it starts with https, take it verbatim and return from the function
                return cluster;
            }
            else
            {
                // Trim any spaces and trailing '/'
                cluster = cluster.TrimEnd('/').Trim();

                // If it doesn't end with .com or .net then default to .kusto.windows.net
                if (!cluster.EndsWith(".com") && !cluster.EndsWith(".net"))
                {
                    cluster = $@"https://{cluster}.kusto.windows.net";
                }

                // Make sure it starts with https
                if (!cluster.StartsWith("https://"))
                {
                    cluster = $"https://{cluster}";
                }

                return cluster;
            }
        }
    }
}
