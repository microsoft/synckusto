// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace SyncKusto.Exceptions
{
    /// <summary>
    /// Exception thrown when Kusto cluster cannot be found or accessed
    /// </summary>
    public class KustoClusterException : SchemaLoadException
    {
        public KustoClusterException(string message) : base(message) { }
        public KustoClusterException(string message, Exception innerException) 
            : base(message, innerException) { }
    }
}
