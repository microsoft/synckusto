// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Kusto.Data.Common;

namespace SyncKusto.Core.Abstractions;

public interface ISchemaSnapshotSource
{
    ISchemaSnapshot CaptureSnapshot(string side, CancellationToken cancellationToken = default);
}

/// <summary>
/// A frozen file selection. Validate all snapshots before loading any of them.
/// </summary>
public interface ISchemaSnapshot
{
    void Validate();
    Task<DatabaseSchema> GetSchemaAsync(CancellationToken cancellationToken = default);
}
