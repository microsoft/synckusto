// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace SyncKusto.Exceptions
{
    /// <summary>
    /// Exception thrown when Kusto database cannot be found or accessed
    /// </summary>
    public class KustoDatabaseException : SchemaLoadException
    {
        public KustoDatabaseException(string message) : base(message) { }
        public KustoDatabaseException(string message, Exception innerException) 
            : base(message, innerException) { }
    }
}
