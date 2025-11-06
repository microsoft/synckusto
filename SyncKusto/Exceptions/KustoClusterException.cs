// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// This file is kept for backward compatibility but the actual implementation
// is now in SyncKusto.Kusto.Exceptions.KustoClusterException
// Consider using the new namespace in new code.

using System;

namespace SyncKusto.Exceptions
{
    /// <summary>
    /// Exception thrown when Kusto cluster cannot be found or accessed
    /// </summary>
    [Obsolete("Use SyncKusto.Kusto.Exceptions.KustoClusterException instead")]
    public class KustoClusterException : SyncKusto.Kusto.Exceptions.KustoClusterException
    {
        public KustoClusterException(string message) : base(message) { }
        public KustoClusterException(string message, Exception innerException) 
            : base(message, innerException) { }
    }
}
