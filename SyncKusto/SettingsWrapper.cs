// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using SyncKusto.Properties;

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
    }
}
