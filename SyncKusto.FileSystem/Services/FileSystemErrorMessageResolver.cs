// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using SyncKusto.Core.Abstractions;
using SyncKusto.FileSystem.Exceptions;

namespace SyncKusto.FileSystem.Services;

/// <summary>
/// Resolves user-friendly error messages for file system exceptions
/// </summary>
public class FileSystemErrorMessageResolver : IErrorMessageResolver
{
    public string? ResolveErrorMessage(Exception exception)
    {
        return exception switch
        {
            SchemaParseException parseEx =>
                $"Failed to parse the following schema objects:\n{string.Join("\n", parseEx.FailedObjects)}\n\nThese objects will be ignored.",

            FileSchemaException fileEx when fileEx.InnerException is UnauthorizedAccessException =>
                $"Access denied to file system: {fileEx.Message}",

            FileSchemaException fileEx when fileEx.InnerException is DirectoryNotFoundException =>
                $"Directory not found: {fileEx.Message}",

            FileSchemaException fileEx when fileEx.InnerException is FileNotFoundException =>
                $"File not found: {fileEx.Message}",

            FileSchemaException fileEx when fileEx.InnerException is IOException =>
                $"File system I/O error: {fileEx.Message}",

            FileSchemaException fileEx =>
                $"File system error: {fileEx.Message}",

            _ => null
        };
    }
}
