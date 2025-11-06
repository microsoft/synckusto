using Kusto.Data.Common;
using SyncKusto.Core.Models;

namespace SyncKusto.Kusto
{
    /// <summary>
    /// If this tool wants to save CSL files that are formatted slightly differently than the Kusto default, the Kusto
    /// calls to CslCommandGenerator can be wrapped in this class.
    /// </summary>
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
            string result = SettingsWrapper.CreateMergeEnabled == true
                ? CslCommandGenerator.GenerateTableCreateMergeCommandWithExtraProperties(table, forceNormalizeColumnName)
                : CslCommandGenerator.GenerateTableCreateCommand(table, forceNormalizeColumnName);

            if (SettingsWrapper.TableFieldsOnNewLine == true)
            {
                // We have to do some kind of line break. The three options in settings are "leave as is", "windows",
                // or "unix" but the default is "leave as is." "Leave as is" doesn't make sense in this scenario so
                // we'll bucket it with "Windows" style.
                string lineEnding = SettingsWrapper.LineEndingMode == LineEndingMode.UnixStyle ? "\n" : "\r\n";

                // Add a line break between each field
                result = result.Replace(", ['", $",{lineEnding}    ['");

                // Add a line break before the first field
                int parameterStartIndex = result.LastIndexOf("([");
                result = result.Insert(parameterStartIndex + 1, $"{lineEnding}    ");
            }

            return result;
        }
    }
}
