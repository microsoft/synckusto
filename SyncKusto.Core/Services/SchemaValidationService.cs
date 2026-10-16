// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using SyncKusto.Core.Abstractions;
using SyncKusto.Core.Models;

namespace SyncKusto.Core.Services;

/// <summary>
/// Service for validating schema source settings
/// </summary>
public class SchemaValidationService : ISchemaValidationService
{
    private readonly ISettingsProvider _settingsProvider;

    public SchemaValidationService(ISettingsProvider settingsProvider)
    {
        _settingsProvider = settingsProvider ?? throw new ArgumentNullException(nameof(settingsProvider));
    }

    /// <summary>
    /// Validate that source and target settings are configured properly
    /// </summary>
    public ValidationResult ValidateSettings(SchemaSourceInfo source, SchemaSourceInfo target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);

        if (source.SourceType == SourceSelection.FilePath() ||
            target.SourceType == SourceSelection.FilePath())
        {
            var settings = _settingsProvider.GetSyncKustoSettings();
            if (string.IsNullOrWhiteSpace(settings.TempCluster))
            {
                return ValidationResult.Failure(
                    "File system sources require temp cluster configuration. Please configure in Settings.");
            }

            if (string.IsNullOrWhiteSpace(settings.TempDatabase))
            {
                return ValidationResult.Failure(
                    "File system sources require temp database configuration. Please configure in Settings.");
            }
        }

        return ValidationResult.Success();
    }
}
