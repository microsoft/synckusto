// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using SyncKusto.Core.Models;

namespace SyncKusto.Core.Abstractions;

/// <summary>
/// Abstracts schema source selection from a UI control
/// </summary>
public interface ISchemaSourceSelector
{
    /// <summary>
    /// Get schema source information from the control
    /// </summary>
    SchemaSourceInfo GetSourceInfo();

    /// <summary>
    /// Validate the current source configuration
    /// </summary>
    ValidationResult Validate();

    /// <summary>
    /// Report progress to the user
    /// </summary>
    void ReportProgress(string message);

    /// <summary>
    /// Save recent values for next session
    /// </summary>
    void SaveRecentValues();

    /// <summary>
    /// Reload recent values
    /// </summary>
    void ReloadRecentValues();
}
