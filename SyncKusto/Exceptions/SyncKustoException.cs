// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace SyncKusto.Exceptions
{
    /// <summary>
    /// Base exception for all SyncKusto operations
    /// </summary>
    public abstract class SyncKustoException : Exception
    {
        protected SyncKustoException(string message) : base(message) { }
        protected SyncKustoException(string message, Exception innerException) 
            : base(message, innerException) { }
    }
}
