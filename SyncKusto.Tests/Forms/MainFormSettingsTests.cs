// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Kusto.Data.Common;
using Moq;
using NUnit.Framework;
using SyncKusto.Abstractions;
using SyncKusto.Core.Abstractions;
using SyncKusto.Core.Models;
using SyncKusto.Core.Services;
using SyncKusto.Kusto.Models;
using System.Reflection;
using static SyncKusto.Tests.Forms.FormsTestHelper;

namespace SyncKusto.Tests.Forms;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public class MainFormSettingsTests
{
    private const string Cluster = "https://temp.invalid";
    private const string Database = "TempDatabase";
    private InMemorySettingsProvider _provider = null!;
    private Mock<IMainFormPresenter> _presenter = null!;
    private MainForm _form = null!;
    private ComparisonResult _comparison = null!;

    [SetUp]
    public void SetUp()
    {
        _provider = new InMemorySettingsProvider();
        var validation = new SchemaValidationService(_provider);
        var kustoValidation = new Mock<IKustoValidationService>();
        kustoValidation.Setup(v => v.ValidateKustoSettingsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Cluster);
        kustoValidation.Setup(v => v.CheckDatabaseEmptyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((0L, 0L));
        _comparison = CreateComparison();
        _presenter = new Mock<IMainFormPresenter>();
        _presenter.Setup(p => p.ValidateSettings(It.IsAny<SchemaSourceInfo>(), It.IsAny<SchemaSourceInfo>()))
            .Returns<SchemaSourceInfo, SchemaSourceInfo>(validation.ValidateSettings);
        _presenter.Setup(p => p.CompareAsync(It.IsAny<SchemaSourceInfo>(), It.IsAny<SchemaSourceInfo>(),
                It.IsAny<IProgress<SyncProgress>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _comparison);
        _presenter.Setup(p => p.SynchronizeAsync(It.IsAny<IEnumerable<SchemaDifference>>(),
                It.IsAny<IProgress<SyncProgress>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SyncResult.Successful(1));
        var errorResolver = new Mock<IErrorMessageResolver>();
        errorResolver.Setup(r => r.ResolveErrorMessage(It.IsAny<Exception>()))
            .Returns<Exception>(ex => ex.ToString());
        _form = new MainForm(_presenter.Object, errorResolver.Object,
            _provider, validation, kustoValidation.Object);
        _form.Show();
        Field<TextBox>(Field<SchemaPickerControl>(_form, "spcSource"), "txtFilePath").Text = @"C:\source";
        Field<TextBox>(Field<SchemaPickerControl>(_form, "spcTarget"), "txtFilePath").Text = @"C:\target";
    }

    [TearDown]
    public void TearDown()
    {
        _form.Dispose();
    }

    [Test]
    public void FirstSettingsSave_AllowsCompareWithoutRecreatingFormOrValidator()
    {
        WithModal<SettingsForm>(() => Field<ToolStripButton>(_form, "btnSettings").PerformClick(), settings =>
        {
            Field<TextBox>(settings, "txtKustoCluster").Text = Cluster;
            Field<TextBox>(settings, "txtKustoDatabase").Text = Database;
            Field<Button>(settings, "btnOk").PerformClick();
        });

        Field<ToolStripButton>(_form, "btnCompare").PerformClick();

        Assert.That(Field<ComparisonResult>(_form, "_lastComparison"), Is.SameAs(_comparison));
        Assert.That(Field<ToolStripButton>(_form, "btnUpdate").Enabled, Is.True);
        _presenter.Verify(p => p.CompareAsync(It.IsAny<SchemaSourceInfo>(), It.IsAny<SchemaSourceInfo>(),
            It.IsAny<IProgress<SyncProgress>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestCase(true, true)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    public void SettingsDialog_InvalidatesComparisonOnlyWhenChangesAreSaved(bool changeSettings, bool save)
    {
        ConfigureTemporaryDatabase();
        Field<ToolStripButton>(_form, "btnCompare").PerformClick();

        WithModal<SettingsForm>(() => Field<ToolStripButton>(_form, "btnSettings").PerformClick(), settings =>
        {
            Field<CheckBox>(settings, "cbCreateMerge").Checked = changeSettings;
            Field<Button>(settings, save ? "btnOk" : "btnCancel").PerformClick();
        });

        var invalidated = changeSettings && save;
        Assert.That(Field<ToolStripButton>(_form, "btnUpdate").Enabled, Is.EqualTo(!invalidated));
        Assert.That(Field<ComparisonResult?>(_form, "_lastComparison"),
            invalidated ? Is.Null : Is.SameAs(_comparison));
        Assert.That(Field<TreeView>(_form, "tvComparison").Nodes.Count == 0, Is.EqualTo(invalidated));
        if (invalidated)
        {
            Assert.That(Field<DatabaseSchema?>(_form, "_sourceSchema"), Is.Null);
            Assert.That(Field<DatabaseSchema?>(_form, "_targetSchema"), Is.Null);
            Assert.That(Field<RichTextBox>(_form, "rtbSourceText").Text, Is.Empty);
        }
    }

    [Test]
    public void DiffRendering_ReadsCurrentFormattingSettings()
    {
        ConfigureTemporaryDatabase();
        Field<ToolStripButton>(_form, "btnCompare").PerformClick();
        _provider.SetSetting("CreateMergeEnabled", "true");
        _provider.SetSetting("TableFieldsOnNewLine", "true");
        _provider.SetSetting("LineEndingMode", ((int)LineEndingMode.UnixStyle).ToString());
        var node = DifferenceNode();

        typeof(MainForm).GetMethod("tvComparison_NodeMouseClick", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(_form, new object[] { Field<TreeView>(_form, "tvComparison"),
                new TreeNodeMouseClickEventArgs(node, MouseButtons.Left, 1, 0, 0) });

        var text = Field<RichTextBox>(_form, "rtbSourceText").Text;
        Assert.That(text, Does.Contain(".create-merge table"));
        Assert.That(text, Does.Contain("\n    ['"));
    }

    [Test]
    public void KustoPicker_ReadsCurrentAuthorityAndCertificateLocation()
    {
        var picker = Field<SchemaPickerControl>(_form, "spcSource");
        _provider.SetSetting("AADAuthority", "updated-authority");
        _provider.SetSetting("CertificateLocation", StoreLocation.LocalMachine.ToString());
        Field<RadioButton>(picker, "rbKusto").Checked = true;

        var info = picker.GetSourceInfo().KustoInfo;

        Assert.That(info, Is.Not.Null);
        Assert.That(info!.Authority, Is.EqualTo("updated-authority"));
        Assert.That(info.CertificateLocation, Is.EqualTo(StoreLocation.LocalMachine));
    }

    [Test]
    public void DropWarningPreference_AffectsNextUpdateWithoutRestart()
    {
        ConfigureTemporaryDatabase();
        var target = Field<SchemaPickerControl>(_form, "spcTarget");
        Field<RadioButton>(target, "rbKusto").Checked = true;
        Field<ComboBox>(target, "cbCluster").Text = Cluster;
        Field<ComboBox>(target, "cbDatabase").Text = Database;
        _comparison = CreateComparison(onlyInTarget: true);
        Field<ToolStripButton>(_form, "btnCompare").PerformClick();
        DifferenceNode().Checked = true;

        using var warning = new DropWarningForm(_provider);
        WithModal<DropWarningForm>(() => warning.ShowDialog(), dialog =>
        {
            Field<CheckBox>(dialog, "chkNextTime").Checked = false;
            Field<Button>(dialog, "btnOk").PerformClick();
        });
        WithMessageBox(() => Field<ToolStripButton>(_form, "btnUpdate").PerformClick(),
            "Complete", DialogResult.OK, "Target update is complete.");

        _presenter.Verify(p => p.SynchronizeAsync(It.IsAny<IEnumerable<SchemaDifference>>(),
            It.IsAny<IProgress<SyncProgress>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Compare_DisablesConflictingActionsAndRestoresThemAfterCompletion(bool fail)
    {
        ConfigureTemporaryDatabase();
        var pending = new TaskCompletionSource<ComparisonResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _presenter.Setup(p => p.CompareAsync(It.IsAny<SchemaSourceInfo>(), It.IsAny<SchemaSourceInfo>(),
                It.IsAny<IProgress<SyncProgress>>(), It.IsAny<CancellationToken>()))
            .Returns(pending.Task);

        Field<ToolStripButton>(_form, "btnCompare").PerformClick();

        AssertBusy();
        if (fail)
        {
            WithMessageBox(() =>
            {
                pending.SetException(new InvalidOperationException("Comparison test failure."));
                PumpUntil(() => Field<ToolStripButton>(_form, "btnCompare").Enabled);
            }, "Error", DialogResult.OK, "Failed to compare schemas:");
        }
        else
        {
            pending.SetResult(_comparison);
            PumpUntil(() => Field<ToolStripButton>(_form, "btnCompare").Enabled);
        }

        AssertIdle();
        Assert.That(Field<ToolStripButton>(_form, "btnUpdate").Enabled, Is.EqualTo(!fail));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Update_DisablesConflictingActionsAndRestoresThemAfterCompletion(bool fail)
    {
        ConfigureTemporaryDatabase();
        Field<ToolStripButton>(_form, "btnCompare").PerformClick();
        DifferenceNode().Checked = true;
        var pending = new TaskCompletionSource<SyncResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _presenter.Setup(p => p.SynchronizeAsync(It.IsAny<IEnumerable<SchemaDifference>>(),
                It.IsAny<IProgress<SyncProgress>>(), It.IsAny<CancellationToken>()))
            .Returns(pending.Task);

        Field<ToolStripButton>(_form, "btnUpdate").PerformClick();

        AssertBusy();
        WithMessageBox(() =>
        {
            if (fail)
                pending.SetException(new InvalidOperationException("Update test failure."));
            else
                pending.SetResult(SyncResult.Successful(1));
            PumpUntil(() => Field<ToolStripButton>(_form, "btnCompare").Enabled);
        }, fail ? "Error" : "Complete", DialogResult.OK,
            fail ? "Failed to synchronize schemas:" : "Target update is complete.");

        AssertIdle();
        Assert.That(Field<ToolStripButton>(_form, "btnUpdate").Enabled, Is.False);
        Assert.That(Field<ComparisonResult?>(_form, "_lastComparison"), Is.Null);
    }

    private void ConfigureTemporaryDatabase()
    {
        _provider.SetSetting("TempCluster", Cluster);
        _provider.SetSetting("TempDatabase", Database);
    }

    private TreeNode DifferenceNode() =>
        Field<TreeView>(_form, "tvComparison").Nodes.Cast<TreeNode>()
            .SelectMany(root => root.Nodes.Cast<TreeNode>())
            .SelectMany(group => group.Nodes.Cast<TreeNode>()).Single();

    private void AssertBusy()
    {
        Assert.That(Field<ToolStripButton>(_form, "btnCompare").Enabled, Is.False);
        Assert.That(Field<ToolStripButton>(_form, "btnSettings").Enabled, Is.False);
        Assert.That(Field<ToolStripButton>(_form, "btnUpdate").Enabled, Is.False);
        Assert.That(Field<SchemaPickerControl>(_form, "spcSource").Enabled, Is.False);
        Assert.That(Field<SchemaPickerControl>(_form, "spcTarget").Enabled, Is.False);
    }

    private void AssertIdle()
    {
        Assert.That(Field<ToolStripButton>(_form, "btnCompare").Enabled, Is.True);
        Assert.That(Field<ToolStripButton>(_form, "btnSettings").Enabled, Is.True);
        Assert.That(Field<SchemaPickerControl>(_form, "spcSource").Enabled, Is.True);
        Assert.That(Field<SchemaPickerControl>(_form, "spcTarget").Enabled, Is.True);
    }

    private static ComparisonResult CreateComparison(bool onlyInTarget = false)
    {
        var table = new TableSchema("SettingsProbe", new[]
        {
            new ColumnSchema("Id", "System.Int32"),
            new ColumnSchema("Label", "System.String")
        }, "", "");
        var source = new DatabaseSchema("Source", "Source");
        var target = new DatabaseSchema("Target", "Target");
        (onlyInTarget ? target : source).Tables.Add(table.Name, table);
        SchemaDifference difference = new TableSchemaDifference(
            onlyInTarget ? new OnlyInTarget() : new OnlyInSource(), new KustoTableSchema(table));
        return new ComparisonResult(
            new SchemaDifferenceResult(new[] { difference }, Array.Empty<SchemaDifference>()), source, target);
    }
}
