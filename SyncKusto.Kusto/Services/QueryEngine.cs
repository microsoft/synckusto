// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;
using Newtonsoft.Json;
using SyncKusto.Core.Models;
using SyncKusto.Kusto.Exceptions;
using System.Data;
using System.Text.RegularExpressions;

namespace SyncKusto.Kusto.Services;

/// <summary>
/// Central service for interacting with a Kusto cluster
/// </summary>
public class QueryEngine : IDisposable
{
    private readonly ICslAdminProvider _adminClient;
    private readonly ICslQueryProvider _queryClient;
    private readonly string _databaseName;
    private readonly string _cluster;
    private readonly bool _tempDatabaseUsed;
    private readonly LineEndingMode _lineEndingMode;

    /// <summary>
    /// Constructor which gets ready to make queries to Kusto
    /// </summary>
    /// <param name="kustoConnectionStringBuilder">The connection string builder to connect to Kusto</param>
    /// <param name="lineEndingMode">The line ending mode to use for function bodies</param>
    public QueryEngine(
        KustoConnectionStringBuilder kustoConnectionStringBuilder,
        LineEndingMode lineEndingMode = LineEndingMode.LeaveAsIs)
    {
        ArgumentNullException.ThrowIfNull(kustoConnectionStringBuilder);

        _adminClient = KustoClientFactory.CreateCslAdminProvider(kustoConnectionStringBuilder);
        _queryClient = KustoClientFactory.CreateCslQueryProvider(kustoConnectionStringBuilder);
        _databaseName = kustoConnectionStringBuilder.InitialCatalog;
        _cluster = kustoConnectionStringBuilder.DataSource;
        _tempDatabaseUsed = false;
        _lineEndingMode = lineEndingMode;
    }

    /// <summary>
    /// Constructor for temporary database operations
    /// </summary>
    /// <param name="tempCluster">The cluster containing the temporary database</param>
    /// <param name="tempDatabase">The name of the temporary database</param>
    /// <param name="authority">The AAD authority</param>
    /// <param name="lineEndingMode">The line ending mode to use for function bodies</param>
    public QueryEngine(
        string tempCluster,
        string tempDatabase,
        string authority,
        LineEndingMode lineEndingMode = LineEndingMode.LeaveAsIs)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tempCluster);
        ArgumentException.ThrowIfNullOrWhiteSpace(tempDatabase);
        ArgumentException.ThrowIfNullOrWhiteSpace(authority);

        _databaseName = tempDatabase;

        var connString = new KustoConnectionStringBuilder(KustoConnectionFactory.NormalizeClusterName(tempCluster))
        {
            FederatedSecurity = true,
            InitialCatalog = _databaseName,
            Authority = authority
        };

        _adminClient = KustoClientFactory.CreateCslAdminProvider(connString);
        _queryClient = KustoClientFactory.CreateCslQueryProvider(connString);
        _tempDatabaseUsed = true;
        _cluster = connString.DataSource;
        _lineEndingMode = lineEndingMode;

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
            throw new InvalidOperationException("CleanDatabase() was called on something other than the temporary database.");
        }

        var schema = GetDatabaseSchema();

        if (schema.Functions.Count > 0)
        {
            _adminClient.ExecuteControlCommand(
                CslCommandGenerator.GenerateFunctionsDropCommand(
                    schema.Functions.Select(f => f.Value.Name), ifExists: true));
        }

        if (schema.Tables.Count > 0)
        {
            _adminClient.ExecuteControlCommand(
                CslCommandGenerator.GenerateTablesDropCommand(
                    schema.Tables.Select(f => f.Value.Name), ifExists: true));
        }
    }

    /// <summary>
    /// Get the full database schema
    /// </summary>
    /// <returns>The database schema</returns>
    /// <exception cref="InvalidOperationException">Thrown when schema cannot be retrieved</exception>
    public DatabaseSchema GetDatabaseSchema()
    {
        DatabaseSchema? result = null;
        string csl = $@".show database ['{_databaseName}'] schema as json";

        using (IDataReader reader = _adminClient.ExecuteControlCommand(_databaseName, csl))
        {
            reader.Read();
            string? json = reader[0].ToString();
            if (json == null)
            {
                throw new InvalidOperationException("Failed to retrieve schema JSON from Kusto");
            }

            ClusterSchema? clusterSchema = JsonConvert.DeserializeObject<ClusterSchema>(json);
            if (clusterSchema?.Databases == null || !clusterSchema.Databases.Any())
            {
                throw new InvalidOperationException("Failed to deserialize cluster schema or no databases found");
            }

            result = clusterSchema.Databases.First().Value;
        }

        if (result == null)
        {
            throw new InvalidOperationException("Failed to load database schema");
        }

        // Normalize function bodies based on line ending mode
        foreach (var function in result.Functions.Values)
        {
            function.Body = NormalizeLineEndings(function.Body, _lineEndingMode);
            function.Folder ??= "";
            function.DocString ??= "";
        }

        return result;
    }

    /// <summary>
    /// Create or alter the function definition to match what is specified in the command
    /// </summary>
    /// <param name="functionCommand">A create-or-alter function command</param>
    /// <param name="functionName">The name of the function</param>
    public async Task CreateOrAlterFunctionAsync(string functionCommand, string functionName)
    {
        try
        {
            // If the CSL files on disk were written with an older version of the tool and did not have the skipvalidation parameter, they will fail.
            // This code will insert the parameter into the script.
            string skipValidationRegEx = @"skipvalidation[\s]*[=]+[\s""@']*true";
            if (!Regex.IsMatch(functionCommand, skipValidationRegEx))
            {
                string searchString = "(";
                string replaceString = searchString + "skipvalidation = @'true',";
                int firstIndexOf = functionCommand.IndexOf(searchString, StringComparison.Ordinal);
                functionCommand = functionCommand.Substring(0, firstIndexOf) + replaceString +
                    functionCommand.Substring(firstIndexOf + searchString.Length);
            }

            await _adminClient.ExecuteControlCommandAsync(_databaseName, functionCommand).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new CreateOrAlterException("Failed to create or alter a function", ex, functionName);
        }
    }

    /// <summary>
    /// Run the create table command. If the table exists, its schema will be altered. This might result in data loss.
    /// </summary>
    /// <param name="tableCommand">A .create table command string</param>
    /// <param name="tableName">The name of the table</param>
    /// <param name="createOnly">
    ///     When this is true, the caller is saying that the table doesn't exist yet so we should skip the alter attempt
    /// </param>
    public async Task CreateOrAlterTableAsync(string tableCommand, string tableName, bool createOnly = false)
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
    }

    /// <summary>
    /// Remove the specified function
    /// </summary>
    /// <param name="functionSchema">The function to drop</param>
    public void DropFunction(FunctionSchema functionSchema)
    {
        ArgumentNullException.ThrowIfNull(functionSchema);
        _adminClient.ExecuteControlCommand(_databaseName, $".drop function ['{functionSchema.Name}']");
    }

    /// <summary>
    /// Remove the specified table
    /// </summary>
    /// <param name="tableName">The table to drop</param>
    public void DropTable(string tableName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        _adminClient.ExecuteControlCommand(_databaseName, $".drop table ['{tableName}']");
    }

    /// <summary>
    /// Dispose of all the resources we created.
    /// </summary>
    public void Dispose()
    {
        _queryClient?.Dispose();
        _adminClient?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Normalizes line endings in the text based on the specified mode
    /// </summary>
    private static string NormalizeLineEndings(string text, LineEndingMode mode)
    {
        return mode switch
        {
            LineEndingMode.WindowsStyle => Regex.Replace(text, @"\r\n|\r|\n", "\r\n"),
            LineEndingMode.UnixStyle => Regex.Replace(text, @"\r\n|\r|\n", "\n"),
            LineEndingMode.LeaveAsIs => text,
            _ => text
        };
    }
}
