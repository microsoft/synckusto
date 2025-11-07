// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using SyncKusto.Core.Exceptions;

namespace SyncKusto.Kusto.Exceptions;

/// <summary>
/// Exception thrown when user lacks required permissions on a Kusto cluster/database
/// </summary>
public class KustoPermissionException : SchemaLoadException
{
    public string ClusterName { get; }
    public string DatabaseName { get; }

    public KustoPermissionException(string clusterName, string databaseName, string message) 
        : base(message)
    {
        ClusterName = clusterName;
        DatabaseName = databaseName;
    }

    public KustoPermissionException(string clusterName, string databaseName, string message, Exception innerException) 
        : base(message, innerException)
    {
        ClusterName = clusterName;
        DatabaseName = databaseName;
    }
}
