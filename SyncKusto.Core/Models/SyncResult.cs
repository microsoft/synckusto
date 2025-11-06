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
    public static SyncResult Successful(int itemsSynchronized) =>
        new(true, itemsSynchronized, Array.Empty<string>());
    
    public static SyncResult Failed(IReadOnlyList<string> errors) =>
        new(false, 0, errors);
}
