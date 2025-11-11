// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using FluentAssertions;
using Kusto.Data.Common;
using NUnit.Framework;
using SyncKusto.Core.Abstractions;
using SyncKusto.Kusto.Extensions;
using SyncKusto.Kusto.Models;

namespace SyncKusto.Tests.Kusto;

/// <summary>
/// Tests for KustoSchemaExtensions functionality
/// </summary>
[TestFixture]
[Category("Kusto")]
public class KustoSchemaExtensionsTests
{
    #region TableSchema Conversion Tests

    [Test]
    public void AsKustoSchema_TableSchema_ReturnsKustoTableSchema()
    {
        // Arrange
        var tableSchema = CreateTestTable("TestTable");

        // Act
        var result = tableSchema.AsKustoSchema();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<KustoTableSchema>();
        result.Name.Should().Be("TestTable");
    }

    [Test]
    public void AsKustoSchema_TableSchema_PreservesTableProperties()
    {
        // Arrange
        var columns = new List<ColumnSchema>
        {
            new ColumnSchema("Column1", "System.String"),
            new ColumnSchema("Column2", "System.Int32")
        };
        var tableSchema = new TableSchema("MyTable", columns, "TestFolder", "Test doc string");

        // Act
        var result = tableSchema.AsKustoSchema();

        // Assert
        result.Should().BeOfType<KustoTableSchema>();
        var kustoTable = (KustoTableSchema)result;
        kustoTable.Value.Name.Should().Be("MyTable");
        kustoTable.Value.Folder.Should().Be("TestFolder");
        kustoTable.Value.DocString.Should().Be("Test doc string");
        kustoTable.Value.OrderedColumns.Should().HaveCount(2);
    }

    [Test]
    public void AsKustoSchema_TableSchemaDictionary_ConvertsAllEntries()
    {
        // Arrange
        var dict = new Dictionary<string, TableSchema>
        {
            { "Table1", CreateTestTable("Table1") },
            { "Table2", CreateTestTable("Table2") },
            { "Table3", CreateTestTable("Table3") }
        };

        // Act
        var result = dict.AsKustoSchema();

        // Assert
        result.Should().HaveCount(3);
        result.Keys.Should().Contain(new[] { "Table1", "Table2", "Table3" });
        result.Values.Should().AllBeOfType<KustoTableSchema>();
    }

    [Test]
    public void AsKustoSchema_EmptyTableSchemaDictionary_ReturnsEmptyDictionary()
    {
        // Arrange
        var dict = new Dictionary<string, TableSchema>();

        // Act
        var result = dict.AsKustoSchema();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion

    #region FunctionSchema Conversion Tests

    [Test]
    public void AsKustoSchema_FunctionSchema_ReturnsKustoFunctionSchema()
    {
        // Arrange
        var functionSchema = CreateTestFunction("TestFunction");

        // Act
        var result = functionSchema.AsKustoSchema();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<KustoFunctionSchema>();
        result.Name.Should().Be("TestFunction");
    }

    [Test]
    public void AsKustoSchema_FunctionSchema_PreservesFunctionProperties()
    {
        // Arrange
        var parameters = new List<FunctionParameterSchema>();
        var outputColumns = new List<ColumnSchema>
        {
            new ColumnSchema("Output", "System.String")
        };
        var functionSchema = new FunctionSchema(
            "MyFunction",
            parameters,
            "{ print 'test' }",
            "TestFolder",
            "Function doc",
            FunctionSchema.FunctionKind.Unknown,
            outputColumns);

        // Act
        var result = functionSchema.AsKustoSchema();

        // Assert
        result.Should().BeOfType<KustoFunctionSchema>();
        var kustoFunction = (KustoFunctionSchema)result;
        kustoFunction.Value.Name.Should().Be("MyFunction");
        kustoFunction.Value.Folder.Should().Be("TestFolder");
        kustoFunction.Value.DocString.Should().Be("Function doc");
        kustoFunction.Value.Body.Should().Be("{ print 'test' }");
        kustoFunction.Value.InputParameters.Should().HaveCount(0);
        kustoFunction.Value.OutputColumns.Should().HaveCount(1);
    }

    [Test]
    public void AsKustoSchema_FunctionSchemaDictionary_ConvertsAllEntries()
    {
        // Arrange
        var dict = new Dictionary<string, FunctionSchema>
        {
            { "Function1", CreateTestFunction("Function1") },
            { "Function2", CreateTestFunction("Function2") },
            { "Function3", CreateTestFunction("Function3") }
        };

        // Act
        var result = dict.AsKustoSchema();

        // Assert
        result.Should().HaveCount(3);
        result.Keys.Should().Contain(new[] { "Function1", "Function2", "Function3" });
        result.Values.Should().AllBeOfType<KustoFunctionSchema>();
    }

    [Test]
    public void AsKustoSchema_EmptyFunctionSchemaDictionary_ReturnsEmptyDictionary()
    {
        // Arrange
        var dict = new Dictionary<string, FunctionSchema>();

        // Act
        var result = dict.AsKustoSchema();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion

    #region IKustoSchema Interface Tests

    [Test]
    public void ConvertedTableSchema_ImplementsIKustoSchema()
    {
        // Arrange
        var tableSchema = CreateTestTable("TestTable");

        // Act
        var result = tableSchema.AsKustoSchema();

        // Assert
        result.Should().BeAssignableTo<IKustoSchema>();
    }

    [Test]
    public void ConvertedFunctionSchema_ImplementsIKustoSchema()
    {
        // Arrange
        var functionSchema = CreateTestFunction("TestFunction");

        // Act
        var result = functionSchema.AsKustoSchema();

        // Assert
        result.Should().BeAssignableTo<IKustoSchema>();
    }

    [Test]
    public void ConvertedSchemas_CanBeStoredInCommonCollection()
    {
        // Arrange
        var tableSchema = CreateTestTable("TestTable");
        var functionSchema = CreateTestFunction("TestFunction");

        // Act
        var schemas = new List<IKustoSchema>
        {
            tableSchema.AsKustoSchema(),
            functionSchema.AsKustoSchema()
        };

        // Assert
        schemas.Should().HaveCount(2);
        schemas[0].Name.Should().Be("TestTable");
        schemas[1].Name.Should().Be("TestFunction");
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

    private FunctionSchema CreateTestFunction(string name, string folder = "")
    {
        var parameters = new List<FunctionParameterSchema>();
        var outputColumns = new List<ColumnSchema>();

        return new FunctionSchema(
            name,
            parameters,
            "{ print 'test' }",
            folder,
            "Test function",
            FunctionSchema.FunctionKind.Unknown,
            outputColumns);
    }

    #endregion
}
