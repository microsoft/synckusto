// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace SyncKusto.Core.Abstractions;

/// <summary>
/// Provides access to application settings
/// </summary>
public interface ISettingsProvider
{
    string? GetSetting(string key);
    void SetSetting(string key, string value);
    IEnumerable<string> GetRecentValues(string key);
    void AddRecentValue(string key, string value);
}
