// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace SyncKusto.Kusto.Exceptions;

public sealed class SchemaParserCompatibilityException : SchemaCommandException
{
    public SchemaParserCompatibilityException()
        : base("cannot be read by this version of the schema parser",
            "Update Sync Kusto or report this compatibility issue; the definition may already be valid.")
    {
    }
}
