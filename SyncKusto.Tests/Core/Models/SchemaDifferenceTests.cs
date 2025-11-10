// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using NUnit.Framework;
using SyncKusto.Core.Abstractions;
using SyncKusto.Core.Models;

namespace SyncKusto.Tests.Core.Models;

[TestFixture]
public class DifferenceTests
{
    [Test]
    public void OnlyInTarget_CreatesCorrectInstance()
    {
        // Act
        var difference = Difference.OnlyInTarget();

        // Assert
        Assert.That(difference, Is.InstanceOf<OnlyInTarget>());
    }

    [Test]
    public void OnlyInSource_CreatesCorrectInstance()
    {
        // Act
        var difference = Difference.OnlyInSource();

        // Assert
        Assert.That(difference, Is.InstanceOf<OnlyInSource>());
    }

    [Test]
    public void Modified_CreatesCorrectInstance()
    {
        // Act
        var difference = Difference.Modified();

        // Assert
        Assert.That(difference, Is.InstanceOf<Modified>());
    }

    [Test]
    public void OnlyInTarget_IsInstanceOfDifference()
    {
        // Act
        var difference = new OnlyInTarget();

        // Assert
        Assert.That(difference, Is.InstanceOf<Difference>());
    }

    [Test]
    public void OnlyInSource_IsInstanceOfDifference()
    {
        // Act
        var difference = new OnlyInSource();

        // Assert
        Assert.That(difference, Is.InstanceOf<Difference>());
    }

    [Test]
    public void Modified_IsInstanceOfDifference()
    {
        // Act
        var difference = new Modified();

        // Assert
        Assert.That(difference, Is.InstanceOf<Difference>());
    }

    [Test]
    public void AllDifferenceTypes_AreDistinct()
    {
        // Arrange
        var onlyInTarget = Difference.OnlyInTarget();
        var onlyInSource = Difference.OnlyInSource();
        var modified = Difference.Modified();

        // Assert
        Assert.That(onlyInTarget.GetType(), Is.Not.EqualTo(onlyInSource.GetType()));
        Assert.That(onlyInTarget.GetType(), Is.Not.EqualTo(modified.GetType()));
        Assert.That(onlyInSource.GetType(), Is.Not.EqualTo(modified.GetType()));
    }

    [Test]
    public void CanBeUsedInPatternMatching()
    {
        // Arrange
        var difference = Difference.OnlyInSource();

        // Act
        var result = difference switch
        {
            OnlyInSource => "OnlyInSource",
            OnlyInTarget => "OnlyInTarget",
            Modified => "Modified",
            _ => "Unknown"
        };

        // Assert
        Assert.That(result, Is.EqualTo("OnlyInSource"));
    }
}

[TestFixture]
public class TableSchemaDifferenceTests
{
    [Test]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange
        var schema = new TestKustoSchema("TestTable");
        var difference = new OnlyInSource();

        // Act
        var tableDiff = new TableSchemaDifference(difference, schema);

        // Assert
        Assert.That(tableDiff.Name, Is.EqualTo("TestTable"));
        Assert.That(tableDiff.Schema, Is.SameAs(schema));
        Assert.That(tableDiff.Difference, Is.SameAs(difference));
    }

    [Test]
    public void Name_ReturnsCorrectValue()
    {
        // Arrange
        var schema = new TestKustoSchema("MyTable");
        var tableDiff = new TableSchemaDifference(new Modified(), schema);

        // Act
        var name = tableDiff.Name;

        // Assert
        Assert.That(name, Is.EqualTo("MyTable"));
    }

    [Test]
    public void Schema_ReturnsCorrectValue()
    {
        // Arrange
        var schema = new TestKustoSchema("MyTable");
        var tableDiff = new TableSchemaDifference(new Modified(), schema);

        // Act
        var result = tableDiff.Schema;

        // Assert
        Assert.That(result, Is.SameAs(schema));
    }

    [Test]
    public void IsInstanceOfSchemaDifference()
    {
        // Arrange
        var schema = new TestKustoSchema("MyTable");
        var tableDiff = new TableSchemaDifference(new Modified(), schema);

        // Assert
        Assert.That(tableDiff, Is.InstanceOf<SchemaDifference>());
    }
}

[TestFixture]
public class FunctionSchemaDifferenceTests
{
    [Test]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange
        var schema = new TestKustoSchema("TestFunction");
        var difference = new OnlyInSource();

        // Act
        var functionDiff = new FunctionSchemaDifference(difference, schema);

        // Assert
        Assert.That(functionDiff.Name, Is.EqualTo("TestFunction"));
        Assert.That(functionDiff.Schema, Is.SameAs(schema));
        Assert.That(functionDiff.Difference, Is.SameAs(difference));
    }

    [Test]
    public void Name_ReturnsCorrectValue()
    {
        // Arrange
        var schema = new TestKustoSchema("MyFunction");
        var functionDiff = new FunctionSchemaDifference(new Modified(), schema);

        // Act
        var name = functionDiff.Name;

        // Assert
        Assert.That(name, Is.EqualTo("MyFunction"));
    }

    [Test]
    public void Schema_ReturnsCorrectValue()
    {
        // Arrange
        var schema = new TestKustoSchema("MyFunction");
        var functionDiff = new FunctionSchemaDifference(new Modified(), schema);

        // Act
        var result = functionDiff.Schema;

        // Assert
        Assert.That(result, Is.SameAs(schema));
    }

    [Test]
    public void IsInstanceOfSchemaDifference()
    {
        // Arrange
        var schema = new TestKustoSchema("MyFunction");
        var functionDiff = new FunctionSchemaDifference(new Modified(), schema);

        // Assert
        Assert.That(functionDiff, Is.InstanceOf<SchemaDifference>());
    }
}

[TestFixture]
public class SchemaDifferencePolymorphismTests
{
    [Test]
    public void CanStoreBothTypesInCollection()
    {
        // Arrange
        var tableSchema = new TestKustoSchema("Table1");
        var functionSchema = new TestKustoSchema("Function1");
        var differences = new List<SchemaDifference>
        {
            new TableSchemaDifference(new OnlyInSource(), tableSchema),
            new FunctionSchemaDifference(new OnlyInTarget(), functionSchema)
        };

        // Assert
        Assert.That(differences.Count, Is.EqualTo(2));
        Assert.That(differences[0], Is.InstanceOf<TableSchemaDifference>());
        Assert.That(differences[1], Is.InstanceOf<FunctionSchemaDifference>());
    }

    [Test]
    public void CanFilterByType()
    {
        // Arrange
        var differences = new List<SchemaDifference>
        {
            new TableSchemaDifference(new OnlyInSource(), new TestKustoSchema("Table1")),
            new FunctionSchemaDifference(new OnlyInTarget(), new TestKustoSchema("Function1")),
            new TableSchemaDifference(new Modified(), new TestKustoSchema("Table2"))
        };

        // Act
        var tables = differences.OfType<TableSchemaDifference>().ToList();
        var functions = differences.OfType<FunctionSchemaDifference>().ToList();

        // Assert
        Assert.That(tables.Count, Is.EqualTo(2));
        Assert.That(functions.Count, Is.EqualTo(1));
    }
}
