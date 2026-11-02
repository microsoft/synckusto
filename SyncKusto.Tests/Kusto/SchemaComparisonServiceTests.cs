// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Kusto.Data.Common;
using NUnit.Framework;
using SyncKusto.Core.Abstractions;
using SyncKusto.Core.Models;
using SyncKusto.Kusto.Models;
using SyncKusto.Kusto.Services;

namespace SyncKusto.Tests.Kusto;

[TestFixture]
[Category("Kusto")]
public class SchemaComparisonServiceTests
{
    private readonly ISchemaComparisonService _service = new SchemaComparisonService();

    [TestCase(false)]
    [TestCase(true)]
    public void CompareSchemas_IdenticalSchemas_HaveNoDifferences(bool populated)
    {
        var source = new DatabaseSchema("source", "");
        var target = new DatabaseSchema("target", "");
        if (populated)
        {
            source.Tables.Add("Table", Table("Table"));
            target.Tables.Add("Table", Table("Table"));
            source.Functions.Add("Function", Function("Function"));
            target.Functions.Add("Function", Function("Function"));
        }

        var result = _service.CompareSchemas(source, target);

        Assert.That(result.AllDifferences, Is.Empty);
    }

    [Test]
    public void CompareSchemas_TableChanges_PreserveKindsAndSchemasNeededForSync()
    {
        var source = new DatabaseSchema("source", "");
        var target = new DatabaseSchema("target", "");
        var added = Table("Added");
        var modified = Table("Modified", "System.String");
        var removed = Table("Removed");
        source.Tables.Add(added.Name, added);
        source.Tables.Add(modified.Name, modified);
        source.Tables.Add("Unchanged", Table("Unchanged"));
        target.Tables.Add(removed.Name, removed);
        target.Tables.Add(modified.Name, Table(modified.Name));
        target.Tables.Add("Unchanged", Table("Unchanged"));

        var result = _service.CompareSchemas(source, target);

        Assert.That(result.TableDifferences, Has.Count.EqualTo(3));
        Assert.That(result.FunctionDifferences, Is.Empty);
        foreach (var (schema, kind) in new[]
        {
            (added, typeof(OnlyInSource)), (modified, typeof(Modified)), (removed, typeof(OnlyInTarget))
        })
        {
            var difference = result.TableDifferences.Single(d => d.Name == schema.Name);
            Assert.That(difference, Is.TypeOf<TableSchemaDifference>());
            Assert.That(difference.Difference.GetType(), Is.EqualTo(kind));
            Assert.That(((KustoTableSchema)difference.Schema).Value, Is.SameAs(schema));
        }
    }

    [Test]
    public void CompareSchemas_FunctionChanges_PreserveKindsAndSchemasNeededForSync()
    {
        var source = new DatabaseSchema("source", "");
        var target = new DatabaseSchema("target", "");
        var added = Function("Added");
        var modified = Function("Modified", "{ print 2 }");
        var removed = Function("Removed");
        source.Functions.Add(added.Name, added);
        source.Functions.Add(modified.Name, modified);
        source.Functions.Add("Unchanged", Function("Unchanged"));
        target.Functions.Add(removed.Name, removed);
        target.Functions.Add(modified.Name, Function(modified.Name));
        target.Functions.Add("Unchanged", Function("Unchanged"));

        var result = _service.CompareSchemas(source, target);

        Assert.That(result.FunctionDifferences, Has.Count.EqualTo(3));
        Assert.That(result.TableDifferences, Is.Empty);
        foreach (var (schema, kind) in new[]
        {
            (added, typeof(OnlyInSource)), (modified, typeof(Modified)), (removed, typeof(OnlyInTarget))
        })
        {
            var difference = result.FunctionDifferences.Single(d => d.Name == schema.Name);
            Assert.That(difference, Is.TypeOf<FunctionSchemaDifference>());
            Assert.That(difference.Difference.GetType(), Is.EqualTo(kind));
            Assert.That(((KustoFunctionSchema)difference.Schema).Value, Is.SameAs(schema));
        }
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    public void CompareSchemas_NullArguments_IdentifyInvalidParameter(bool nullSource, bool nullTarget)
    {
        var source = nullSource ? null : new DatabaseSchema("source", "");
        var target = nullTarget ? null : new DatabaseSchema("target", "");

        var error = Assert.Throws<ArgumentNullException>(() => _service.CompareSchemas(source!, target!));

        Assert.That(error!.ParamName, Is.EqualTo(nullSource ? "source" : "target"));
    }

    private static TableSchema Table(string name, string columnType = "System.Int32") =>
        new(name, new[] { new ColumnSchema("Id", columnType) }, "", "");

    private static FunctionSchema Function(string name, string body = "{ print 1 }") =>
        new(name, new List<FunctionParameterSchema>(), body, "", "",
            FunctionSchema.FunctionKind.Unknown, Array.Empty<ColumnSchema>());
}
