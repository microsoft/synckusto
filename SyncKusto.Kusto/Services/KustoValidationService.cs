// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;
using SyncKusto.Core.Abstractions;
using SyncKusto.Core.Exceptions;
using SyncKusto.Kusto.Exceptions;

namespace SyncKusto.Kusto.Services;

/// <summary>
/// Service for validating Kusto cluster and database settings
/// </summary>
public class KustoValidationService : IKustoValidationService
{
    /// <summary>
    /// Validate Kusto cluster and database settings
    /// </summary>
    public async Task<string> ValidateKustoSettingsAsync(string clusterName, string databaseName, string? authority = null)
    {
        // Validate cluster name is provided
        if (string.IsNullOrWhiteSpace(clusterName))
        {
            throw new KustoSettingsException("No Kusto cluster was specified.");
        }

        // Normalize cluster name
        clusterName = KustoConnectionFactory.NormalizeClusterName(clusterName);

        // Validate database name is provided
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new KustoSettingsException("No Kusto database was specified.");
        }

        // Verify connection and permissions by creating and removing a test function
        var connString = new KustoConnectionStringBuilder(clusterName)
        {
            FederatedSecurity = true,
            InitialCatalog = databaseName,
            Authority = authority ?? string.Empty
        };

        using var adminClient = KustoClientFactory.CreateCslAdminProvider(connString);

        try
        {
            string functionName = "SyncKustoPermissionsTest" + Guid.NewGuid();
            await Task.Run(() =>
            {
                adminClient.ExecuteControlCommand(
                    CslCommandGenerator.GenerateCreateOrAlterFunctionCommand(
                        functionName,
                        "",
                        "",
                        new Dictionary<string, string>(),
                        "{print now()}"));
                adminClient.ExecuteControlCommand(CslCommandGenerator.GenerateFunctionDropCommand(functionName));
            });
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("403-Forbidden"))
            {
                throw new KustoPermissionException(
                    clusterName,
                    databaseName,
                    $"The current user does not have permission to create a function on cluster('{clusterName}').database('{databaseName}')",
                    ex);
            }
            else if (ex.Message.Contains("failed to resolve the service name"))
            {
                throw new KustoClusterException($"Cluster {clusterName} could not be found.", ex);
            }
            else if (ex.Message.Contains("Kusto client failed to perform authentication"))
            {
                throw new KustoAuthenticationException(
                    "Could not authenticate with Microsoft Entra ID. Please verify that the Microsoft Entra ID Authority is specified correctly.",
                    ex);
            }
            else
            {
                throw new KustoClusterException($"Unknown error validating cluster: {ex.Message}", ex);
            }
        }

        return clusterName;
    }

    /// <summary>
    /// Check if a Kusto database is empty
    /// </summary>
    public async Task<(long functionCount, long tableCount)> CheckDatabaseEmptyAsync(string clusterName, string databaseName, string? authority = null)
    {
        clusterName = KustoConnectionFactory.NormalizeClusterName(clusterName);

        var connString = new KustoConnectionStringBuilder(clusterName)
        {
            FederatedSecurity = true,
            InitialCatalog = databaseName,
            Authority = authority ?? string.Empty
        };

        using var adminClient = KustoClientFactory.CreateCslAdminProvider(connString);

        try
        {
            long functionCount = 0;
            long tableCount = 0;

            await Task.Run(() =>
            {
                using (var functionReader = adminClient.ExecuteControlCommand(".show functions | count"))
                {
                    functionReader.Read();
                    functionCount = functionReader.GetInt64(0);
                }

                using (var tableReader = adminClient.ExecuteControlCommand(".show tables | count"))
                {
                    tableReader.Read();
                    tableCount = tableReader.GetInt64(0);
                }
            });

            if (functionCount != 0 || tableCount != 0)
            {
                throw new KustoDatabaseValidationException(
                    clusterName,
                    databaseName,
                    functionCount,
                    tableCount,
                    $"Database '{databaseName}' on cluster '{clusterName}' is not empty. It contains {functionCount} function(s) and {tableCount} table(s).");
            }

            return (functionCount, tableCount);
        }
        catch (KustoDatabaseValidationException)
        {
            throw; // Re-throw our own exception
        }
        catch (Exception ex)
        {
            throw new KustoClusterException($"Error validating database: {ex.Message}", ex);
        }
    }
}
