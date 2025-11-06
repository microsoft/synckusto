// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Kusto.Data.Common;
using SyncKusto.Core.Models;

namespace SyncKusto.Kusto.Services;

/// <summary>
/// Wraps Kusto's CslCommandGenerator to allow custom formatting of CSL commands
/// </summary>
public static class FormattedCslCommandGenerator
{
    /// <summary>
    /// Generates a table create command with optional formatting customizations
    /// </summary>
    /// <param name="table">The table schema to convert to a string</param>
    /// <param name="forceNormalizeColumnName">True to force the column names to be normalized/escaped</param>
    /// <param name="createMergeEnabled">True to generate create-merge command instead of create</param>
    /// <param name="tableFieldsOnNewLine">True to put each field on a new line</param>
    /// <param name="lineEndingMode">The line ending style to use when formatting</param>
    /// <returns>A CSL command string</returns>
    public static string GenerateTableCreateCommand(
        TableSchema table, 
        bool forceNormalizeColumnName = false,
        bool createMergeEnabled = false,
        bool tableFieldsOnNewLine = false,
        LineEndingMode lineEndingMode = LineEndingMode.WindowsStyle)
    {
        string result = createMergeEnabled
            ? CslCommandGenerator.GenerateTableCreateMergeCommandWithExtraProperties(table, forceNormalizeColumnName)
            : CslCommandGenerator.GenerateTableCreateCommand(table, forceNormalizeColumnName);

        if (tableFieldsOnNewLine)
        {
            // Determine line ending to use
            string lineEnding = lineEndingMode == LineEndingMode.UnixStyle ? "\n" : "\r\n";

            // Add a line break between each field
            result = result.Replace(", ['", $",{lineEnding}    ['");

            // Add a line break before the first field
            int parameterStartIndex = result.LastIndexOf("([");
            if (parameterStartIndex >= 0)
            {
                result = result.Insert(parameterStartIndex + 1, $"{lineEnding}    ");
            }
        }

        return result;
    }
}
