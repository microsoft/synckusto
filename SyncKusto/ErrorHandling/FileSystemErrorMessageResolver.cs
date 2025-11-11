// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using SyncKusto.Core.Abstractions;
using SyncKusto.FileSystem.Exceptions;
using System;
using System.IO;

namespace SyncKusto.ErrorHandling
{
    /// <summary>
    /// Resolves error messages for file system-related exceptions
    /// </summary>
    public class FileSystemErrorMessageResolver : IErrorMessageResolver
    {
        public string? ResolveErrorMessage(Exception exception)
        {
            return exception switch
            {
                FileSystemSchemaException => "Failed to load schema from file system.",
                DirectoryNotFoundException => "The folder path provided could not be found.",
                FileNotFoundException => "The specified file could not be found.",
                UnauthorizedAccessException => "Access to the file system path was denied.",
                _ => null
            };
        }
    }
}
