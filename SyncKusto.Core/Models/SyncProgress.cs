// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace SyncKusto.Core.Models;

/// <summary>
/// Represents progress during schema synchronization operations
/// </summary>
public record SyncProgress(
    string Message,
    int? PercentComplete = null,
    SyncProgressStage Stage = SyncProgressStage.Unknown);

/// <summary>
/// Stages of the synchronization process
/// </summary>
public enum SyncProgressStage
{
    Unknown,
    ValidatingSource,
    ValidatingTarget,
    LoadingSourceSchema,
    LoadingTargetSchema,
    ComparingSchemas,
    SynchronizingSchemas,
    Complete
}
