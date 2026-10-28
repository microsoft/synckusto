// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using SyncKusto.Core.Abstractions;
using SyncKusto.Core.Exceptions;
using SyncKusto.FileSystem.Exceptions;

namespace SyncKusto.FileSystem.Services;

/// <summary>
/// Resolves user-friendly error messages for file system exceptions
/// </summary>
public class FileSystemErrorMessageResolver : IErrorMessageResolver
{
    public string? ResolveErrorMessage(Exception exception)
    {
        if (SchemaInputException.Find(exception) is { } input)
            return input.Message;

        return exception switch
        {
            SchemaParseException parseEx =>
                $"Failed to parse the following schema objects:\n{string.Join("\n", parseEx.FailedObjects)}\n\nCorrect the definitions and compare again.",

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

            FileSystemSchemaException => "Failed to load schema from file system.",
            DirectoryNotFoundException => "The folder path provided could not be found.",
            FileNotFoundException => "The specified file could not be found.",
            UnauthorizedAccessException => "Access to the file system path was denied.",

            _ => null
        };
    }
}
