// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace SyncKusto.Core.Exceptions;

/// <summary>
/// An actionable schema input failure, safe to display without exposing the file contents.
/// </summary>
public sealed class SchemaInputException : SchemaLoadException
{
    public string Side { get; }
    public string RelativePath { get; }
    public int? Line { get; }
    public int? Column { get; }

    public SchemaInputException(string side, string relativePath, string reason, string hint,
        Exception innerException, int? line = null, int? column = null)
        : base($"{side} file '{relativePath}'" +
            (line.HasValue && column.HasValue ? $" (line {line}, column {column})" : "") +
            $" {reason}. {hint}", innerException)
    {
        Side = side;
        RelativePath = relativePath;
        Line = line;
        Column = column;
    }

    public static SchemaInputException? Find(Exception exception)
    {
        if (exception is SchemaInputException input)
            return input;
        if (exception is AggregateException aggregate)
        {
            foreach (var inner in aggregate.InnerExceptions)
                if (Find(inner) is { } found)
                    return found;
            return null;
        }

        return exception.InnerException is { } cause ? Find(cause) : null;
    }
}
