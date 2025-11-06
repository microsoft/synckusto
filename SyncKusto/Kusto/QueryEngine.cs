// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// This file is kept for backward compatibility but the actual implementation
// is now in SyncKusto.Kusto.Services.QueryEngine
// Consider using the new namespace in new code.

using Kusto.Data;
using Kusto.Data.Common;
using SyncKusto.Core.Models;
using SyncKusto.Kusto.Services;
using System;
using System.Threading.Tasks;

namespace SyncKusto.Kusto
{
    /// <summary>
    /// Central location for interacting with a Kusto cluster
    /// </summary>
    [Obsolete("Use SyncKusto.Kusto.Services.QueryEngine instead")]
    public class QueryEngine : IDisposable
    {
        private readonly SyncKusto.Kusto.Services.QueryEngine _inner;

        /// <summary>
        /// Constructor which gets ready to make queries to Kusto
        /// </summary>
        /// <param name="kustoConnectionStringBuilder">The connection string builder to connect to Kusto</param>
        public QueryEngine(KustoConnectionStringBuilder kustoConnectionStringBuilder)
        {
            _inner = new SyncKusto.Kusto.Services.QueryEngine(kustoConnectionStringBuilder, SettingsWrapper.LineEndingMode);
        }

        /// <summary>
        /// Constructor which creates a connection to the workspace cluster and uses the workspace database to temporarily load the schema
        /// </summary>
        public QueryEngine()
        {
            if (string.IsNullOrEmpty(SettingsWrapper.KustoClusterForTempDatabases)) 
                throw new ArgumentNullException(nameof(SettingsWrapper.KustoClusterForTempDatabases));

            _inner = new SyncKusto.Kusto.Services.QueryEngine(
                SettingsWrapper.KustoClusterForTempDatabases,
                SettingsWrapper.TemporaryKustoDatabase,
                SettingsWrapper.AADAuthority,
                SettingsWrapper.LineEndingMode);
        }

        /// <summary>
        /// Remove any functions and tables that are present in the database. Note that this should only be called when
        /// connecting to the temporary database
        /// </summary>
        public void CleanDatabase() => _inner.CleanDatabase();

        /// <summary>
        /// Get the full database schema
        /// </summary>
        /// <returns></returns>
        public DatabaseSchema GetDatabaseSchema() => _inner.GetDatabaseSchema();

        /// <summary>
        /// Create or alter the function definition to match what is specified in the command
        /// </summary>
        /// <param name="functionCommand">A create-or-alter function command</param>
        /// <param name="functionName">The name of the function</param>
        public Task CreateOrAlterFunctionAsync(string functionCommand, string functionName) =>
            _inner.CreateOrAlterFunctionAsync(functionCommand, functionName);

        /// <summary>
        ///     Run the create table command. If the table exists, it's schema will be altered. This might result in
        ///     data loss.
        /// </summary>
        /// <param name="tableCommand">A .create table command string</param>
        /// <param name="tableName">The name of the table</param>
        /// <param name="createOnly">
        ///     When this is true, the caller is saying that the table doesn't exist yet so we should skip the alter attempt
        /// </param>
        public Task CreateOrAlterTableAsync(string tableCommand, string tableName, bool createOnly = false) =>
            _inner.CreateOrAlterTableAsync(tableCommand, tableName, createOnly);

        /// <summary>
        /// Remove the specified function
        /// </summary>
        /// <param name="functionSchema">The function to drop</param>
        public void DropFunction(FunctionSchema functionSchema) => _inner.DropFunction(functionSchema);

        /// <summary>
        /// Remove the specified table
        /// </summary>
        /// <param name="tableName">The table to drop</param>
        public void DropTable(string tableName) => _inner.DropTable(tableName);

        /// <summary>
        /// Dispose of all the resources we created. Note that if this hangs and you're calling from a UI thread, you might
        /// have better luck spinning up the engine in a spearate thread.
        /// </summary>
        public void Dispose()
        {
            _inner?.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Create a new connection string builder based on the input parameters
        /// </summary>
        /// <param name="cluster">Either the full cluster name or the short one</param>
        /// <param name="database">The name of the database to connect to</param>
        /// <param name="aadClientId">Optionally connect with AAD client app</param>
        /// <param name="aadClientKey">Optional key for AAD client app</param>
        /// <param name="certificateThumbprint">Optional thumbprint of a certificate to use for Subject Name Issuer authentication</param>
        /// <returns>A connection string for accessing Kusto</returns>
        public static KustoConnectionStringBuilder GetKustoConnectionStringBuilder(
            string cluster,
            string database,
            string? aadClientId = null,
            string? aadClientKey = null,
            string? certificateThumbprint = null)
        {
            var factory = new KustoConnectionFactory();
            
            AuthenticationMode authMode = AuthenticationMode.AadFederated;
            if (!string.IsNullOrWhiteSpace(aadClientId) && !string.IsNullOrWhiteSpace(aadClientKey))
            {
                authMode = AuthenticationMode.AadApplication;
            }
            else if (!string.IsNullOrWhiteSpace(aadClientId) && !string.IsNullOrWhiteSpace(certificateThumbprint))
            {
                authMode = AuthenticationMode.AadApplicationSni;
            }

            var options = new Core.Abstractions.KustoConnectionOptions(
                Cluster: cluster,
                Database: database,
                AuthMode: authMode,
                Authority: SettingsWrapper.AADAuthority,
                AppId: aadClientId,
                AppKey: aadClientKey,
                CertificateThumbprint: certificateThumbprint,
                CertificateLocation: SettingsWrapper.CertificateLocation);

            return (KustoConnectionStringBuilder)factory.CreateConnectionString(options);
        }

        /// <summary>
        /// Allow users to specify cluster.eastus2, cluster.eastus2.kusto.windows.net, or https://cluster.eastus2.kusto.windows.net
        /// </summary>
        /// <param name="cluster">Input cluster name</param>
        /// <returns>Normalized cluster name e.g. https://cluster.eastus2.kusto.windows.net</returns>
        public static string NormalizeClusterName(string cluster) =>
            KustoConnectionFactory.NormalizeClusterName(cluster);
    }
}
