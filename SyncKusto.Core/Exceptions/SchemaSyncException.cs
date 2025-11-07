// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace SyncKusto.Core.Exceptions;

/// <summary>
/// Exception thrown when schema synchronization fails
/// </summary>
public class SchemaSyncException : SyncKustoException
{
    public SchemaSyncException(string message) : base(message) { }
    public SchemaSyncException(string message, Exception innerException)
        : base(message, innerException) { }
}
