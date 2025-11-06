// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// This file is kept for backward compatibility but the actual implementation
// is now in SyncKusto.Kusto.Services.KustoErrorMessageResolver
// Consider using the new namespace in new code.

using System;
using SyncKusto.Core.Abstractions;

namespace SyncKusto.ErrorHandling
{
    /// <summary>
    /// Resolves error messages for Kusto-related exceptions
    /// </summary>
    [Obsolete("Use SyncKusto.Kusto.Services.KustoErrorMessageResolver instead")]
    public class KustoErrorMessageResolver : IErrorMessageResolver
    {
        private readonly SyncKusto.Kusto.Services.KustoErrorMessageResolver _inner = new();

        public string? ResolveErrorMessage(Exception exception) => _inner.ResolveErrorMessage(exception);
    }
}
