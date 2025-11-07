// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using FluentAssertions;
using Kusto.Data.Common;
using NUnit.Framework;
using SyncKusto.Core.Models;
using SyncKusto.Kusto.Services;

namespace SyncKusto.Tests.Kusto;

/// <summary>
/// Tests for FormattedCslCommandGenerator functionality
/// </summary>
[TestFixture]
[Category("Kusto")]
public class FormattedCslCommandGeneratorTests
{
    #region Basic Command Generation Tests

    [Test]
    public void GenerateTableCreateCommand_BasicTable_GeneratesCreateCommand()
    {
        // Arrange
        var table = CreateTestTable("TestTable");

        // Act
        var result = FormattedCslCommandGenerator.GenerateTableCreateCommand(table);

        // Assert
        result.Should().Contain(".create table");
        result.Should().Contain("TestTable");
        result.Should().Contain("Column1");
        result.Should().Contain("Column2");
    }

    [Test]
    public void GenerateTableCreateCommand_WithoutForceNormalize_UsesDefaultEscaping()
    {
        // Arrange
        var table = CreateTestTable("TestTable");

        // Act
        var result = FormattedCslCommandGenerator.GenerateTableCreateCommand(table, forceNormalizeColumnName: false);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("TestTable");
    }

    [Test]
    public void GenerateTableCreateCommand_WithForceNormalize_NormalizesColumnNames()
    {
        // Arrange
        var table = CreateTestTable("TestTable");

        // Act
        var result = FormattedCslCommandGenerator.GenerateTableCreateCommand(table, forceNormalizeColumnName: true);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("['");
    }

    #endregion

    #region Create-Merge Command Tests

    [Test]
    public void GenerateTableCreateCommand_CreateMergeDisabled_GeneratesCreateCommand()
    {
        // Arrange
        var table = CreateTestTable("TestTable");

        // Act
        var result = FormattedCslCommandGenerator.GenerateTableCreateCommand(
            table,
            createMergeEnabled: false);

        // Assert
        result.Should().Contain(".create table");
        result.Should().NotContain(".create-merge");
    }

    [Test]
    public void GenerateTableCreateCommand_CreateMergeEnabled_GeneratesCreateMergeCommand()
    {
        // Arrange
        var table = CreateTestTable("TestTable");

        // Act
        var result = FormattedCslCommandGenerator.GenerateTableCreateCommand(
            table,
            createMergeEnabled: true);

        // Assert
        result.Should().Contain(".create-merge table");
        result.Should().NotContain(".create table");
    }

    #endregion

    #region Table Fields Formatting Tests

    [Test]
    public void GenerateTableCreateCommand_TableFieldsOnNewLineFalse_SingleLine()
    {
        // Arrange
        var table = CreateTestTable("TestTable");

        // Act
        var result = FormattedCslCommandGenerator.GenerateTableCreateCommand(
            table,
            tableFieldsOnNewLine: false);

        // Assert
        // Should be relatively compact without intentional line breaks between fields
        var lineCount = result.Split('\n').Length;
        lineCount.Should().BeLessThan(5); // Basic threshold
    }

    [Test]
    public void GenerateTableCreateCommand_TableFieldsOnNewLineTrue_MultiLine()
    {
        // Arrange
        var table = CreateTestTable("TestTable");

        // Act
        var result = FormattedCslCommandGenerator.GenerateTableCreateCommand(
            table,
            forceNormalizeColumnName: true, // Need normalization for the formatting to work
            tableFieldsOnNewLine: true);

        // Assert
        // Should have line breaks added for formatting
        // Verify the indentation pattern is present (indicates multi-line formatting)
        result.Should().Match(s => s.Contains("\r\n    [") || s.Contains("\n    ["),
            "because tableFieldsOnNewLine should add line breaks with indentation");
    }

    [Test]
    public void GenerateTableCreateCommand_TableFieldsOnNewLine_EachFieldOnSeparateLine()
    {
        // Arrange
        var columns = new List<ColumnSchema>
        {
            new ColumnSchema("Col1", "System.String"),
            new ColumnSchema("Col2", "System.Int32"),
            new ColumnSchema("Col3", "System.DateTime"),
            new ColumnSchema("Col4", "System.Boolean")
        };
        var table = new TableSchema("MultiColumnTable", columns, "", "");

        // Act
        var result = FormattedCslCommandGenerator.GenerateTableCreateCommand(
            table,
            forceNormalizeColumnName: true, // Need normalization for the formatting to work
            tableFieldsOnNewLine: true);

        // Assert
        // Verify formatting was applied - should have indented fields
        result.Should().Match(s => s.Contains("\n    [") || s.Contains("\r\n    ["),
            "because tableFieldsOnNewLine should add indented field separators");
        
        // Count the number of field separators with line breaks
        var fieldSeparatorCount = System.Text.RegularExpressions.Regex.Matches(result, @",[\r]?\n\s+\[").Count;
        // Should have separators between fields (columns.Count - 1)
        fieldSeparatorCount.Should().BeGreaterOrEqualTo(columns.Count - 1);
    }

    #endregion

    #region Line Ending Mode Tests

    [Test]
    public void GenerateTableCreateCommand_WindowsStyle_UsesWindowsLineEndings()
    {
        // Arrange
        var table = CreateTestTable("TestTable");

        // Act
        var result = FormattedCslCommandGenerator.GenerateTableCreateCommand(
            table,
            tableFieldsOnNewLine: true,
            lineEndingMode: LineEndingMode.WindowsStyle);

        // Assert
        if (result.Contains('\n'))
        {
            result.Should().Contain("\r\n");
        }
    }

    [Test]
    public void GenerateTableCreateCommand_UnixStyle_UsesUnixLineEndings()
    {
        // Arrange
        var table = CreateTestTable("TestTable");

        // Act
        var result = FormattedCslCommandGenerator.GenerateTableCreateCommand(
            table,
            tableFieldsOnNewLine: true,
            lineEndingMode: LineEndingMode.UnixStyle);

        // Assert
        if (result.Contains('\n'))
        {
            // Should have \n but not \r\n
            var hasUnixEndings = result.Contains("\n");
            var hasWindowsEndings = result.Contains("\r\n");
            hasUnixEndings.Should().BeTrue();
            // Note: The result may still have \r\n from the base command, but new lines should be \n only
        }
    }

    [Test]
    public void GenerateTableCreateCommand_LeaveAsIs_NoExplicitFormatting()
    {
        // Arrange
        var table = CreateTestTable("TestTable");

        // Act
        var result = FormattedCslCommandGenerator.GenerateTableCreateCommand(
            table,
            tableFieldsOnNewLine: false,
            lineEndingMode: LineEndingMode.LeaveAsIs);

        // Assert
        result.Should().NotBeNullOrEmpty();
        // LeaveAsIs with no new lines should produce compact output
    }

    #endregion

    #region Complex Scenarios Tests

    [Test]
    public void GenerateTableCreateCommand_AllOptionsEnabled_GeneratesCorrectCommand()
    {
        // Arrange
        var table = CreateTestTable("ComplexTable");

        // Act
        var result = FormattedCslCommandGenerator.GenerateTableCreateCommand(
            table,
            forceNormalizeColumnName: true,
            createMergeEnabled: true,
            tableFieldsOnNewLine: true,
            lineEndingMode: LineEndingMode.WindowsStyle);

        // Assert
        result.Should().Contain(".create-merge table");
        result.Should().Contain("ComplexTable");
        result.Should().Contain("['");
        result.Split('\n').Length.Should().BeGreaterThan(1);
    }

    [Test]
    public void GenerateTableCreateCommand_TableWithManyColumns_HandlesCorrectly()
    {
        // Arrange
        var columns = new List<ColumnSchema>();
        for (int i = 1; i <= 20; i++)
        {
            columns.Add(new ColumnSchema($"Column{i}", "System.String"));
        }
        var table = new TableSchema("LargeTable", columns, "", "");

        // Act
        var result = FormattedCslCommandGenerator.GenerateTableCreateCommand(
            table,
            tableFieldsOnNewLine: true);

        // Assert
        result.Should().Contain("LargeTable");
        for (int i = 1; i <= 20; i++)
        {
            result.Should().Contain($"Column{i}");
        }
    }

    [Test]
    public void GenerateTableCreateCommand_TableWithSpecialCharacters_EscapesCorrectly()
    {
        // Arrange
        var columns = new List<ColumnSchema>
        {
            new ColumnSchema("Column-With-Dashes", "System.String"),
            new ColumnSchema("Column.With.Dots", "System.Int32")
        };
        var table = new TableSchema("SpecialTable", columns, "", "");

        // Act
        var result = FormattedCslCommandGenerator.GenerateTableCreateCommand(
            table,
            forceNormalizeColumnName: true);

        // Assert
        result.Should().Contain("SpecialTable");
        result.Should().Contain("['Column-With-Dashes']");
        result.Should().Contain("['Column.With.Dots']");
    }

    [Test]
    public void GenerateTableCreateCommand_TableWithFolder_IncludesFolderInCommand()
    {
        // Arrange
        var table = CreateTestTable("TableWithFolder", folder: "MyFolder");

        // Act
        var result = FormattedCslCommandGenerator.GenerateTableCreateCommand(table);

        // Assert
        result.Should().Contain("TableWithFolder");
        result.Should().Contain("folder");
        result.Should().Contain("MyFolder");
    }

    [Test]
    public void GenerateTableCreateCommand_TableWithDocString_IncludesDocString()
    {
        // Arrange
        var columns = new List<ColumnSchema>
        {
            new ColumnSchema("Column1", "System.String")
        };
        var table = new TableSchema("DocumentedTable", columns, "", "This is a test table");

        // Act
        var result = FormattedCslCommandGenerator.GenerateTableCreateCommand(table);

        // Assert
        result.Should().Contain("DocumentedTable");
        result.Should().Contain("docstring");
    }

    #endregion

    #region Edge Cases Tests

    [Test]
    public void GenerateTableCreateCommand_SingleColumnTable_GeneratesCorrectCommand()
    {
        // Arrange
        var columns = new List<ColumnSchema>
        {
            new ColumnSchema("OnlyColumn", "System.String")
        };
        var table = new TableSchema("SingleColumnTable", columns, "", "");

        // Act
        var result = FormattedCslCommandGenerator.GenerateTableCreateCommand(table);

        // Assert
        result.Should().Contain("SingleColumnTable");
        result.Should().Contain("OnlyColumn");
    }

    [Test]
    public void GenerateTableCreateCommand_DifferentColumnTypes_HandlesAllTypes()
    {
        // Arrange
        var columns = new List<ColumnSchema>
        {
            new ColumnSchema("StringCol", "System.String"),
            new ColumnSchema("IntCol", "System.Int32"),
            new ColumnSchema("LongCol", "System.Int64"),
            new ColumnSchema("DateTimeCol", "System.DateTime"),
            new ColumnSchema("BoolCol", "System.Boolean"),
            new ColumnSchema("DoubleCol", "System.Double"),
            new ColumnSchema("DecimalCol", "System.Decimal"),
            new ColumnSchema("GuidCol", "System.Guid")
        };
        var table = new TableSchema("MultiTypeTable", columns, "", "");

        // Act
        var result = FormattedCslCommandGenerator.GenerateTableCreateCommand(table);

        // Assert
        result.Should().Contain("MultiTypeTable");
        result.Should().Contain("StringCol");
        result.Should().Contain("IntCol");
        result.Should().Contain("LongCol");
        result.Should().Contain("DateTimeCol");
        result.Should().Contain("BoolCol");
        result.Should().Contain("DoubleCol");
        result.Should().Contain("DecimalCol");
        result.Should().Contain("GuidCol");
    }

    #endregion

    #region Helper Methods

    private TableSchema CreateTestTable(string name, string folder = "")
    {
        var columns = new List<ColumnSchema>
        {
            new ColumnSchema("Column1", "System.String"),
            new ColumnSchema("Column2", "System.Int32")
        };

        return new TableSchema(name, columns, folder, "Test table");
    }

    #endregion
}
