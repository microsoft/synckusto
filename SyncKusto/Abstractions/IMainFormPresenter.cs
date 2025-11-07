// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SyncKusto.Core.Abstractions;
using SyncKusto.Core.Models;

namespace SyncKusto.Abstractions;

/// <summary>
/// Presenter for the main form, orchestrating schema comparison and synchronization
/// </summary>
public interface IMainFormPresenter
{
    /// <summary>
    /// Compare source and target schemas
    /// </summary>
    Task<ComparisonResult> CompareAsync(
        SchemaSourceInfo source,
        SchemaSourceInfo target,
        IProgress<SyncProgress>? progress = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Synchronize selected differences
    /// </summary>
    Task<SyncResult> SynchronizeAsync(
        IEnumerable<SchemaDifference> selectedDifferences,
        IProgress<SyncProgress>? progress = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validate that settings are configured properly
    /// </summary>
    ValidationResult ValidateSettings(SchemaSourceInfo source, SchemaSourceInfo target);
}
