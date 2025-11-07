// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using SyncKusto.Core.Abstractions;
using SyncKusto.Core.Models;

namespace SyncKusto.Adapters;

/// <summary>
/// Adapter that wraps SchemaPickerControl to implement ISchemaSourceSelector
/// </summary>
public class SchemaSourceSelectorAdapter : ISchemaSourceSelector
{
    private readonly SchemaPickerControl _control;

    public SchemaSourceSelectorAdapter(SchemaPickerControl control)
    {
        _control = control ?? throw new ArgumentNullException(nameof(control));
    }

    /// <summary>
    /// Get schema source information from the control
    /// </summary>
    public SchemaSourceInfo GetSourceInfo()
    {
        return _control.GetSourceInfo();
    }
    
    /// <summary>
    /// Validate the current source configuration
    /// </summary>
    public ValidationResult Validate()
    {
        return _control.IsValid() 
            ? ValidationResult.Success() 
            : ValidationResult.Failure("Invalid source configuration");
    }
    
    /// <summary>
    /// Report progress to the user
    /// </summary>
    public void ReportProgress(string message)
    {
        _control.ReportProgress(message);
    }
    
    /// <summary>
    /// Save recent values for next session
    /// </summary>
    public void SaveRecentValues()
    {
        _control.SaveRecentValues();
    }
    
    /// <summary>
    /// Reload recent values
    /// </summary>
    public void ReloadRecentValues()
    {
        _control.ReloadRecentValues();
    }
}
