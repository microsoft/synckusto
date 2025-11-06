// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using Kusto.Data.Common;
using SyncKusto.Core.Models;
using SyncKusto.FileSystem.Exceptions;

namespace SyncKusto.FileSystem.Extensions;

/// <summary>
/// Extension methods for file system operations on Kusto schemas
/// </summary>
public static class FileSystemSchemaExtensions
{
    /// <summary>
    /// Write the function to the file system.
    /// </summary>
    /// <param name="functionSchema">The function to write</param>
    /// <param name="rootFolder">The root folder for all the CSL files</param>
    /// <param name="fileExtension">The file extension to use</param>
    /// <exception cref="FileSchemaException">Thrown when the file cannot be written</exception>
    public static void WriteToFile(this FunctionSchema functionSchema, string rootFolder, string fileExtension)
    {
        ArgumentNullException.ThrowIfNull(functionSchema);
        ArgumentException.ThrowIfNullOrWhiteSpace(rootFolder);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileExtension);

        try
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
                        File.Delete(HandleLongFileNames(file));
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

            File.WriteAllText(
                HandleLongFileNames(destinationFile), 
                CslCommandGenerator.GenerateCreateOrAlterFunctionCommand(functionSchema, true));
        }
        catch (Exception ex) when (ex is not FileSchemaException)
        {
            throw new FileSchemaException($"Failed to write function '{functionSchema.Name}' to file system", ex);
        }
    }

    /// <summary>
    /// Delete a function from the file system
    /// </summary>
    /// <param name="functionSchema">The function to remove</param>
    /// <param name="rootFolder">The root folder for all the CSL files</param>
    /// <param name="fileExtension">The file extension to use</param>
    /// <exception cref="FileSchemaException">Thrown when the file cannot be deleted</exception>
    public static void DeleteFromFolder(this FunctionSchema functionSchema, string rootFolder, string fileExtension)
    {
        ArgumentNullException.ThrowIfNull(functionSchema);
        ArgumentException.ThrowIfNullOrWhiteSpace(rootFolder);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileExtension);

        try
        {
            string funcFolder = Path.Combine(rootFolder, "Functions");
            if (!string.IsNullOrEmpty(functionSchema.Folder))
            {
                funcFolder = Path.Combine(funcFolder, functionSchema.Folder);
            }
            string destinationFile = Path.ChangeExtension(Path.Combine(funcFolder, functionSchema.Name), fileExtension);
            File.Delete(HandleLongFileNames(destinationFile));
        }
        catch (Exception ex) when (ex is not FileSchemaException)
        {
            throw new FileSchemaException($"Failed to delete function '{functionSchema.Name}' from file system", ex);
        }
    }

    /// <summary>
    /// Write a table to the file system
    /// </summary>
    /// <param name="tableSchema">The table to write</param>
    /// <param name="rootFolder">The root folder for all the CSL files</param>
    /// <param name="fileExtension">The file extension to use</param>
    /// <param name="createMergeEnabled">Whether to use create-merge command</param>
    /// <param name="tableFieldsOnNewLine">Whether to put each field on a new line</param>
    /// <param name="lineEndingMode">The line ending mode to use</param>
    /// <exception cref="FileSchemaException">Thrown when the file cannot be written</exception>
    public static void WriteToFile(
        this TableSchema tableSchema, 
        string rootFolder, 
        string fileExtension,
        bool createMergeEnabled = false,
        bool tableFieldsOnNewLine = false,
        LineEndingMode lineEndingMode = LineEndingMode.WindowsStyle)
    {
        ArgumentNullException.ThrowIfNull(tableSchema);
        ArgumentException.ThrowIfNullOrWhiteSpace(rootFolder);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileExtension);

        try
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

            File.WriteAllText(
                HandleLongFileNames(destinationFile), 
                FormattedCslCommandGenerator.GenerateTableCreateCommand(
                    tableSchema, 
                    forceNormalizeColumnName: true,
                    createMergeEnabled: createMergeEnabled,
                    tableFieldsOnNewLine: tableFieldsOnNewLine,
                    lineEndingMode: lineEndingMode));
        }
        catch (Exception ex) when (ex is not FileSchemaException)
        {
            throw new FileSchemaException($"Failed to write table '{tableSchema.Name}' to file system", ex);
        }
    }

    /// <summary>
    /// Delete a table from the file system
    /// </summary>
    /// <param name="tableSchema">The table to remove</param>
    /// <param name="rootFolder">The root folder for all the CSL files</param>
    /// <param name="fileExtension">The file extension to use</param>
    /// <exception cref="FileSchemaException">Thrown when the file cannot be deleted</exception>
    public static void DeleteFromFolder(this TableSchema tableSchema, string rootFolder, string fileExtension)
    {
        ArgumentNullException.ThrowIfNull(tableSchema);
        ArgumentException.ThrowIfNullOrWhiteSpace(rootFolder);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileExtension);

        try
        {
            string tableFolder = rootFolder;
            if (!string.IsNullOrEmpty(tableSchema.Folder))
            {
                tableFolder = Path.Combine(rootFolder, "Tables", tableSchema.Folder);
            }
            string destinationFile = Path.ChangeExtension(Path.Combine(tableFolder, tableSchema.Name), fileExtension);
            File.Delete(HandleLongFileNames(destinationFile));
        }
        catch (Exception ex) when (ex is not FileSchemaException)
        {
            throw new FileSchemaException($"Failed to delete table '{tableSchema.Name}' from file system", ex);
        }
    }

    /// <summary>
    /// Convert to long path to avoid issues with long file names
    /// https://learn.microsoft.com/en-us/windows/win32/fileio/naming-a-file#win32-file-namespaces
    /// </summary>
    /// <param name="filename">The filename to process</param>
    /// <returns>The filename with long path prefix if needed</returns>
    public static string HandleLongFileNames(string filename)
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

    /// <summary>
    /// Helper class for generating formatted CSL commands
    /// This is a simplified version that calls the real implementation in SyncKusto.Kusto
    /// </summary>
    private static class FormattedCslCommandGenerator
    {
        public static string GenerateTableCreateCommand(
            TableSchema tableSchema,
            bool forceNormalizeColumnName,
            bool createMergeEnabled,
            bool tableFieldsOnNewLine,
            LineEndingMode lineEndingMode)
        {
            // We need to reference the actual implementation from SyncKusto.Kusto
            // For now, this is a placeholder that will be replaced with proper dependency injection
            return SyncKusto.Kusto.Services.FormattedCslCommandGenerator.GenerateTableCreateCommand(
                tableSchema,
                forceNormalizeColumnName,
                createMergeEnabled,
                tableFieldsOnNewLine,
                lineEndingMode);
        }
    }
}
