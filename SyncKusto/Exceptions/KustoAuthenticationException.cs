// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// This file is kept for backward compatibility but the actual implementation
// is now in SyncKusto.Kusto.Exceptions.KustoAuthenticationException
// Consider using the new namespace in new code.

using System;

namespace SyncKusto.Exceptions
{
    /// <summary>
    /// Exception thrown when authentication to Kusto fails
    /// </summary>
    [Obsolete("Use SyncKusto.Kusto.Exceptions.KustoAuthenticationException instead")]
    public class KustoAuthenticationException : SyncKusto.Kusto.Exceptions.KustoAuthenticationException
    {
        public KustoAuthenticationException(string message) : base(message) { }
        public KustoAuthenticationException(string message, Exception innerException) 
            : base(message, innerException) { }
    }
}
