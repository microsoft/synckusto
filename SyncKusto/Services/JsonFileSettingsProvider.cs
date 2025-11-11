// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using SyncKusto.Core.Abstractions;
using SyncKusto.Core.Configuration;
using SyncKusto.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SyncKusto.Services;

/// <summary>
/// Settings provider that persists to a JSON file in the user's AppData folder.
/// This wraps the existing SettingsWrapper functionality to provide a testable interface.
/// </summary>
public class JsonFileSettingsProvider : ISettingsProvider
{
    private static readonly object _lockObject = new object();
    private readonly string _settingsDirectory;
    private readonly string _settingsFilePath;
    private UserSettings _settings = new UserSettings();
    private bool _isLoaded;

    public JsonFileSettingsProvider()
    {
        _settingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SyncKusto");
        _settingsFilePath = Path.Combine(_settingsDirectory, "userSettings.json");
    }

    /// <summary>
    /// Constructor for testing with custom file path
    /// </summary>
    public JsonFileSettingsProvider(string settingsFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settingsFilePath);
        _settingsFilePath = settingsFilePath;
        _settingsDirectory = Path.GetDirectoryName(settingsFilePath)
            ?? throw new ArgumentException("Invalid settings file path", nameof(settingsFilePath));
    }

    /// <summary>
    /// Load settings from disk if not already loaded
    /// </summary>
    private void EnsureLoaded()
    {
        if (_isLoaded)
        {
            return;
        }

        lock (_lockObject)
        {
            if (_isLoaded)
            {
                return;
            }

            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    string json = File.ReadAllText(_settingsFilePath);
                    var data = JsonSerializer.Deserialize<UserSettings>(json);
                    _settings = data ?? new UserSettings();
                }
                else
                {
                    _settings = new UserSettings();
                }
            }
            catch
            {
                // If the file is corrupt or inaccessible, fall back to defaults
                _settings = new UserSettings();
            }

            _isLoaded = true;
        }
    }

    /// <summary>
    /// Persist settings changes to disk
    /// </summary>
    private void Save()
    {
        lock (_lockObject)
        {
            if (!_isLoaded)
            {
                return; // Nothing loaded, nothing to save
            }

            try
            {
                if (!Directory.Exists(_settingsDirectory))
                {
                    Directory.CreateDirectory(_settingsDirectory);
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };
                string json = JsonSerializer.Serialize(_settings, options);
                File.WriteAllText(_settingsFilePath, json);
            }
            catch
            {
                // Intentionally ignore persistence errors to avoid crashing UI
            }
        }
    }

    public string? GetSetting(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        EnsureLoaded();

        return key switch
        {
            "PreviousFilePath" => _settings.PreviousFilePath,
            "TempCluster" or "KustoClusterForTempDatabases" => _settings.KustoClusterForTempDatabases,
            "TempDatabase" or "TemporaryKustoDatabase" => _settings.TemporaryKustoDatabase,
            "AADAuthority" => _settings.AADAuthority,
            "KustoObjectDropWarning" => _settings.KustoObjectDropWarning.ToString(),
            "TableFieldsOnNewLine" => _settings.TableFieldsOnNewLine?.ToString(),
            "CreateMergeEnabled" => _settings.CreateMergeEnabled?.ToString(),
            "UseLegacyCslExtension" => _settings.UseLegacyCslExtension?.ToString(),
            "FileExtension" => _settings.UseLegacyCslExtension.GetValueOrDefault() ? "csl" : "kql",
            "LineEndingMode" => _settings.LineEndingMode.ToString(),
            "CertificateLocation" => _settings.CertificateLocation,
            _ => null
        };
    }

    public void SetSetting(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        EnsureLoaded();

        switch (key)
        {
            case "PreviousFilePath":
                _settings.PreviousFilePath = value;
                break;
            case "TempCluster":
            case "KustoClusterForTempDatabases":
                _settings.KustoClusterForTempDatabases = value;
                break;
            case "TempDatabase":
            case "TemporaryKustoDatabase":
                _settings.TemporaryKustoDatabase = value;
                break;
            case "AADAuthority":
                _settings.AADAuthority = value;
                break;
            case "KustoObjectDropWarning":
                _settings.KustoObjectDropWarning = bool.Parse(value);
                break;
            case "TableFieldsOnNewLine":
                _settings.TableFieldsOnNewLine = bool.Parse(value);
                break;
            case "CreateMergeEnabled":
                _settings.CreateMergeEnabled = bool.Parse(value);
                break;
            case "UseLegacyCslExtension":
                _settings.UseLegacyCslExtension = bool.Parse(value);
                break;
            case "LineEndingMode":
                _settings.LineEndingMode = int.Parse(value);
                break;
            case "CertificateLocation":
                _settings.CertificateLocation = value;
                break;
            default:
                throw new ArgumentException($"Unknown setting key: {key}", nameof(key));
        }

        Save();
    }

    public IEnumerable<string> GetRecentValues(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        EnsureLoaded();

        return key switch
        {
            "RecentClusters" => _settings.RecentClusters ?? new List<string>(),
            "RecentDatabases" => _settings.RecentDatabases ?? new List<string>(),
            "RecentAppIds" => _settings.RecentAppIds ?? new List<string>(),
            _ => Enumerable.Empty<string>()
        };
    }

    public void AddRecentValue(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        EnsureLoaded();

        var itemList = key switch
        {
            "RecentClusters" => _settings.RecentClusters ??= new List<string>(),
            "RecentDatabases" => _settings.RecentDatabases ??= new List<string>(),
            "RecentAppIds" => _settings.RecentAppIds ??= new List<string>(),
            _ => throw new ArgumentException($"Unknown recent values key: {key}", nameof(key))
        };

        value = value.Trim();

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

        Save();
    }

    /// <summary>
    /// Get the current settings as an immutable configuration object
    /// </summary>
    public SyncKustoSettings GetSettings()
    {
        EnsureLoaded();

        return new SyncKustoSettings
        {
            TempCluster = _settings.KustoClusterForTempDatabases ?? string.Empty,
            TempDatabase = _settings.TemporaryKustoDatabase ?? string.Empty,
            AADAuthority = _settings.AADAuthority,
            KustoObjectDropWarning = _settings.KustoObjectDropWarning,
            TableFieldsOnNewLine = _settings.TableFieldsOnNewLine ?? false,
            CreateMergeEnabled = _settings.CreateMergeEnabled ?? false,
            UseLegacyCslExtension = _settings.UseLegacyCslExtension ?? true,
            LineEndingMode = ParseLineEndingMode(_settings.LineEndingMode),
            CertificateLocation = ParseStoreLocation(_settings.CertificateLocation)
        };
    }

    private static LineEndingMode ParseLineEndingMode(int value)
    {
        return System.Enum.IsDefined(typeof(LineEndingMode), value)
            ? (LineEndingMode)value
            : LineEndingMode.LeaveAsIs;
    }

    private static StoreLocation ParseStoreLocation(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return StoreLocation.CurrentUser;
        }

        return System.Enum.TryParse<StoreLocation>(value, out var result)
            ? result
            : StoreLocation.CurrentUser;
    }

    /// <summary>
    /// Internal data model for settings serialization
    /// </summary>
    private class UserSettings
    {
        public string? PreviousFilePath { get; set; }
        public string? KustoClusterForTempDatabases { get; set; }
        public string? AADAuthority { get; set; }
        public string? TemporaryKustoDatabase { get; set; }
        public bool KustoObjectDropWarning { get; set; } = true;
        public bool? TableFieldsOnNewLine { get; set; } = false;
        public bool? CreateMergeEnabled { get; set; } = false;
        public bool? UseLegacyCslExtension { get; set; } = true;
        public string CertificateLocation { get; set; } = "CurrentUser";
        public List<string>? RecentClusters { get; set; }
        public List<string>? RecentDatabases { get; set; }
        public List<string>? RecentAppIds { get; set; }
        public int LineEndingMode { get; set; } = 0; // LeaveAsIs
    }
}
