// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Kusto.Data.Common;
using SyncKusto.Core.Abstractions;
using SyncKusto.Core.Configuration;
using SyncKusto.Core.Exceptions;
using SyncKusto.FileSystem.Extensions;
using SyncKusto.Kusto.Exceptions;
using SyncKusto.Kusto.Models;
using SyncKusto.Kusto.Services;

namespace SyncKusto.FileSystem.Repositories;

/// <summary>
/// Repository for accessing database schemas from the file system
/// </summary>
public class FileSystemSchemaRepository : ISchemaRepository, ISchemaSnapshotSource
{
    private readonly string _rootFolder;
    private readonly string _fileExtension;
    private readonly string _tempCluster;
    private readonly string _tempDatabase;
    private readonly string _authority;
    private readonly SyncKustoSettings _settings;
    private readonly Func<ISchemaQueryEngine> _queryEngineFactory;

    /// <summary>
    /// Initializes a new instance of the FileSystemSchemaRepository
    /// </summary>
    /// <param name="rootFolder">The root folder containing the schema files</param>
    /// <param name="fileExtension">The file extension to use (kql or csl)</param>
    /// <param name="tempCluster">The temporary Kusto cluster for schema loading</param>
    /// <param name="tempDatabase">The temporary database for schema loading</param>
    /// <param name="authority">The AAD authority for authentication</param>
    /// <param name="settings">Application settings</param>
    /// <param name="queryEngineFactory">Creates the normalization engine only after preflight succeeds</param>
    public FileSystemSchemaRepository(
        string rootFolder,
        string fileExtension,
        string tempCluster,
        string tempDatabase,
        string authority,
        SyncKustoSettings settings,
        Func<ISchemaQueryEngine>? queryEngineFactory = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootFolder);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileExtension);
        ArgumentException.ThrowIfNullOrWhiteSpace(tempCluster);
        ArgumentException.ThrowIfNullOrWhiteSpace(tempDatabase);
        ArgumentNullException.ThrowIfNull(settings);

        _rootFolder = rootFolder;
        _fileExtension = fileExtension;
        _tempCluster = tempCluster;
        _tempDatabase = tempDatabase;
        _authority = authority;
        _settings = settings;
        _queryEngineFactory = queryEngineFactory ??
            (() => new QueryEngine(_tempCluster, _tempDatabase, _authority, _settings.LineEndingMode));
    }

    /// <summary>
    /// Gets the database schema by loading files from the file system
    /// </summary>
    public async Task<DatabaseSchema> GetSchemaAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = CaptureSnapshot("Schema", cancellationToken);
        snapshot.Validate();
        return await snapshot.GetSchemaAsync(cancellationToken);
    }

    public ISchemaSnapshot CaptureSnapshot(string side, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            Directory.CreateDirectory(_rootFolder);
            Directory.CreateDirectory(Path.Combine(_rootFolder, "Functions"));

            var files = new List<SnapshotFile>();
            var paths = Directory.GetFiles(_rootFolder, $"*.{_fileExtension}", SearchOption.AllDirectories)
                .OrderBy(path => Path.GetRelativePath(_rootFolder, path), StringComparer.Ordinal);
            foreach (string path in paths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string relativePath = Path.GetRelativePath(_rootFolder, path);
                bool function = relativePath.StartsWith("Functions" + Path.DirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase);
                try
                {
                    files.Add(new SnapshotFile(relativePath, function,
                        File.ReadAllText(FileSystemSchemaExtensions.HandleLongFileNames(path))));
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    throw new SchemaInputException(side, relativePath, "could not be read",
                        "Check that the file exists and is readable, then compare again.", ex);
                }
            }

            return new Snapshot(side, files.ToArray(), _queryEngineFactory);
        }
        catch (Exception ex) when (ex is not SyncKustoException and not OperationCanceledException)
        {
            throw new SchemaLoadException($"Failed to load schema from file system at '{_rootFolder}'", ex);
        }
    }

    /// <summary>
    /// Saves schemas to the file system
    /// </summary>
    public Task SaveSchemaAsync(
        IEnumerable<IKustoSchema> schemas,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(schemas);

        try
        {
            foreach (var schema in schemas)
            {
                // Unwrap the schema to get the actual Kusto SDK types
                if (schema is KustoTableSchema kustoTable)
                {
                    kustoTable.Value.WriteToFile(
                        _rootFolder,
                        _fileExtension,
                        _settings.CreateMergeEnabled,
                        _settings.TableFieldsOnNewLine,
                        _settings.LineEndingMode);
                }
                else if (schema is KustoFunctionSchema kustoFunction)
                {
                    kustoFunction.Value.WriteToFile(_rootFolder, _fileExtension);
                }
                else
                {
                    throw new SchemaSyncException($"Unknown schema type: {schema.GetType().Name}");
                }
            }

            return Task.CompletedTask;
        }
        catch (Exception ex) when (ex is not SyncKustoException)
        {
            throw new SchemaSyncException($"Failed to save schemas to file system at '{_rootFolder}'", ex);
        }
    }

    /// <summary>
    /// Deletes schemas from the file system
    /// </summary>
    public Task DeleteSchemaAsync(
        IEnumerable<IKustoSchema> schemas,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(schemas);

        try
        {
            foreach (var schema in schemas)
            {
                // Unwrap the schema to get the actual Kusto SDK types
                if (schema is KustoTableSchema kustoTable)
                {
                    kustoTable.Value.DeleteFromFolder(_rootFolder, _fileExtension);
                }
                else if (schema is KustoFunctionSchema kustoFunction)
                {
                    kustoFunction.Value.DeleteFromFolder(_rootFolder, _fileExtension);
                }
                else
                {
                    throw new SchemaSyncException($"Unknown schema type: {schema.GetType().Name}");
                }
            }

            return Task.CompletedTask;
        }
        catch (Exception ex) when (ex is not SyncKustoException)
        {
            throw new SchemaSyncException($"Failed to delete schemas from file system at '{_rootFolder}'", ex);
        }
    }

    private sealed record SnapshotFile(string RelativePath, bool Function, string Content);

    private sealed class Snapshot(
        string side, SnapshotFile[] files, Func<ISchemaQueryEngine> queryEngineFactory) : ISchemaSnapshot
    {
        private SnapshotFile[]? _validatedFiles;

        public void Validate()
        {
            _validatedFiles = null;
            var validated = new List<SnapshotFile>();
            foreach (var file in files)
            {
                try
                {
                    string command = file.Function
                        ? SchemaCommandValidator.Function(file.Content)
                        : SchemaCommandValidator.Table(file.Content);
                    validated.Add(file with { Content = command });
                }
                catch (SchemaCommandException ex)
                {
                    throw new SchemaInputException(side, file.RelativePath, ex.Message, ex.Hint, ex,
                        ex.Line, ex.Column);
                }
            }
            _validatedFiles = validated.ToArray();
        }

        public Task<DatabaseSchema> GetSchemaAsync(CancellationToken cancellationToken = default)
        {
            var validated = _validatedFiles ??
                throw new InvalidOperationException("Validate the schema snapshot before loading it.");
            return Task.Run(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var queryEngine = queryEngineFactory();
                // Tables precede functions; skipvalidation permits function dependency order.
                foreach (var file in validated.OrderBy(file => file.Function))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        if (file.Function)
                            await queryEngine.CreateOrAlterFunctionAsync(file.Content, file.RelativePath);
                        else
                            await queryEngine.CreateOrAlterTableAsync(file.Content, file.RelativePath, true);
                    }
                    catch (CreateOrAlterException ex)
                    {
                        throw new SchemaInputException(side, file.RelativePath,
                            "could not be loaded into the temporary database",
                            "Check the definition and supported Kusto types, then compare again.", ex);
                    }
                }
                return queryEngine.GetDatabaseSchema();
            }, cancellationToken);
        }
    }
}
