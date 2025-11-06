// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace SyncKusto.ErrorHandling
{
    /// <summary>
    /// Resolves user-friendly error messages from exceptions
    /// </summary>
    public interface IErrorMessageResolver
    {
        /// <summary>
        /// Attempts to resolve a user-friendly error message from the exception.
        /// Returns null if this resolver cannot handle the exception.
        /// </summary>
        string? ResolveErrorMessage(Exception exception);
    }
}
