// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using SyncKusto.Core.Exceptions;
using System;

namespace SyncKusto.Exceptions
{
    /// <summary>
    /// Exception thrown when authentication to Kusto fails
    /// </summary>
    public class KustoAuthenticationException : SchemaLoadException
    {
        public KustoAuthenticationException(string message) : base(message) { }
        public KustoAuthenticationException(string message, Exception innerException) 
            : base(message, innerException) { }
    }
}
