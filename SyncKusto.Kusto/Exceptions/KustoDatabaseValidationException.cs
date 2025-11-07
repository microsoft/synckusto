// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using SyncKusto.Core.Exceptions;

namespace SyncKusto.Kusto.Exceptions;

/// <summary>
/// Exception thrown when a Kusto database validation fails
/// </summary>
public class KustoDatabaseValidationException : SchemaValidationException
{
    public string ClusterName { get; }
    public string DatabaseName { get; }
    public long FunctionCount { get; }
    public long TableCount { get; }

    public KustoDatabaseValidationException(
        string clusterName, 
        string databaseName,
        long functionCount,
        long tableCount,
        string message) 
        : base(message, new List<string> { $"Functions: {functionCount}", $"Tables: {tableCount}" })
    {
        ClusterName = clusterName;
        DatabaseName = databaseName;
        FunctionCount = functionCount;
        TableCount = tableCount;
    }
}
