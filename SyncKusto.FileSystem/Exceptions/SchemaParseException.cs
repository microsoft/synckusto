// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace SyncKusto.FileSystem.Exceptions;

/// <summary>
/// Exception thrown when schema files cannot be parsed
/// </summary>
public class SchemaParseException : FileSchemaException
{
    /// <summary>
    /// List of files or objects that failed to parse
    /// </summary>
    public IReadOnlyList<string> FailedObjects { get; }

    public SchemaParseException(string message, IReadOnlyList<string> failedObjects)
        : base(message)
    {
        FailedObjects = failedObjects;
    }

    public SchemaParseException(string message, IReadOnlyList<string> failedObjects, Exception innerException)
        : base(message, innerException)
    {
        FailedObjects = failedObjects;
    }
}
