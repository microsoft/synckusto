// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace SyncKusto
{
public static partial class SettingsWrapper
    {
        /// <summary>
        /// Internal data model for settings serialization.
        /// </summary>
        private class UserSettings
        {
            public string PreviousFilePath { get; set; }
            public string KustoClusterForTempDatabases { get; set; }
            public string AADAuthority { get; set; }
            public string TemporaryKustoDatabase { get; set; }
            public bool KustoObjectDropWarning { get; set; } = true;
            public bool? TableFieldsOnNewLine { get; set; } = false;
            public bool? CreateMergeEnabled { get; set; } = false;
            public bool? UseLegacyCslExtension { get; set; } = true;
            public string CertificateLocation { get; set; } = "CurrentUser";
            public List<string> RecentClusters { get; set; } = new List<string>();
            public List<string> RecentDatabases { get; set; } = new List<string>();
            public List<string> RecentAppIds { get; set; } = new List<string>();
            public int LineEndingMode { get; set; } = 0; // LeaveAsIs
        }
    }
}
