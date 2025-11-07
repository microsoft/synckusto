// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace SyncKusto.Core.Exceptions;

/// <summary>
/// Exception thrown when Kusto settings are invalid or incomplete
/// </summary>
public class KustoSettingsException : SchemaValidationException
{
    public KustoSettingsException(string message)
        : base(message, new List<string>())
    {
    }

    public KustoSettingsException(string message, IReadOnlyList<string> validationErrors)
        : base(message, validationErrors)
    {
    }
}
