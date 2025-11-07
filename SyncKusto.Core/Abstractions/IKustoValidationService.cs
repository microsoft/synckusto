// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace SyncKusto.Core.Abstractions;

/// <summary>
/// Service for validating Kusto cluster and database settings
/// </summary>
public interface IKustoValidationService
{
    /// <summary>
    /// Validate Kusto cluster and database settings
    /// </summary>
    /// <param name="clusterName">Kusto cluster name</param>
    /// <param name="databaseName">Kusto database name</param>
    /// <param name="authority">AAD authority (optional)</param>
    /// <returns>Normalized cluster name</returns>
    /// <exception cref="Core.Exceptions.KustoSettingsException">Thrown when cluster or database name is missing</exception>
    /// <exception cref="Kusto.Exceptions.KustoPermissionException">Thrown when user lacks required permissions</exception>
    /// <exception cref="Kusto.Exceptions.KustoClusterException">Thrown when cluster cannot be found</exception>
    /// <exception cref="Kusto.Exceptions.KustoAuthenticationException">Thrown when authentication fails</exception>
    Task<string> ValidateKustoSettingsAsync(string clusterName, string databaseName, string? authority = null);

    /// <summary>
    /// Check if a Kusto database is empty
    /// </summary>
    /// <param name="clusterName">Kusto cluster name</param>
    /// <param name="databaseName">Kusto database name</param>
    /// <param name="authority">AAD authority (optional)</param>
    /// <returns>Task returning tuple of (functionCount, tableCount)</returns>
    /// <exception cref="Kusto.Exceptions.KustoDatabaseValidationException">Thrown when database is not empty (contains validation info)</exception>
    Task<(long functionCount, long tableCount)> CheckDatabaseEmptyAsync(string clusterName, string databaseName, string? authority = null);
}
