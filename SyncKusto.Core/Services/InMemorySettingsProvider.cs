// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using SyncKusto.Core.Abstractions;
using System.Collections.Concurrent;

namespace SyncKusto.Core.Services;

/// <summary>
/// In-memory implementation of ISettingsProvider for testing
/// </summary>
public class InMemorySettingsProvider : ISettingsProvider
{
    private readonly ConcurrentDictionary<string, string> _settings = new();
    private readonly ConcurrentDictionary<string, List<string>> _recentValues = new();

    public string? GetSetting(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return _settings.TryGetValue(key, out var value) ? value : null;
    }

    public void SetSetting(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);
        _settings[key] = value;
    }

    public IEnumerable<string> GetRecentValues(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return _recentValues.TryGetValue(key, out var values) 
            ? values.ToList() 
            : Enumerable.Empty<string>();
    }

    public void AddRecentValue(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        value = value.Trim();

        var itemList = _recentValues.GetOrAdd(key, _ => new List<string>());

        lock (itemList)
        {
            // Remove if already exists (to move to top)
            if (itemList.Contains(value))
            {
                itemList.Remove(value);
            }

            // Add to the top of the list
            itemList.Insert(0, value);

            // Keep only the 10 most recent
            while (itemList.Count > 10)
            {
                itemList.RemoveAt(itemList.Count - 1);
            }
        }
    }

    /// <summary>
    /// Clear all settings (useful for tests)
    /// </summary>
    public void Clear()
    {
        _settings.Clear();
        _recentValues.Clear();
    }
}
