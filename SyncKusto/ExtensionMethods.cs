// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using Kusto.Data.Common;
using SyncKusto.Kusto;

namespace SyncKusto
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Write the function to the file system.
        /// </summary>
        /// <param name="functionSchema">The function to write</param>
        /// <param name="rootFolder">The root folder for all the CSL files</param>
        /// <returns></returns>
        public static void WriteToFile(this FunctionSchema functionSchema, string rootFolder, string fileExtension)
        {
            string filename = Path.ChangeExtension(functionSchema.Name, fileExtension);

            // First remove any other files with this name. In the case where you moved an object to a new folder, this will handle cleaning up the old file
            string[] existingFiles = Directory.GetFiles(rootFolder, filename, SearchOption.AllDirectories);
            if (existingFiles.Length > 0)
            {
                foreach (string file in existingFiles)
                {
                    try
                    {
                        File.Delete(file.HandleLongFileNames());
                    }
                    catch
                    {
                        // It's not the end of the world if this call fails
                    }
                }
            }

            // Now add write the new file to the correct location.
            string funcFolder = Path.Combine(rootFolder, "Functions");
            if (!string.IsNullOrEmpty(functionSchema.Folder))
            {
                string cleanedFolder = string.Join("", functionSchema.Folder.Split(Path.GetInvalidPathChars()));
                funcFolder = Path.Combine(funcFolder, cleanedFolder);
            }
            
            string destinationFile = Path.Combine(funcFolder, filename);
            if (!Directory.Exists(funcFolder))
            {
                Directory.CreateDirectory(funcFolder);
            }

            File.WriteAllText(destinationFile.HandleLongFileNames(), CslCommandGenerator.GenerateCreateOrAlterFunctionCommand(functionSchema, true));
        }

        /// <summary>
        /// Write a function to Kusto
        /// </summary>
        /// <param name="functionSchema">The function to write</param>
        /// <param name="kustoQueryEngine">An initialized query engine for issuing the Kusto command</param>
        public static void WriteToKusto(this FunctionSchema functionSchema, QueryEngine kustoQueryEngine)
        {
            kustoQueryEngine.CreateOrAlterFunctionAsync(CslCommandGenerator.GenerateCreateOrAlterFunctionCommand(functionSchema, true), functionSchema.Name).Wait();
        }

        /// <summary>
        /// Delete a function from the file system
        /// </summary>
        /// <param name="functionSchema">The function to remove</param>
        /// <param name="rootFolder">The root folder for all the CSL files</param>
        public static void DeleteFromFolder(this FunctionSchema functionSchema, string rootFolder, string fileExtension)
        {
            string funcFolder = Path.Combine(rootFolder, "Functions");
            if (!string.IsNullOrEmpty(functionSchema.Folder))
            {
                funcFolder = Path.Combine(funcFolder, functionSchema.Folder);
            }
            string destinationFile = Path.ChangeExtension(Path.Combine(funcFolder, functionSchema.Name), fileExtension);
            File.Delete(destinationFile.HandleLongFileNames());
        }

        /// <summary>
        /// Delete a function from Kusto
        /// </summary>
        /// <param name="functionSchema">The function to remove</param>
        /// <param name="kustoQueryEngine">An initialized query engine for issuing the Kusto command</param>
        public static void DeleteFromKusto(this FunctionSchema functionSchema, QueryEngine kustoQueryEngine)
        {
            kustoQueryEngine.DropFunction(functionSchema);
        }

        /// <summary>
        /// Write a table to the file system
        /// </summary>
        /// <param name="tableSchema">The table to write</param>
        /// <param name="rootFolder">The root folder for all the CSL files</param>
        public static void WriteToFile(this TableSchema tableSchema, string rootFolder, string fileExtension)
        {
            string tableFolder = rootFolder;
            if (!string.IsNullOrEmpty(tableSchema.Folder))
            {
                string cleanedFolder = string.Join("", tableSchema.Folder.Split(Path.GetInvalidPathChars()));
                tableFolder = Path.Combine(rootFolder, "Tables", cleanedFolder);
            }
            string destinationFile = Path.ChangeExtension(Path.Combine(tableFolder, tableSchema.Name), fileExtension);
            if (!Directory.Exists(tableFolder))
            {
                Directory.CreateDirectory(tableFolder);
            }

            File.WriteAllText(destinationFile.HandleLongFileNames(), FormattedCslCommandGenerator.GenerateTableCreateCommand(tableSchema, true));
        }

        /// <summary>
        /// Write a table to Kusto
        /// </summary>
        /// <param name="tableSchema">The table to write</param>
        /// <param name="kustoQueryEngine">An initialized query engine for issuing the Kusto command</param>
        public static void WriteToKusto(this TableSchema tableSchema, QueryEngine kustoQueryEngine)
        {
            kustoQueryEngine.CreateOrAlterTableAsync(FormattedCslCommandGenerator.GenerateTableCreateCommand(tableSchema, false), tableSchema.Name).Wait();
        }

        /// <summary>
        /// Delete a table from the file system
        /// </summary>
        /// <param name="tableSchema">The table to remove</param>
        /// <param name="rootFolder">The root folder for all the CSL files</param>
        public static void DeleteFromFolder(this TableSchema tableSchema, string rootFolder, string fileExtension)
        {
            string tableFolder = rootFolder;
            if (!string.IsNullOrEmpty(tableSchema.Folder))
            {
                tableFolder = Path.Combine(rootFolder, "Tables", tableSchema.Folder);
            }
            string destinationFile = Path.ChangeExtension(Path.Combine(tableFolder, tableSchema.Name), fileExtension);
            File.Delete(destinationFile.HandleLongFileNames());
        }

        /// <summary>
        /// Delete a table from Kusto
        /// </summary>
        /// <param name="tableSchema">The table to remove</param>
        /// <param name="kustoQueryEngine">An initialized query engine for issuing the Kusto command</param>
        public static void DeleteFromKusto(this TableSchema tableSchema, QueryEngine kustoQueryEngine)
        {
            kustoQueryEngine.DropTable(tableSchema.Name);
        }

        /// <summary>
        /// Convert to long path to avoid issues with long file names
        //  https://learn.microsoft.com/en-us/windows/win32/fileio/naming-a-file#win32-file-namespaces
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static string HandleLongFileNames(this string filename)
        {
            const string LongPathPrefix = "\\\\?\\";

            if (filename.Length > 248)
            {
                // The path is getting close to the limit so prepend the longPathPrefix.
                return LongPathPrefix + filename;
            }
            else if (filename.StartsWith(LongPathPrefix))
            {
                // The path has the long path prefix but doesn't need it.
                return filename.Substring(LongPathPrefix.Length);
            }
            else
            {
                // The path doesn't have the long path prefix and doesn't need it.
                return filename;
            }
        }
    }
}
