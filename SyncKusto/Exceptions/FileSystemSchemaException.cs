// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using SyncKusto.Core.Exceptions;
using System;

namespace SyncKusto.Exceptions
{
    /// <summary>
    /// Exception thrown when file system operations fail
    /// </summary>
    public class FileSystemSchemaException : SchemaLoadException
    {
        public FileSystemSchemaException(string message) : base(message) { }
        public FileSystemSchemaException(string message, Exception innerException) 
            : base(message, innerException) { }
    }
}
