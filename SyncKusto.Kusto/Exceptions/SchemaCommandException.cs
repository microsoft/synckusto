// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace SyncKusto.Kusto.Exceptions;

public class SchemaCommandException : Exception
{
    public string Hint { get; }
    public int? Line { get; }
    public int? Column { get; }

    public SchemaCommandException(string reason, string hint, int? line = null, int? column = null)
        : base(reason)
    {
        Hint = hint;
        Line = line;
        Column = column;
    }
}
