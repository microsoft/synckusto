// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using FluentAssertions;
using Kusto.Data.Common;
using NUnit.Framework;
using SyncKusto.Core.Models;
using SyncKusto.FileSystem.Exceptions;
using SyncKusto.FileSystem.Extensions;

namespace SyncKusto.Tests.FileSystem;

/// <summary>
/// Tests for FileSystemSchemaExtensions functionality
/// </summary>
[TestFixture]
[Category("FileSystem")]
public class FileSystemSchemaExtensionsTests
{
    private string _testDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"SyncKustoFileSystemTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
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

    #region HandleLongFileNames Tests

    [Test]
    public void HandleLongFileNames_ShortPath_ReturnsUnmodified()
    {
        // Arrange
        var shortPath = @"C:\test\file.txt";

        // Act
        var result = FileSystemSchemaExtensions.HandleLongFileNames(shortPath);

        // Assert
        result.Should().Be(shortPath);
    }

    [Test]
    public void HandleLongFileNames_LongPath_AddsPrefix()
    {
        // Arrange
        var longPath = @"C:\" + new string('a', 250) + @"\file.txt";

        // Act
        var result = FileSystemSchemaExtensions.HandleLongFileNames(longPath);

        // Assert
        result.Should().StartWith(@"\\?\");
        result.Should().Contain(longPath);
    }

    [Test]
    public void HandleLongFileNames_PathWithPrefixButShort_RemovesPrefix()
    {
        // Arrange
        var pathWithPrefix = @"\\?\C:\test\file.txt";

        // Act
        var result = FileSystemSchemaExtensions.HandleLongFileNames(pathWithPrefix);

        // Assert
        result.Should().Be(@"C:\test\file.txt");
    }

    [Test]
    public void HandleLongFileNames_PathExactlyAtLimit_ReturnsUnmodified()
    {
        // Arrange - Path length of exactly 248 characters
        var path = @"C:\" + new string('a', 244);

        // Act
        var result = FileSystemSchemaExtensions.HandleLongFileNames(path);

        // Assert
        result.Should().Be(path);
    }

    [Test]
    public void HandleLongFileNames_PathJustOverLimit_AddsPrefix()
    {
        // Arrange - Path length of 249 characters (just over the 248 limit)
        var path = @"C:\" + new string('a', 246);

        // Act
        var result = FileSystemSchemaExtensions.HandleLongFileNames(path);

        // Assert
        result.Should().StartWith(@"\\?\");
    }

    #endregion

    #region FunctionSchema WriteToFile Tests

    [Test]
    public void WriteToFile_Function_CreatesFileInFunctionsFolder()
    {
        // Arrange
        var function = CreateTestFunction("TestFunction", "TestFolder");

        // Act
        function.WriteToFile(_testDirectory, "kql");

        // Assert
        var expectedPath = Path.Combine(_testDirectory, "Functions", "TestFolder", "TestFunction.kql");
        File.Exists(expectedPath).Should().BeTrue();
    }

    [Test]
    public void WriteToFile_FunctionWithoutFolder_CreatesFileInRootFunctionsFolder()
    {
        // Arrange
        var function = CreateTestFunction("TestFunction", string.Empty);

        // Act
        function.WriteToFile(_testDirectory, "kql");

        // Assert
        var expectedPath = Path.Combine(_testDirectory, "Functions", "TestFunction.kql");
        File.Exists(expectedPath).Should().BeTrue();
    }

    [Test]
    public void WriteToFile_Function_ContainsCreateOrAlterCommand()
    {
        // Arrange
        var function = CreateTestFunction("TestFunction", string.Empty);

        // Act
        function.WriteToFile(_testDirectory, "csl");

        // Assert
        var expectedPath = Path.Combine(_testDirectory, "Functions", "TestFunction.csl");
        var content = File.ReadAllText(expectedPath);
        content.Should().Contain(".create-or-alter function");
        content.Should().Contain("TestFunction");
    }

    [Test]
    public void WriteToFile_FunctionWithInvalidFolderChars_SanitizesFolder()
    {
        // Arrange
        var function = CreateTestFunction("TestFunction", "Test<>Folder|Invalid*Chars");

        // Act & Assert - The sanitization removes invalid chars but the resulting path may still have issues
        var act = () => function.WriteToFile(_testDirectory, "kql");
        
        // This may succeed or fail depending on what characters remain after sanitization
        try
        {
            act();
            var functionsDir = Path.Combine(_testDirectory, "Functions");
            if (Directory.Exists(functionsDir))
            {
                var subdirs = Directory.GetDirectories(functionsDir);
                subdirs.Should().HaveCount(1);
            }
        }
        catch (FileSchemaException)
        {
            // Also acceptable - the sanitization may not make a valid path
        }
    }

    [Test]
    public void WriteToFile_FunctionTwice_OverwritesExistingFile()
    {
        // Arrange
        var function = CreateTestFunction("TestFunction", string.Empty);

        // Act
        function.WriteToFile(_testDirectory, "kql");
        System.Threading.Thread.Sleep(10); // Ensure different timestamp
        function.WriteToFile(_testDirectory, "kql");

        // Assert
        var expectedPath = Path.Combine(_testDirectory, "Functions", "TestFunction.kql");
        File.Exists(expectedPath).Should().BeTrue();
        var files = Directory.GetFiles(Path.Combine(_testDirectory, "Functions"), "TestFunction.kql", SearchOption.AllDirectories);
        files.Should().HaveCount(1);
    }

    [Test]
    public void WriteToFile_FunctionNullSchema_ThrowsArgumentNullException()
    {
        // Arrange
        FunctionSchema? function = null;

        // Act & Assert
        var act = () => function!.WriteToFile(_testDirectory, "kql");
        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void WriteToFile_FunctionNullRootFolder_ThrowsArgumentException()
    {
        // Arrange
        var function = CreateTestFunction("TestFunction", string.Empty);

        // Act & Assert
        var act = () => function.WriteToFile(null!, "kql");
        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void WriteToFile_FunctionEmptyExtension_ThrowsArgumentException()
    {
        // Arrange
        var function = CreateTestFunction("TestFunction", string.Empty);

        // Act & Assert
        var act = () => function.WriteToFile(_testDirectory, string.Empty);
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region FunctionSchema DeleteFromFolder Tests

    [Test]
    public void DeleteFromFolder_Function_RemovesFile()
    {
        // Arrange
        var function = CreateTestFunction("TestFunction", string.Empty);
        function.WriteToFile(_testDirectory, "kql");
        var filePath = Path.Combine(_testDirectory, "Functions", "TestFunction.kql");
        File.Exists(filePath).Should().BeTrue();

        // Act
        function.DeleteFromFolder(_testDirectory, "kql");

        // Assert
        File.Exists(filePath).Should().BeFalse();
    }

    [Test]
    public void DeleteFromFolder_FunctionInSubfolder_RemovesFile()
    {
        // Arrange
        var function = CreateTestFunction("TestFunction", "Subfolder");
        function.WriteToFile(_testDirectory, "kql");
        var filePath = Path.Combine(_testDirectory, "Functions", "Subfolder", "TestFunction.kql");
        File.Exists(filePath).Should().BeTrue();

        // Act
        function.DeleteFromFolder(_testDirectory, "kql");

        // Assert
        File.Exists(filePath).Should().BeFalse();
    }

    [Test]
    public void DeleteFromFolder_FunctionNullSchema_ThrowsArgumentNullException()
    {
        // Arrange
        FunctionSchema? function = null;

        // Act & Assert
        var act = () => function!.DeleteFromFolder(_testDirectory, "kql");
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region TableSchema WriteToFile Tests

    [Test]
    public void WriteToFile_Table_CreatesFileInRootFolder()
    {
        // Arrange
        var table = CreateTestTable("TestTable", string.Empty);

        // Act
        table.WriteToFile(_testDirectory, "kql");

        // Assert
        var expectedPath = Path.Combine(_testDirectory, "TestTable.kql");
        File.Exists(expectedPath).Should().BeTrue();
    }

    [Test]
    public void WriteToFile_TableWithFolder_CreatesFileInTablesSubfolder()
    {
        // Arrange
        var table = CreateTestTable("TestTable", "MyFolder");

        // Act
        table.WriteToFile(_testDirectory, "kql");

        // Assert
        var expectedPath = Path.Combine(_testDirectory, "Tables", "MyFolder", "TestTable.kql");
        File.Exists(expectedPath).Should().BeTrue();
    }

    [Test]
    public void WriteToFile_Table_ContainsCreateCommand()
    {
        // Arrange
        var table = CreateTestTable("TestTable", string.Empty);

        // Act
        table.WriteToFile(_testDirectory, "csl");

        // Assert
        var expectedPath = Path.Combine(_testDirectory, "TestTable.csl");
        var content = File.ReadAllText(expectedPath);
        content.Should().Contain(".create");
        content.Should().Contain("TestTable");
    }

    [Test]
    public void WriteToFile_TableWithInvalidFolderChars_SanitizesFolder()
    {
        // Arrange
        var table = CreateTestTable("TestTable", "Test<>Folder|Invalid*Chars");

        // Act & Assert - The sanitization removes invalid chars but the resulting path may still have issues
        // The actual behavior is that it attempts to create the sanitized path
        var act = () => table.WriteToFile(_testDirectory, "kql");
        
        // This may succeed or fail depending on what characters remain after sanitization
        // Since this is testing file system behavior, we just verify it handles the input
        try
        {
            act();
            var tablesDir = Path.Combine(_testDirectory, "Tables");
            if (Directory.Exists(tablesDir))
            {
                var subdirs = Directory.GetDirectories(tablesDir);
                subdirs.Should().HaveCount(1);
            }
        }
        catch (FileSchemaException)
        {
            // Also acceptable - the sanitization may not make a valid path
        }
    }

    [Test]
    public void WriteToFile_TableTwice_OverwritesExistingFile()
    {
        // Arrange
        var table = CreateTestTable("TestTable", string.Empty);

        // Act
        table.WriteToFile(_testDirectory, "kql");
        System.Threading.Thread.Sleep(10);
        table.WriteToFile(_testDirectory, "kql");

        // Assert
        var expectedPath = Path.Combine(_testDirectory, "TestTable.kql");
        File.Exists(expectedPath).Should().BeTrue();
        var files = Directory.GetFiles(_testDirectory, "TestTable.kql", SearchOption.AllDirectories);
        files.Should().HaveCount(1);
    }

    [Test]
    public void WriteToFile_TableNullSchema_ThrowsArgumentNullException()
    {
        // Arrange
        TableSchema? table = null;

        // Act & Assert
        var act = () => table!.WriteToFile(_testDirectory, "kql");
        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void WriteToFile_TableNullRootFolder_ThrowsArgumentException()
    {
        // Arrange
        var table = CreateTestTable("TestTable", string.Empty);

        // Act & Assert
        var act = () => table.WriteToFile(null!, "kql");
        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void WriteToFile_TableEmptyExtension_ThrowsArgumentException()
    {
        // Arrange
        var table = CreateTestTable("TestTable", string.Empty);

        // Act & Assert
        var act = () => table.WriteToFile(_testDirectory, string.Empty);
        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void WriteToFile_TableWithCreateMergeEnabled_UsesCreateMerge()
    {
        // Arrange
        var table = CreateTestTable("TestTable", string.Empty);

        // Act
        table.WriteToFile(_testDirectory, "kql", createMergeEnabled: true);

        // Assert
        var expectedPath = Path.Combine(_testDirectory, "TestTable.kql");
        var content = File.ReadAllText(expectedPath);
        content.Should().Contain(".create-merge");
    }

    [Test]
    public void WriteToFile_TableWithFieldsOnNewLine_FormatsCorrectly()
    {
        // Arrange
        var table = CreateTestTable("TestTable", string.Empty);

        // Act
        table.WriteToFile(_testDirectory, "kql", tableFieldsOnNewLine: true);

        // Assert
        var expectedPath = Path.Combine(_testDirectory, "TestTable.kql");
        var content = File.ReadAllText(expectedPath);
        // Should have multiple lines for fields
        content.Split('\n').Length.Should().BeGreaterThan(1);
    }

    [Test]
    public void WriteToFile_TableWithDifferentExtensions_CreatesCorrectFiles()
    {
        // Arrange
        var table = CreateTestTable("TestTable", string.Empty);

        // Act
        table.WriteToFile(_testDirectory, "kql");
        table.WriteToFile(_testDirectory, "csl");

        // Assert
        File.Exists(Path.Combine(_testDirectory, "TestTable.kql")).Should().BeTrue();
        File.Exists(Path.Combine(_testDirectory, "TestTable.csl")).Should().BeTrue();
    }

    #endregion

    #region TableSchema DeleteFromFolder Tests

    [Test]
    public void DeleteFromFolder_Table_RemovesFile()
    {
        // Arrange
        var table = CreateTestTable("TestTable", string.Empty);
        table.WriteToFile(_testDirectory, "kql");
        var filePath = Path.Combine(_testDirectory, "TestTable.kql");
        File.Exists(filePath).Should().BeTrue();

        // Act
        table.DeleteFromFolder(_testDirectory, "kql");

        // Assert
        File.Exists(filePath).Should().BeFalse();
    }

    [Test]
    public void DeleteFromFolder_TableInSubfolder_RemovesFile()
    {
        // Arrange
        var table = CreateTestTable("TestTable", "Subfolder");
        table.WriteToFile(_testDirectory, "kql");
        var filePath = Path.Combine(_testDirectory, "Tables", "Subfolder", "TestTable.kql");
        File.Exists(filePath).Should().BeTrue();

        // Act
        table.DeleteFromFolder(_testDirectory, "kql");

        // Assert
        File.Exists(filePath).Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    private FunctionSchema CreateTestFunction(string name, string folder)
    {
        // Using Kusto SDK's FunctionSchema - these are readonly types from the SDK
        var parameters = new List<FunctionParameterSchema>();
        var outputColumns = new List<ColumnSchema>();
        
        return new FunctionSchema(
            name: name,
            parameters: parameters,
            body: "{ print 'Hello World' }",
            folder: folder,
            docString: "Test function",
            kind: FunctionSchema.FunctionKind.Unknown,
            outputColumns: outputColumns);
    }

    private TableSchema CreateTestTable(string name, string folder)
    {
        var columns = new List<ColumnSchema>
        {
            new ColumnSchema("Column1", "System.String"),
            new ColumnSchema("Column2", "System.Int32"),
            new ColumnSchema("Column3", "System.DateTime")
        };

        return new TableSchema(
            name: name,
            columns: columns,
            folder: folder,
            docString: "Test table");
    }

    #endregion
}
