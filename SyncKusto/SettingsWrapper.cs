// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using SyncKusto.Properties;
using System;
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

        /// <summary>
        /// Gets the file extension to use throughout the application when reading and writing Kusto files
        /// </summary>
        public static string FileExtension
        {
            get => SettingsWrapper.UseLegacyCslExtension.GetValueOrDefault() ? "csl" : "kql";
        }

        /// <summary>
        /// The certificate location to search use when displaing certs in the Subject Name Issuer cert picker.
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

                // Couldn't parse so we'll go with CurrentUser.
                return StoreLocation.CurrentUser;
            }
            set
            {
                Settings.Default["CertificateLocation"] = value.ToString();
                Settings.Default.Save();
            }
        }
    }
}