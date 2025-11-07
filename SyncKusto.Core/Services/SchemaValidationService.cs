// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using SyncKusto.Core.Abstractions;
using SyncKusto.Core.Configuration;
using SyncKusto.Core.Models;

namespace SyncKusto.Core.Services;

/// <summary>
/// Service for validating schema source settings
/// </summary>
public class SchemaValidationService : ISchemaValidationService
{
    private readonly SyncKustoSettings _settings;

    public SchemaValidationService(SyncKustoSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <summary>
    /// Validate that source and target settings are configured properly
    /// </summary>
    public ValidationResult ValidateSettings(SchemaSourceInfo source, SchemaSourceInfo target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);

        // Using the local file system for either the source or the target requires access to a temp cluster
        if ((source.SourceType == SourceSelection.FilePath() || 
             target.SourceType == SourceSelection.FilePath()) &&
            string.IsNullOrWhiteSpace(_settings.TempCluster))
        {
            return ValidationResult.Failure(
                "File system sources require temp cluster configuration. Please configure in Settings.");
        }
        
        return ValidationResult.Success();
    }
}
