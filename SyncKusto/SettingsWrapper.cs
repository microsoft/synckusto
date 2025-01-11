// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Kusto.Data;
using SyncKusto.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace SyncKusto
{
    /// <summary>
    /// Settings are saved in a config file. This class wraps all of them to provide type-checking.
    /// </summary>
    public static class SettingsWrapper
    {
        /// <summary>
        /// The location of Kusto files that was used previously
        /// </summary>
        public static string PreviousFilePath
        {
            get => Settings.Default["PreviousFilePath"] as string;
            set
            {
                Settings.Default["PreviousFilePath"] = value;
                Settings.Default.Save();
            }
        }

        /// <summary>
        /// The Kusto cluster that should be used to create temporary databases for schema comparison
        /// </summary>
        public static string KustoClusterForTempDatabases
        {
            get => Settings.Default["KustoClusterForTempDatabases"] as string;
            set
            {
                Settings.Default["KustoClusterForTempDatabases"] = value;
                Settings.Default.Save();
            }
        }

        /// <summary>
        /// The temporary database to use to load a schema
        /// </summary>
        public static string TemporaryKustoDatabase
        {
            get => Settings.Default["TemporaryKustoDatabase"] as string;
            set
            {
                Settings.Default["TemporaryKustoDatabase"] = value;
                Settings.Default.Save();
            }
        }

        /// <summary>
        /// The AAD Authority to use to authenticate a user. For user auth, this might work when it's empty depending on the tenant configuration,
        /// but it's always required for AAD application auth.
        /// </summary>
        public static string AADAuthority
        {
            get => Settings.Default["AADAuthority"] as string;
            set
            {
                Settings.Default["AADAuthority"] = value;
                Settings.Default.Save();
            }
        }

        /// <summary>
        /// Prompt the user before dropping any tables from the target as part of an Update operation
        /// </summary>
        public static bool KustoObjectDropWarning
        {
            get
            {
                bool? currentSetting = Settings.Default["KustoObjectDropWarning"] as bool?;
                return currentSetting ?? false;
            }
            set
            {
                Settings.Default["KustoObjectDropWarning"] = value;
                Settings.Default.Save();
            }
        }

        /// <summary>
        /// If true, every table field will get it's own line in the resulting CSL file
        /// </summary>
        public static bool? TableFieldsOnNewLine
        {
            get => Settings.Default["TableFieldsOnNewLine"] as bool?;
            set
            {
                Settings.Default["TableFieldsOnNewLine"] = value;
                Settings.Default.Save();
            }
        }

        /// <summary>
        /// If true, table create commands will be ".create-merge table" instead of ".create table"
        /// </summary>
        public static bool? CreateMergeEnabled
        {
            get => Settings.Default["CreateMergeEnabled"] as bool?;
            set
            {
                Settings.Default["CreateMergeEnabled"] = value;
                Settings.Default.Save();
            }
        }

        /// <summary>
        /// If true, files will be read and written using the legacy ".csl" extension instead of ".kql"
        /// </summary>
        public static bool? UseLegacyCslExtension
        {
            get => Settings.Default[nameof(UseLegacyCslExtension)] as bool?;
            set
            {
                Settings.Default[nameof(UseLegacyCslExtension)] = value;
                Settings.Default.Save();
            }
        }

        public static bool? IgnoreLineEndings
        {
	        get => Settings.Default[nameof(IgnoreLineEndings)] as bool?;
	        set
	        {
		        Settings.Default[nameof(IgnoreLineEndings)] = value;
		        Settings.Default.Save();
	        }
        }

        /// <summary>
        /// Gets the file extension to use throughout the application when reading and writing Kusto files
        /// </summary>
        public static string FileExtension
        {
            get => SettingsWrapper.UseLegacyCslExtension.GetValueOrDefault() ? "csl" : "kql";
        }

        /// <summary>
        /// Get or set the certificate location to search use when displaing certs in the Subject Name Issuer cert picker.
        /// </summary>
        public static StoreLocation CertificateLocation
        {
            get
            {
                var currentValue = Settings.Default["CertificateLocation"] as string;
                if (string.IsNullOrWhiteSpace(currentValue))
                {
                    return StoreLocation.CurrentUser;
                }

                if (Enum.TryParse(currentValue, out StoreLocation result))
                {
                    return result;
                }

                throw new Exception($"Could not map {currentValue} to StoreLocation enum type.");
            }
            set
            {
                Settings.Default["CertificateLocation"] = value.ToString();
                Settings.Default.Save();
            }
        }

        /// <summary>
        /// Get or set the most recently used clusters
        /// </summary>
        public static List<string> RecentClusters
        {
            get
            {
                var currentValue = Settings.Default["RecentClusters"] as StringCollection;
                if (currentValue == null)
                {
                    return new List<string>();
                }

                return currentValue.Cast<string>().ToList();
            }
            set
            {
                if (!(value is IList<string>))
                {
                    throw new ArgumentException("Value must be of type IList<string>");
                }

                var sc = new StringCollection();
                sc.AddRange(value.ToArray());
                Settings.Default["RecentClusters"] = sc;
                Settings.Default.Save();
            }
        }

        /// <summary>
        /// Get or set the most recently used databases
        /// </summary>
        public static List<string> RecentDatabases
        {
            get
            {
                var currentValue = Settings.Default["RecentDatabases"] as StringCollection;
                if (currentValue == null)
                {
                    return new List<string>();
                }

                return currentValue.Cast<string>().ToList();
            }
            set
            {
                if (!(value is IList<string>))
                {
                    throw new ArgumentException("Value must be of type IList<string>");
                }

                var sc = new StringCollection();
                sc.AddRange(value.ToArray());
                Settings.Default["RecentDatabases"] = sc;
                Settings.Default.Save();
            }
        }

        /// <summary>
        /// Get or set the most recently used application ids
        /// </summary>
        public static List<string> RecentAppIds
        {
            get
            {
                var currentValue = Settings.Default["RecentAppIds"] as StringCollection;
                if (currentValue == null)
                {
                    return new List<string>();
                }

                return currentValue.Cast<string>().ToList();
            }
            set
            {
                if (!(value is IList<string>))
                {
                    throw new ArgumentException("Value must be of type IList<string>");
                }

                var sc = new StringCollection();
                sc.AddRange(value.ToArray());
                Settings.Default["RecentAppIds"] = sc;
                Settings.Default.Save();
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
