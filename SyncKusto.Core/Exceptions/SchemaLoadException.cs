// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace SyncKusto.Core.Exceptions;

/// <summary>
/// Exception thrown when a schema cannot be loaded
/// </summary>
public class SchemaLoadException : SyncKustoException
{
    public SchemaLoadException(string message) : base(message) { }
    public SchemaLoadException(string message, Exception innerException) 
        : base(message, innerException) { }
}
