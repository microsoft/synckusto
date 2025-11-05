// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using SyncKusto.ChangeModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SyncKusto
{
    /// <summary>
    /// Settings are saved in a JSON file in the user's AppData folder. This wrapper provides type-checking.
    /// </summary>
    public static partial class SettingsWrapper
    {
        private static readonly object _lockObject = new object();
        private static readonly string _settingsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SyncKusto");
        private static readonly string _settingsFilePath = Path.Combine(_settingsDirectory, "userSettings.json");
        private static UserSettings _settings;
        private static bool _isLoaded;

        /// <summary>
        /// Load settings from disk if not already loaded.
        /// </summary>
        private static void EnsureLoaded()
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
                    // If the file is corrupt or inaccessible, fall back to defaults.
                    _settings = new UserSettings();
                }

                _isLoaded = true;
            }
        }

        /// <summary>
        /// Persist settings changes to disk.
        /// </summary>
        private static void Save()
        {
            lock (_lockObject)
            {
                if (!_isLoaded)
                {
                    return; // Nothing loaded, nothing to save.
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
                    // Intentionally ignore persistence errors to avoid crashing UI.
                }
            }
        }

        /// <summary>
        /// The location of Kusto files that was used previously
        /// </summary>
        public static string PreviousFilePath
        {
            get
            {
                EnsureLoaded();
                return _settings.PreviousFilePath;
            }
            set
            {
                EnsureLoaded();
                _settings.PreviousFilePath = value;
                Save();
            }
        }

        /// <summary>
        /// The Kusto cluster that should be used to create temporary databases for schema comparison
        /// </summary>
        public static string KustoClusterForTempDatabases
        {
            get
            {
                EnsureLoaded();
                return _settings.KustoClusterForTempDatabases;
            }
            set
            {
                EnsureLoaded();
                _settings.KustoClusterForTempDatabases = value;
                Save();
            }
        }

        /// <summary>
        /// The temporary database to use to load a schema
        /// </summary>
        public static string TemporaryKustoDatabase
        {
            get
            {
                EnsureLoaded();
                return _settings.TemporaryKustoDatabase;
            }
            set
            {
                EnsureLoaded();
                _settings.TemporaryKustoDatabase = value;
                Save();
            }
        }

        /// <summary>
        /// The AAD Authority to use to authenticate a user. For user auth, this might work when it's empty depending on the tenant configuration,
        /// but it's always required for AAD application auth.
        /// </summary>
        public static string AADAuthority
        {
            get
            {
                EnsureLoaded();
                return _settings.AADAuthority;
            }
            set
            {
                EnsureLoaded();
                _settings.AADAuthority = value;
                Save();
            }
        }

        /// <summary>
        /// Prompt the user before dropping any tables from the target as part of an Update operation
        /// </summary>
        public static bool KustoObjectDropWarning
        {
            get
            {
                EnsureLoaded();
                return _settings.KustoObjectDropWarning;
            }
            set
            {
                EnsureLoaded();
                _settings.KustoObjectDropWarning = value;
                Save();
            }
        }

        /// <summary>
        /// If true, every table field will get it's own line in the resulting CSL file
        /// </summary>
        public static bool? TableFieldsOnNewLine
        {
            get
            {
                EnsureLoaded();
                return _settings.TableFieldsOnNewLine;
            }
            set
            {
                EnsureLoaded();
                _settings.TableFieldsOnNewLine = value;
                Save();
            }
        }

        /// <summary>
        /// If true, table create commands will be ".create-merge table" instead of ".create table"
        /// </summary>
        public static bool? CreateMergeEnabled
        {
            get
            {
                EnsureLoaded();
                return _settings.CreateMergeEnabled;
            }
            set
            {
                EnsureLoaded();
                _settings.CreateMergeEnabled = value;
                Save();
            }
        }

        /// <summary>
        /// If true, files will be read and written using the legacy ".csl" extension instead of ".kql"
        /// </summary>
        public static bool? UseLegacyCslExtension
        {
            get
            {
                EnsureLoaded();
                return _settings.UseLegacyCslExtension;
            }
            set
            {
                EnsureLoaded();
                _settings.UseLegacyCslExtension = value;
                Save();
            }
        }

        /// <summary>
        /// Specifies how to handle line endings in the files. They can be left as they are or converted 
        /// to Windows or Unix style when they are written.
        /// </summary>
        public static LineEndingMode LineEndingMode
        {
            get
            {
                EnsureLoaded();
                if (Enum.IsDefined(typeof(LineEndingMode), _settings.LineEndingMode))
                {
                    return (LineEndingMode)_settings.LineEndingMode;
                }
                return ChangeModel.LineEndingMode.LeaveAsIs;
            }
            set
            {
                EnsureLoaded();
                _settings.LineEndingMode = (int)value;
                Save();
            }
        }

        /// <summary>
        /// Gets the file extension to use throughout the application when reading and writing Kusto files
        /// </summary>
        public static string FileExtension
        {
            get
            {
                return SettingsWrapper.UseLegacyCslExtension.GetValueOrDefault() ? "csl" : "kql";
            }
        }

        /// <summary>
        /// Get or set the certificate location to search use when displaing certs in the Subject Name Issuer cert picker.
        /// </summary>
        public static StoreLocation CertificateLocation
        {
            get
            {
                EnsureLoaded();
                if (string.IsNullOrWhiteSpace(_settings.CertificateLocation))
                {
                    return StoreLocation.CurrentUser;
                }
                if (Enum.TryParse(_settings.CertificateLocation, out StoreLocation result))
                {
                    return result;
                }
                throw new Exception($"Could not map {_settings.CertificateLocation} to StoreLocation enum type.");
            }
            set
            {
                EnsureLoaded();
                _settings.CertificateLocation = value.ToString();
                Save();
            }
        }

        /// <summary>
        /// Get or set the most recently used clusters
        /// </summary>
        public static List<string> RecentClusters
        {
            get
            {
                EnsureLoaded();
                return _settings.RecentClusters ?? new List<string>();
            }
            set
            {
                EnsureLoaded();
                if (value == null)
                {
                    _settings.RecentClusters = new List<string>();
                }
                else if (!(value is IList<string>))
                {
                    throw new ArgumentException("Value must be of type IList<string>");
                }
                else
                {
                    _settings.RecentClusters = value.ToList();
                }
                Save();
            }
        }

        /// <summary>
        /// Get or set the most recently used databases
        /// </summary>
        public static List<string> RecentDatabases
        {
            get
            {
                EnsureLoaded();
                return _settings.RecentDatabases ?? new List<string>();
            }
            set
            {
                EnsureLoaded();
                if (value == null)
                {
                    _settings.RecentDatabases = new List<string>();
                }
                else if (!(value is IList<string>))
                {
                    throw new ArgumentException("Value must be of type IList<string>");
                }
                else
                {
                    _settings.RecentDatabases = value.ToList();
                }
                Save();
            }
        }

        /// <summary>
        /// Get or set the most recently used application ids
        /// </summary>
        public static List<string> RecentAppIds
        {
            get
            {
                EnsureLoaded();
                return _settings.RecentAppIds ?? new List<string>();
            }
            set
            {
                EnsureLoaded();
                if (value == null)
                {
                    _settings.RecentAppIds = new List<string>();
                }
                else if (!(value is IList<string>))
                {
                    throw new ArgumentException("Value must be of type IList<string>");
                }
                else
                {
                    _settings.RecentAppIds = value.ToList();
                }
                Save();
            }
        }

        /// <summary>
        /// Include a cluster in the recent history list. If it's already in the list, it will be
        /// moved to the top of the list.
        /// </summary>
        /// <param name="cluster">The cluster to include</param>
        public static void AddRecentCluster(string cluster)
        {
            RecentClusters = AddRecentItem(RecentClusters, cluster);
        }

        /// <summary>
        /// Include a database in the recent history list. If it's already in the list, it will be
        /// moved to the top of the list.
        /// </summary>
        /// <param name="database">The database to include</param>
        public static void AddRecentDatabase(string database)
        {
            RecentDatabases = AddRecentItem(RecentDatabases, database);
        }

        /// <summary>
        /// Include an application id in the recent history list. If it's already in the list, it
        /// will be moved to the top of the list.
        /// </summary>
        /// <param name="applicationId">The application id to include</param>
        public static void AddRecentAppId(string applicationId)
        {
            RecentAppIds = AddRecentItem(RecentAppIds, applicationId);
        }

        /// <summary>
        /// Make sure that an item is included in a list. If it's a new item, it gets added at the
        /// top of the list. If it's an existing item, that item gets moved to the top of the list.
        /// If the list is longer than 10 items, the least recently used items are truncated to get
        /// back to 10.
        /// </summary>
        /// <param name="itemList">The list of items to update.</param>
        /// <param name="item">The item to include in the list.</param>
        /// <returns>The updated list of items.</returns>
        private static List<string> AddRecentItem(List<string> itemList, string item)
        {
            if (string.IsNullOrWhiteSpace(item))
            {
                return itemList;
            }

            item = item.Trim();

            if (itemList.Contains(item))
            {
                // To bubble this to the top we'll first remove it from the list and then the next
                // code block will add it back in.
                itemList.Remove(item);
            }

            // Add it to the bottom of the list
            itemList.Insert(0, item);
            while (itemList.Count > 10)
            {
                itemList.RemoveAt(itemList.Count - 1);
            }

            return itemList;
        }
    }
}
