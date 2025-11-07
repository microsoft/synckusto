// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using SyncKusto.Core.Models;

namespace SyncKusto.Core.Abstractions;

/// <summary>
/// Service for validating schema source settings
/// </summary>
public interface ISchemaValidationService
{
    /// <summary>
    /// Validate that source and target settings are configured properly
    /// </summary>
    /// <param name="source">Source schema information</param>
    /// <param name="target">Target schema information</param>
    /// <returns>Validation result indicating success or failure with error message</returns>
    ValidationResult ValidateSettings(SchemaSourceInfo source, SchemaSourceInfo target);
}
