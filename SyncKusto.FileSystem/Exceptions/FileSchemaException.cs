// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using SyncKusto.Core.Exceptions;

namespace SyncKusto.FileSystem.Exceptions;

/// <summary>
/// Exception thrown when file system schema operations fail
/// </summary>
public class FileSchemaException : SyncKustoException
{
    public FileSchemaException(string message) : base(message) { }
    public FileSchemaException(string message, Exception innerException)
        : base(message, innerException) { }
}
