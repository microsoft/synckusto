// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Kusto.Data.Common;
using Moq;
using NUnit.Framework;
using SyncKusto.Abstractions;
using SyncKusto.Core.Abstractions;
using SyncKusto.Core.Configuration;
using SyncKusto.Core.Exceptions;
using SyncKusto.Core.Models;
using SyncKusto.Core.Services;
using SyncKusto.ErrorHandling;
using SyncKusto.Kusto.Models;

namespace SyncKusto.Tests.Services;

[TestFixture]
[Category("SchemaImport")]
[Apartment(ApartmentState.STA)]
public class SchemaImportFormTests
{
    [Test]
    public void FailedCompare_ClearsResultsUpdateAndBusyState_ThenCorrectionCanBeCompared()
    {
        var presenter = new Mock<IMainFormPresenter>(MockBehavior.Strict);
        var source = new SchemaSourceInfo(SourceSelection.FilePath(), "source");
        var target = new SchemaSourceInfo(SourceSelection.FilePath(), "target");
        var settings = new SyncKustoSettings { TempCluster = "unused", TempDatabase = "scratch" };
        var result = new ComparisonResult(
            new SchemaDifferenceResult(Array.Empty<SchemaDifference>(), Array.Empty<SchemaDifference>()),
            new DatabaseSchema("source", ""), new DatabaseSchema("target", ""));
        var error = new SchemaInputException("Target", @"Functions\f.kql", "must contain one definition",
            "Remove other commands and compare again.", new InvalidOperationException("private_payload"));
        presenter.SetupSequence(p => p.CompareAsync(source, target, It.IsAny<IProgress<SyncProgress>>(), default))
            .ReturnsAsync(result).ThrowsAsync(error).ReturnsAsync(result);
        using var form = new MainForm(presenter.Object, ErrorMessageResolverFactory.CreateDefault(),
            new InMemorySettingsProvider(), settings, Mock.Of<ISchemaValidationService>(), Mock.Of<IKustoValidationService>());
        var originalCursor = form.Cursor;
        var update = Field<ToolStripButton>(form, "btnUpdate");
        var compare = Field<ToolStripButton>(form, "btnCompare");
        var tree = Field<TreeView>(form, "tvComparison");

        form.CompareSchemasAsync(source, target).GetAwaiter().GetResult();
        Assert.That(update.Enabled, Is.True);
        Assert.That(tree.Nodes.Count, Is.GreaterThan(0));

        Assert.Throws<SchemaInputException>(() => form.CompareSchemasAsync(source, target).GetAwaiter().GetResult());
        Assert.That(update.Enabled, Is.False);
        Assert.That(compare.Enabled, Is.True);
        Assert.That(tree.Nodes.Count, Is.Zero);
        Assert.That(Field<object?>(form, "_lastComparison"), Is.Null);
        Assert.That(Field<object?>(form, "_sourceSchema"), Is.Null);
        Assert.That(Field<object?>(form, "_targetSchema"), Is.Null);
        Assert.That(form.Cursor, Is.EqualTo(originalCursor));
        foreach (string name in new[] { "spcSource", "spcTarget" })
        {
            var picker = Field<SchemaPickerControl>(form, name);
            Assert.That(picker.Enabled, Is.True);
            Assert.That(Field<Label>(picker, "txtOperationProgress").Text, Is.Empty);
        }

        form.CompareSchemasAsync(source, target).GetAwaiter().GetResult();
        Assert.That(update.Enabled, Is.True);
        Assert.That(Field<object?>(form, "_lastComparison"), Is.SameAs(result));
        presenter.Verify(p => p.SynchronizeAsync(It.IsAny<IEnumerable<SchemaDifference>>(),
            It.IsAny<IProgress<SyncProgress>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void KustoDrops_RequireConfirmationBeforeSynchronizing(bool confirmed)
    {
        var presenter = new Mock<IMainFormPresenter>(MockBehavior.Strict);
        var settings = new SyncKustoSettings { TempCluster = "unused", TempDatabase = "scratch", KustoObjectDropWarning = true };
        var drop = new TableSchemaDifference(new OnlyInTarget(),
            new KustoTableSchema(new TableSchema("t", new[] { new ColumnSchema("x", "System.Int64") }, "", "")));
        presenter.Setup(p => p.SynchronizeAsync(It.IsAny<IEnumerable<SchemaDifference>>(), null, default))
            .ReturnsAsync(SyncResult.Successful(1));
        using var form = new MainForm(presenter.Object, ErrorMessageResolverFactory.CreateDefault(),
            new InMemorySettingsProvider(), settings, Mock.Of<ISchemaValidationService>(), Mock.Of<IKustoValidationService>());
        int prompts = 0;

        var result = form.SynchronizeSelectionAsync(new[] { drop },
            new SchemaSourceInfo(SourceSelection.Kusto()), () => { prompts++; return confirmed; }).GetAwaiter().GetResult();

        Assert.That(prompts, Is.EqualTo(1));
        Assert.That(result?.Success, confirmed ? Is.True : Is.Null);
        presenter.Verify(p => p.SynchronizeAsync(
            It.Is<IEnumerable<SchemaDifference>>(selected => selected.Single() == drop), null, default),
            confirmed ? Times.Once : Times.Never);
    }

    [Test]
    public void GeneratedUpdates_DoNotAddANewDropConfirmation()
    {
        var presenter = new Mock<IMainFormPresenter>(MockBehavior.Strict);
        var settings = new SyncKustoSettings { TempCluster = "unused", TempDatabase = "scratch", KustoObjectDropWarning = true };
        var update = new TableSchemaDifference(new Modified(),
            new KustoTableSchema(new TableSchema("t", new[] { new ColumnSchema("x", "System.Int64") }, "", "")));
        presenter.Setup(p => p.SynchronizeAsync(It.IsAny<IEnumerable<SchemaDifference>>(), null, default))
            .ReturnsAsync(SyncResult.Successful(1));
        using var form = new MainForm(presenter.Object, ErrorMessageResolverFactory.CreateDefault(),
            new InMemorySettingsProvider(), settings, Mock.Of<ISchemaValidationService>(), Mock.Of<IKustoValidationService>());

        var result = form.SynchronizeSelectionAsync(new[] { update },
            new SchemaSourceInfo(SourceSelection.Kusto()),
            () => throw new AssertionException("An update should not ask for drop confirmation.")).GetAwaiter().GetResult();

        Assert.That(result!.Success, Is.True);
        presenter.Verify(p => p.SynchronizeAsync(
            It.Is<IEnumerable<SchemaDifference>>(selected => selected.Single() == update), null, default), Times.Once);
    }

    private static T Field<T>(object instance, string name) =>
        (T)instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(instance)!;
}
