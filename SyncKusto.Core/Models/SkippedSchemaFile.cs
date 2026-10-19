// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace SyncKusto.Core.Models;

public sealed record SkippedSchemaFile(
    string Side,
    string RelativePath,
    string Reason,
    string Hint,
    int? Line = null,
    int? Column = null)
{
    public string Message => $"{Side} file '{RelativePath}'" +
        (Line.HasValue && Column.HasValue ? $" (line {Line}, column {Column})" : "") +
        $" {Reason}. {Hint}";
}
