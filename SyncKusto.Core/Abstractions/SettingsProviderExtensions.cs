// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using SyncKusto.Core.Configuration;

namespace SyncKusto.Core.Abstractions;

/// <summary>
/// Extension methods for ISettingsProvider
/// </summary>
public static class SettingsProviderExtensions
{
    /// <summary>
    /// Get the settings as a SyncKustoSettings object
    /// </summary>
    public static SyncKustoSettings GetSyncKustoSettings(this ISettingsProvider provider)
    {
        return SyncKustoSettingsFactory.CreateFromProvider(provider);
    }
}
