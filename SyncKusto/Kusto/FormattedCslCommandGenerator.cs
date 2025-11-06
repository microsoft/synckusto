// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// This file is kept for backward compatibility but the actual implementation
// is now in SyncKusto.Kusto.Services.FormattedCslCommandGenerator
// Consider using the new namespace in new code.

using System;
using Kusto.Data.Common;
using SyncKusto.Core.Models;
using SyncKusto.Kusto.Services;

namespace SyncKusto.Kusto
{
    /// <summary>
    /// If this tool wants to save CSL files that are formatted slightly differently than the Kusto default, the Kusto
    /// calls to CslCommandGenerator can be wrapped in this class.
    /// </summary>
    [Obsolete("Use SyncKusto.Kusto.Services.FormattedCslCommandGenerator instead")]
    public static class FormattedCslCommandGenerator
    {
        /// <summary>
        /// Wrap the call to CslCommandGenerator.GenerateTableCreateCommand and allow special formatting if the user
        /// has enabled the setting flag for it. Also choose between "create" and "create merge" based on setting
        /// </summary>
        /// <param name="table">The table schema to convert to a string</param>
        /// <param name="forceNormalizeColumnName">True to force the column names to be normalized/escaped</param>
        /// <returns></returns>
        public static string GenerateTableCreateCommand(TableSchema table, bool forceNormalizeColumnName = false)
        {
            return SyncKusto.Kusto.Services.FormattedCslCommandGenerator.GenerateTableCreateCommand(
                table,
                forceNormalizeColumnName,
                SettingsWrapper.CreateMergeEnabled ?? false,
                SettingsWrapper.TableFieldsOnNewLine ?? false,
                SettingsWrapper.LineEndingMode);
        }
    }
}
