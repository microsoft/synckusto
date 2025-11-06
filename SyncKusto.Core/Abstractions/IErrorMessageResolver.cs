// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace SyncKusto.Core.Abstractions;

/// <summary>
/// Resolves user-friendly error messages from exceptions
/// </summary>
public interface IErrorMessageResolver
{
    string? ResolveErrorMessage(Exception exception);
}
