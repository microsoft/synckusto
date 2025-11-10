// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using FluentAssertions;
using Kusto.Data.Common;
using NUnit.Framework;
using SyncKusto.Core.Abstractions;
using SyncKusto.Kusto.Models;

namespace SyncKusto.Tests.Kusto;

/// <summary>
/// Tests for KustoTableSchema and KustoFunctionSchema model functionality
/// </summary>
[TestFixture]
[Category("Kusto")]
public class KustoSchemaModelsTests
{
    #region KustoTableSchema Construction Tests

    [Test]
    public void KustoTableSchema_Constructor_WithValidTableSchema_CreatesInstance()
    {
        // Arrange
        var tableSchema = CreateTestTable("TestTable");

        // Act
        var kustoTable = new KustoTableSchema(tableSchema);

        // Assert
        kustoTable.Should().NotBeNull();
        kustoTable.Value.Should().Be(tableSchema);
        kustoTable.Name.Should().Be("TestTable");
    }

    [Test]
    public void KustoTableSchema_Constructor_WithNullTableSchema_ThrowsArgumentNullException()
    {
        // Arrange
        TableSchema? nullSchema = null;

        // Act
        var act = () => new KustoTableSchema(nullSchema!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void KustoTableSchema_Name_ReturnsTableSchemaName()
    {
        // Arrange
        var tableSchema = CreateTestTable("MySpecialTable");
        var kustoTable = new KustoTableSchema(tableSchema);

        // Act
        var name = kustoTable.Name;

        // Assert
        name.Should().Be("MySpecialTable");
    }

    #endregion

    #region KustoTableSchema Equality Tests

    [Test]
    public void KustoTableSchema_Equals_SameInstance_ReturnsTrue()
    {
        // Arrange
        var tableSchema = CreateTestTable("TestTable");
        var kustoTable = new KustoTableSchema(tableSchema);

        // Act
        var result = kustoTable.Equals(kustoTable);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void KustoTableSchema_Equals_SameValue_ReturnsTrue()
    {
        // Arrange
        var tableSchema = CreateTestTable("TestTable");
        var kustoTable1 = new KustoTableSchema(tableSchema);
        var kustoTable2 = new KustoTableSchema(tableSchema);

        // Act
        var result = kustoTable1.Equals(kustoTable2);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void KustoTableSchema_Equals_DifferentValue_ReturnsFalse()
    {
        // Arrange
        var tableSchema1 = CreateTestTable("Table1");
        var tableSchema2 = CreateTestTable("Table2");
        var kustoTable1 = new KustoTableSchema(tableSchema1);
        var kustoTable2 = new KustoTableSchema(tableSchema2);

        // Act
        var result = kustoTable1.Equals(kustoTable2);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void KustoTableSchema_Equals_Null_ReturnsFalse()
    {
        // Arrange
        var tableSchema = CreateTestTable("TestTable");
        var kustoTable = new KustoTableSchema(tableSchema);

        // Act
        var result = kustoTable.Equals(null);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void KustoTableSchema_EqualsOperator_SameValue_ReturnsTrue()
    {
        // Arrange
        var tableSchema = CreateTestTable("TestTable");
        var kustoTable1 = new KustoTableSchema(tableSchema);
        var kustoTable2 = new KustoTableSchema(tableSchema);

        // Act
        var result = kustoTable1 == kustoTable2;

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void KustoTableSchema_NotEqualsOperator_DifferentValue_ReturnsTrue()
    {
        // Arrange
        var tableSchema1 = CreateTestTable("Table1");
        var tableSchema2 = CreateTestTable("Table2");
        var kustoTable1 = new KustoTableSchema(tableSchema1);
        var kustoTable2 = new KustoTableSchema(tableSchema2);

        // Act
        var result = kustoTable1 != kustoTable2;

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void KustoTableSchema_GetHashCode_SameValue_ReturnsSameHashCode()
    {
        // Arrange
        var tableSchema = CreateTestTable("TestTable");
        var kustoTable1 = new KustoTableSchema(tableSchema);
        var kustoTable2 = new KustoTableSchema(tableSchema);

        // Act
        var hash1 = kustoTable1.GetHashCode();
        var hash2 = kustoTable2.GetHashCode();

        // Assert
        hash1.Should().Be(hash2);
    }

    #endregion

    #region KustoTableSchema Conversion Tests

    [Test]
    public void KustoTableSchema_ImplicitConversion_ToTableSchema_Works()
    {
        // Arrange
        var tableSchema = CreateTestTable("TestTable");
        var kustoTable = new KustoTableSchema(tableSchema);

        // Act
        TableSchema convertedSchema = kustoTable;

        // Assert
        convertedSchema.Should().Be(tableSchema);
    }

    [Test]
    public void KustoTableSchema_ImplementsIKustoSchema_CanBeAssigned()
    {
        // Arrange
        var tableSchema = CreateTestTable("TestTable");
        var kustoTable = new KustoTableSchema(tableSchema);

        // Act
        IKustoSchema schema = kustoTable;

        // Assert
        schema.Should().NotBeNull();
        schema.Name.Should().Be("TestTable");
    }

    #endregion

    #region KustoFunctionSchema Construction Tests

    [Test]
    public void KustoFunctionSchema_Constructor_WithValidFunctionSchema_CreatesInstance()
    {
        // Arrange
        var functionSchema = CreateTestFunction("TestFunction");

        // Act
        var kustoFunction = new KustoFunctionSchema(functionSchema);

        // Assert
        kustoFunction.Should().NotBeNull();
        kustoFunction.Value.Should().Be(functionSchema);
        kustoFunction.Name.Should().Be("TestFunction");
    }

    [Test]
    public void KustoFunctionSchema_Constructor_WithNullFunctionSchema_ThrowsArgumentNullException()
    {
        // Arrange
        FunctionSchema? nullSchema = null;

        // Act
        var act = () => new KustoFunctionSchema(nullSchema!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void KustoFunctionSchema_Name_ReturnsFunctionSchemaName()
    {
        // Arrange
        var functionSchema = CreateTestFunction("MySpecialFunction");
        var kustoFunction = new KustoFunctionSchema(functionSchema);

        // Act
        var name = kustoFunction.Name;

        // Assert
        name.Should().Be("MySpecialFunction");
    }

    #endregion

    #region KustoFunctionSchema Equality Tests

    [Test]
    public void KustoFunctionSchema_Equals_SameInstance_ReturnsTrue()
    {
        // Arrange
        var functionSchema = CreateTestFunction("TestFunction");
        var kustoFunction = new KustoFunctionSchema(functionSchema);

        // Act
        var result = kustoFunction.Equals(kustoFunction);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void KustoFunctionSchema_Equals_SameValue_ReturnsTrue()
    {
        // Arrange
        var functionSchema = CreateTestFunction("TestFunction");
        var kustoFunction1 = new KustoFunctionSchema(functionSchema);
        var kustoFunction2 = new KustoFunctionSchema(functionSchema);

        // Act
        var result = kustoFunction1.Equals(kustoFunction2);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void KustoFunctionSchema_Equals_DifferentValue_ReturnsFalse()
    {
        // Arrange
        var functionSchema1 = CreateTestFunction("Function1");
        var functionSchema2 = CreateTestFunction("Function2");
        var kustoFunction1 = new KustoFunctionSchema(functionSchema1);
        var kustoFunction2 = new KustoFunctionSchema(functionSchema2);

        // Act
        var result = kustoFunction1.Equals(kustoFunction2);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void KustoFunctionSchema_Equals_Null_ReturnsFalse()
    {
        // Arrange
        var functionSchema = CreateTestFunction("TestFunction");
        var kustoFunction = new KustoFunctionSchema(functionSchema);

        // Act
        var result = kustoFunction.Equals(null);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void KustoFunctionSchema_EqualsOperator_SameValue_ReturnsTrue()
    {
        // Arrange
        var functionSchema = CreateTestFunction("TestFunction");
        var kustoFunction1 = new KustoFunctionSchema(functionSchema);
        var kustoFunction2 = new KustoFunctionSchema(functionSchema);

        // Act
        var result = kustoFunction1 == kustoFunction2;

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void KustoFunctionSchema_NotEqualsOperator_DifferentValue_ReturnsTrue()
    {
        // Arrange
        var functionSchema1 = CreateTestFunction("Function1");
        var functionSchema2 = CreateTestFunction("Function2");
        var kustoFunction1 = new KustoFunctionSchema(functionSchema1);
        var kustoFunction2 = new KustoFunctionSchema(functionSchema2);

        // Act
        var result = kustoFunction1 != kustoFunction2;

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void KustoFunctionSchema_GetHashCode_SameValue_ReturnsSameHashCode()
    {
        // Arrange
        var functionSchema = CreateTestFunction("TestFunction");
        var kustoFunction1 = new KustoFunctionSchema(functionSchema);
        var kustoFunction2 = new KustoFunctionSchema(functionSchema);

        // Act
        var hash1 = kustoFunction1.GetHashCode();
        var hash2 = kustoFunction2.GetHashCode();

        // Assert
        hash1.Should().Be(hash2);
    }

    #endregion

    #region KustoFunctionSchema Conversion Tests

    [Test]
    public void KustoFunctionSchema_ImplicitConversion_ToFunctionSchema_Works()
    {
        // Arrange
        var functionSchema = CreateTestFunction("TestFunction");
        var kustoFunction = new KustoFunctionSchema(functionSchema);

        // Act
        FunctionSchema convertedSchema = kustoFunction;

        // Assert
        convertedSchema.Should().Be(functionSchema);
    }

    [Test]
    public void KustoFunctionSchema_ImplementsIKustoSchema_CanBeAssigned()
    {
        // Arrange
        var functionSchema = CreateTestFunction("TestFunction");
        var kustoFunction = new KustoFunctionSchema(functionSchema);

        // Act
        IKustoSchema schema = kustoFunction;

        // Assert
        schema.Should().NotBeNull();
        schema.Name.Should().Be("TestFunction");
    }

    #endregion

    #region Mixed Type Tests

    [Test]
    public void KustoTableSchema_NotEqualToKustoFunctionSchema()
    {
        // Arrange
        var tableSchema = CreateTestTable("TestSchema");
        var functionSchema = CreateTestFunction("TestSchema");
        var kustoTable = new KustoTableSchema(tableSchema);
        var kustoFunction = new KustoFunctionSchema(functionSchema);

        // Act & Assert
        kustoTable.Equals(kustoFunction).Should().BeFalse();
        kustoFunction.Equals(kustoTable).Should().BeFalse();
    }

    [Test]
    public void IKustoSchema_CanHoldBothTableAndFunction()
    {
        // Arrange
        var tableSchema = CreateTestTable("TestTable");
        var functionSchema = CreateTestFunction("TestFunction");
        var kustoTable = new KustoTableSchema(tableSchema);
        var kustoFunction = new KustoFunctionSchema(functionSchema);

        // Act
        var schemas = new List<IKustoSchema> { kustoTable, kustoFunction };

        // Assert
        schemas.Should().HaveCount(2);
        schemas[0].Name.Should().Be("TestTable");
        schemas[1].Name.Should().Be("TestFunction");
    }

    #endregion

    #region Complex Schema Tests

    [Test]
    public void KustoTableSchema_WithComplexTable_PreservesAllProperties()
    {
        // Arrange
        var columns = new List<ColumnSchema>
        {
            new ColumnSchema("Col1", "System.String"),
            new ColumnSchema("Col2", "System.Int32"),
            new ColumnSchema("Col3", "System.DateTime")
        };
        var tableSchema = new TableSchema("ComplexTable", columns, "TestFolder", "This is a test table");

        // Act
        var kustoTable = new KustoTableSchema(tableSchema);

        // Assert
        kustoTable.Value.Name.Should().Be("ComplexTable");
        kustoTable.Value.Folder.Should().Be("TestFolder");
        kustoTable.Value.DocString.Should().Be("This is a test table");
        kustoTable.Value.OrderedColumns.Should().HaveCount(3);
    }

    [Test]
    public void KustoFunctionSchema_WithComplexFunction_PreservesAllProperties()
    {
        // Arrange
        var parameters = new List<FunctionParameterSchema>();
        var outputColumns = new List<ColumnSchema>
        {
            new ColumnSchema("Output1", "System.String"),
            new ColumnSchema("Output2", "System.Int32")
        };
        var functionSchema = new FunctionSchema(
            "ComplexFunction",
            parameters,
            "{ print param1, param2 }",
            "TestFolder",
            "This is a test function",
            FunctionSchema.FunctionKind.Unknown,
            outputColumns);

        // Act
        var kustoFunction = new KustoFunctionSchema(functionSchema);

        // Assert
        kustoFunction.Value.Name.Should().Be("ComplexFunction");
        kustoFunction.Value.Folder.Should().Be("TestFolder");
        kustoFunction.Value.DocString.Should().Be("This is a test function");
        kustoFunction.Value.Body.Should().Contain("param1");
        kustoFunction.Value.InputParameters.Should().HaveCount(0);
        kustoFunction.Value.OutputColumns.Should().HaveCount(2);
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
