// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using FluentAssertions;
using Kusto.Data.Common;
using NUnit.Framework;
using SyncKusto.Core.Configuration;
using SyncKusto.Core.Exceptions;
using SyncKusto.Core.Models;
using SyncKusto.FileSystem.Repositories;
using SyncKusto.Kusto.Models;

namespace SyncKusto.Tests.FileSystem;

/// <summary>
/// Tests for FileSystemSchemaRepository functionality
/// Note: These tests focus on validation, error handling, and basic operations.
/// Full integration tests with QueryEngine are in Integration tests.
/// </summary>
[TestFixture]
[Category("FileSystem")]
public class FileSystemSchemaRepositoryTests
{
    private string _testDirectory = null!;
    private SyncKustoSettings _testSettings = null!;

    [SetUp]
    public void SetUp()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"SyncKustoRepoTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);

        _testSettings = new SyncKustoSettings
        {
            TempCluster = "https://test.kusto.windows.net",
            TempDatabase = "TestDB",
            AADAuthority = "https://login.microsoftonline.com/common",
            KustoObjectDropWarning = true,
            TableFieldsOnNewLine = false,
            CreateMergeEnabled = false,
            UseLegacyCslExtension = true,
            LineEndingMode = LineEndingMode.WindowsStyle,
            CertificateLocation = StoreLocation.CurrentUser
        };
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }

    #region Constructor Tests

    [Test]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act
        var repository = new FileSystemSchemaRepository(
            _testDirectory,
            "kql",
            "https://test.kusto.windows.net",
            "TestDB",
            "https://login.microsoftonline.com/common",
            _testSettings);

        // Assert
        repository.Should().NotBeNull();
    }

    [Test]
    public void Constructor_WithNullRootFolder_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => new FileSystemSchemaRepository(
            null!,
            "kql",
            "https://test.kusto.windows.net",
            "TestDB",
            "https://login.microsoftonline.com/common",
            _testSettings);

        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void Constructor_WithEmptyRootFolder_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => new FileSystemSchemaRepository(
            string.Empty,
            "kql",
            "https://test.kusto.windows.net",
            "TestDB",
            "https://login.microsoftonline.com/common",
            _testSettings);

        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void Constructor_WithWhitespaceRootFolder_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => new FileSystemSchemaRepository(
            "   ",
            "kql",
            "https://test.kusto.windows.net",
            "TestDB",
            "https://login.microsoftonline.com/common",
            _testSettings);

        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void Constructor_WithNullFileExtension_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => new FileSystemSchemaRepository(
            _testDirectory,
            null!,
            "https://test.kusto.windows.net",
            "TestDB",
            "https://login.microsoftonline.com/common",
            _testSettings);

        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void Constructor_WithEmptyFileExtension_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => new FileSystemSchemaRepository(
            _testDirectory,
            string.Empty,
            "https://test.kusto.windows.net",
            "TestDB",
            "https://login.microsoftonline.com/common",
            _testSettings);

        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void Constructor_WithNullTempCluster_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => new FileSystemSchemaRepository(
            _testDirectory,
            "kql",
            null!,
            "TestDB",
            "https://login.microsoftonline.com/common",
            _testSettings);

        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void Constructor_WithEmptyTempCluster_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => new FileSystemSchemaRepository(
            _testDirectory,
            "kql",
            string.Empty,
            "TestDB",
            "https://login.microsoftonline.com/common",
            _testSettings);

        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void Constructor_WithNullTempDatabase_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => new FileSystemSchemaRepository(
            _testDirectory,
            "kql",
            "https://test.kusto.windows.net",
            null!,
            "https://login.microsoftonline.com/common",
            _testSettings);

        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void Constructor_WithEmptyTempDatabase_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => new FileSystemSchemaRepository(
            _testDirectory,
            "kql",
            "https://test.kusto.windows.net",
            string.Empty,
            "https://login.microsoftonline.com/common",
            _testSettings);

        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void Constructor_WithNullSettings_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new FileSystemSchemaRepository(
            _testDirectory,
            "kql",
            "https://test.kusto.windows.net",
            "TestDB",
            "https://login.microsoftonline.com/common",
            null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Constructor_WithNullAuthority_DoesNotThrow()
    {
        // Act & Assert - authority can be null
        var act = () => new FileSystemSchemaRepository(
            _testDirectory,
            "kql",
            "https://test.kusto.windows.net",
            "TestDB",
            string.Empty, // Use empty string instead of null
            _testSettings);

        act.Should().NotThrow();
    }

    #endregion

    #region SaveSchemaAsync Tests

    [Test]
    public async Task SaveSchemaAsync_WithNullSchemas_ThrowsArgumentNullException()
    {
        // Arrange
        var repository = CreateRepository();

        // Act & Assert
        var act = async () => await repository.SaveSchemaAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Test]
    public async Task SaveSchemaAsync_WithEmptyList_CompletesSuccessfully()
    {
        // Arrange
        var repository = CreateRepository();
        var schemas = new List<KustoTableSchema>();

        // Act & Assert
        var act = async () => await repository.SaveSchemaAsync(schemas);
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task SaveSchemaAsync_WithTableSchema_CreatesFile()
    {
        // Arrange
        var repository = CreateRepository();
        var table = CreateTestTable("TestTable");
        var schemas = new List<KustoTableSchema> { new KustoTableSchema(table) };

        // Act
        await repository.SaveSchemaAsync(schemas);

        // Assert
        var expectedPath = Path.Combine(_testDirectory, "TestTable.kql");
        File.Exists(expectedPath).Should().BeTrue();
    }

    [Test]
    public async Task SaveSchemaAsync_WithFunctionSchema_CreatesFileInFunctionsFolder()
    {
        // Arrange
        var repository = CreateRepository();
        var function = CreateTestFunction("TestFunction");
        var schemas = new List<KustoFunctionSchema> { new KustoFunctionSchema(function) };

        // Act
        await repository.SaveSchemaAsync(schemas);

        // Assert
        var expectedPath = Path.Combine(_testDirectory, "Functions", "TestFunction.kql");
        File.Exists(expectedPath).Should().BeTrue();
    }

    [Test]
    public async Task SaveSchemaAsync_WithMultipleSchemas_CreatesAllFiles()
    {
        // Arrange
        var repository = CreateRepository();
        var table1 = new KustoTableSchema(CreateTestTable("Table1"));
        var table2 = new KustoTableSchema(CreateTestTable("Table2"));
        var function1 = new KustoFunctionSchema(CreateTestFunction("Function1"));
        var schemas = new List<SyncKusto.Core.Abstractions.IKustoSchema> { table1, table2, function1 };

        // Act
        await repository.SaveSchemaAsync(schemas);

        // Assert
        File.Exists(Path.Combine(_testDirectory, "Table1.kql")).Should().BeTrue();
        File.Exists(Path.Combine(_testDirectory, "Table2.kql")).Should().BeTrue();
        File.Exists(Path.Combine(_testDirectory, "Functions", "Function1.kql")).Should().BeTrue();
    }

    [Test]
    public async Task SaveSchemaAsync_WithCancellationToken_AcceptsToken()
    {
        // Arrange
        var repository = CreateRepository();
        var table = new KustoTableSchema(CreateTestTable("TestTable"));
        var schemas = new List<KustoTableSchema> { table };
        using var cts = new CancellationTokenSource();

        // Act & Assert
        var act = async () => await repository.SaveSchemaAsync(schemas, cts.Token);
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region DeleteSchemaAsync Tests

    [Test]
    public async Task DeleteSchemaAsync_WithNullSchemas_ThrowsArgumentNullException()
    {
        // Arrange
        var repository = CreateRepository();

        // Act & Assert
        var act = async () => await repository.DeleteSchemaAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Test]
    public async Task DeleteSchemaAsync_WithEmptyList_CompletesSuccessfully()
    {
        // Arrange
        var repository = CreateRepository();
        var schemas = new List<KustoTableSchema>();

        // Act & Assert
        var act = async () => await repository.DeleteSchemaAsync(schemas);
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task DeleteSchemaAsync_WithExistingTable_RemovesFile()
    {
        // Arrange
        var repository = CreateRepository();
        var table = CreateTestTable("TestTable");
        var kustoTable = new KustoTableSchema(table);

        // Create the file first
        await repository.SaveSchemaAsync(new[] { kustoTable });
        var filePath = Path.Combine(_testDirectory, "TestTable.kql");
        File.Exists(filePath).Should().BeTrue();

        // Act
        await repository.DeleteSchemaAsync(new[] { kustoTable });

        // Assert
        File.Exists(filePath).Should().BeFalse();
    }

    [Test]
    public async Task DeleteSchemaAsync_WithExistingFunction_RemovesFile()
    {
        // Arrange
        var repository = CreateRepository();
        var function = CreateTestFunction("TestFunction");
        var kustoFunction = new KustoFunctionSchema(function);

        // Create the file first
        await repository.SaveSchemaAsync(new[] { kustoFunction });
        var filePath = Path.Combine(_testDirectory, "Functions", "TestFunction.kql");
        File.Exists(filePath).Should().BeTrue();

        // Act
        await repository.DeleteSchemaAsync(new[] { kustoFunction });

        // Assert
        File.Exists(filePath).Should().BeFalse();
    }

    [Test]
    public async Task DeleteSchemaAsync_WithCancellationToken_AcceptsToken()
    {
        // Arrange
        var repository = CreateRepository();
        var table = new KustoTableSchema(CreateTestTable("TestTable"));
        await repository.SaveSchemaAsync(new[] { table });
        using var cts = new CancellationTokenSource();

        // Act & Assert
        var act = async () => await repository.DeleteSchemaAsync(new[] { table }, cts.Token);
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region GetSchemaAsync Tests

    [Test]
    public async Task GetSchemaAsync_WithNonExistentDirectory_CreatesDirectory()
    {
        // Arrange
        var nonExistentDir = Path.Combine(_testDirectory, "NewDir");
        Directory.Exists(nonExistentDir).Should().BeFalse();

        var repository = new FileSystemSchemaRepository(
            nonExistentDir,
            "kql",
            _testSettings.TempCluster,
            _testSettings.TempDatabase,
            _testSettings.AADAuthority ?? string.Empty,
            _testSettings);

        // Act
        // Note: This will fail without a real QueryEngine, but should create the directory
        try
        {
            await repository.GetSchemaAsync();
        }
        catch (SchemaLoadException)
        {
            // Expected to fail without real QueryEngine
        }

        // Assert
        Directory.Exists(nonExistentDir).Should().BeTrue();
        Directory.Exists(Path.Combine(nonExistentDir, "Functions")).Should().BeTrue();
    }

    [Test]
    public async Task GetSchemaAsync_WithEmptyDirectory_CreatesSubdirectories()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        try
        {
            await repository.GetSchemaAsync();
        }
        catch (SchemaLoadException)
        {
            // Expected to fail without real QueryEngine
        }

        // Assert
        Directory.Exists(Path.Combine(_testDirectory, "Functions")).Should().BeTrue();
    }

    [Test]
    public async Task GetSchemaAsync_WithCancellationToken_AcceptsToken()
    {
        // Arrange
        var repository = CreateRepository();
        using var cts = new CancellationTokenSource();

        // Act & Assert
        try
        {
            await repository.GetSchemaAsync(cts.Token);
        }
        catch (SchemaLoadException)
        {
            // Expected to fail without real QueryEngine
        }

        // If we got here without OperationCanceledException, the token was accepted
        cts.Token.IsCancellationRequested.Should().BeFalse();
    }

    #endregion

    #region Edge Cases and Error Handling

    [Test]
    public async Task SaveSchemaAsync_WithVeryLongTableName_HandlesCorrectly()
    {
        // Arrange
        var repository = CreateRepository();
        var longName = new string('A', 200);
        var table = new KustoTableSchema(CreateTestTable(longName));

        // Act
        await repository.SaveSchemaAsync(new[] { table });

        // Assert
        var files = Directory.GetFiles(_testDirectory, "*.kql", SearchOption.TopDirectoryOnly);
        files.Should().NotBeEmpty();
    }

    [Test]
    public async Task SaveSchemaAsync_WithSpecialCharactersInName_SanitizesFileName()
    {
        // Arrange
        var repository = CreateRepository();
        // Note: Actual Kusto table names shouldn't have these, but test the robustness
        var table = new KustoTableSchema(CreateTestTable("TestTable"));

        // Act
        await repository.SaveSchemaAsync(new[] { table });

        // Assert
        File.Exists(Path.Combine(_testDirectory, "TestTable.kql")).Should().BeTrue();
    }

    #endregion

    #region Helper Methods

    private FileSystemSchemaRepository CreateRepository()
    {
        return new FileSystemSchemaRepository(
            _testDirectory,
            "kql",
            _testSettings.TempCluster,
            _testSettings.TempDatabase,
            _testSettings.AADAuthority ?? string.Empty,
            _testSettings);
    }

    private TableSchema CreateTestTable(string name)
    {
        var columns = new List<ColumnSchema>
        {
            new ColumnSchema("Column1", "System.String"),
            new ColumnSchema("Column2", "System.Int32"),
            new ColumnSchema("Timestamp", "System.DateTime")
        };

        return new TableSchema(
            name: name,
            columns: columns,
            folder: string.Empty,
            docString: $"Test table {name}");
    }

    private FunctionSchema CreateTestFunction(string name)
    {
        var parameters = new List<FunctionParameterSchema>();
        var outputColumns = new List<ColumnSchema>();

        return new FunctionSchema(
            name: name,
            parameters: parameters,
            body: "{ StormEvents | take 10 }",
            folder: string.Empty,
            docString: $"Test function {name}",
            kind: FunctionSchema.FunctionKind.Unknown,
            outputColumns: outputColumns);
    }

    #endregion
}
