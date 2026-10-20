// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace SyncKusto.Core.Models;

/// <summary>
/// Result of a synchronization operation
/// </summary>
public record SyncResult(
    bool Success,
    int ItemsSynchronized,
    IReadOnlyList<string> Errors)
{
    public IReadOnlyList<Exception> Exceptions { get; init; } = Array.Empty<Exception>();

    public static SyncResult Successful(int itemsSynchronized) =>
        new(true, itemsSynchronized, Array.Empty<string>());

    public static SyncResult Failed(IReadOnlyList<string> errors) =>
        new(false, 0, errors);

    public static SyncResult Failed(IReadOnlyList<string> errors, IReadOnlyList<Exception> exceptions) =>
        new(false, 0, errors) { Exceptions = exceptions };
}
