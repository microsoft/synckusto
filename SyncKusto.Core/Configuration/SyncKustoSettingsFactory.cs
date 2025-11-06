// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using SyncKusto.Core.Abstractions;
using SyncKusto.Core.Models;

namespace SyncKusto.Core.Configuration;

/// <summary>
/// Factory for creating SyncKustoSettings from ISettingsProvider
/// </summary>
public static class SyncKustoSettingsFactory
{
    /// <summary>
    /// Create SyncKustoSettings from the settings provider
    /// </summary>
    public static SyncKustoSettings CreateFromProvider(ISettingsProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        return new SyncKustoSettings
        {
            TempCluster = provider.GetSetting("TempCluster") ?? string.Empty,
            TempDatabase = provider.GetSetting("TempDatabase") ?? string.Empty,
            AADAuthority = provider.GetSetting("AADAuthority"),
            KustoObjectDropWarning = ParseBool(provider.GetSetting("KustoObjectDropWarning"), true),
            TableFieldsOnNewLine = ParseBool(provider.GetSetting("TableFieldsOnNewLine"), false),
            CreateMergeEnabled = ParseBool(provider.GetSetting("CreateMergeEnabled"), false),
            UseLegacyCslExtension = ParseBool(provider.GetSetting("UseLegacyCslExtension"), true),
            LineEndingMode = ParseLineEndingMode(provider.GetSetting("LineEndingMode")),
            CertificateLocation = ParseStoreLocation(provider.GetSetting("CertificateLocation"))
        };
    }

    private static bool ParseBool(string? value, bool defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return bool.TryParse(value, out var result) ? result : defaultValue;
    }

    private static LineEndingMode ParseLineEndingMode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return LineEndingMode.LeaveAsIs;
        }

        if (int.TryParse(value, out var intValue) && 
            Enum.IsDefined(typeof(LineEndingMode), intValue))
        {
            return (LineEndingMode)intValue;
        }

        if (Enum.TryParse<LineEndingMode>(value, out var result))
        {
            return result;
        }

        return LineEndingMode.LeaveAsIs;
    }

    private static StoreLocation ParseStoreLocation(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return StoreLocation.CurrentUser;
        }

        return Enum.TryParse<StoreLocation>(value, out var result)
            ? result
            : StoreLocation.CurrentUser;
    }
}
