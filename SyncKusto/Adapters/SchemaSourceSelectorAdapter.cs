// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using SyncKusto.Core.Abstractions;
using SyncKusto.Core.Models;
using Kusto.Data;

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
        if (_control.SourceSelection == SourceSelection.FilePath())
        {
            return new SchemaSourceInfo(
                SourceSelection.FilePath(),
                FilePath: _control.SourceFilePath);
        }
        else if (_control.SourceSelection == SourceSelection.Kusto())
        {
            var connection = _control.KustoConnection;
            
            // Extract connection info from the KustoConnectionStringBuilder
            var kustoInfo = new KustoConnectionInfo(
                Cluster: connection.DataSource,
                Database: connection.InitialCatalog,
                AuthMode: GetAuthenticationMode(connection),
                Authority: connection.Authority ?? "",
                AppId: connection.ApplicationClientId,
                AppKey: connection.ApplicationKey,
                CertificateThumbprint: connection.ApplicationCertificateThumbprint);
            
            return new SchemaSourceInfo(
                SourceSelection.Kusto(),
                KustoInfo: kustoInfo);
        }
        else
        {
            throw new InvalidOperationException($"Unknown source selection type: {_control.SourceSelection}");
        }
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

    private AuthenticationMode GetAuthenticationMode(KustoConnectionStringBuilder connection)
    {
        // Determine auth mode based on what's set in the connection string
        if (!string.IsNullOrWhiteSpace(connection.ApplicationClientId))
        {
            if (!string.IsNullOrWhiteSpace(connection.ApplicationKey))
            {
                return AuthenticationMode.AadApplication;
            }
            else if (!string.IsNullOrWhiteSpace(connection.ApplicationCertificateThumbprint))
            {
                return AuthenticationMode.AadApplicationSni;
            }
        }
        
        // Default to user authentication
        return AuthenticationMode.AadFederated;
    }
}
