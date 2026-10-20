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
using SyncKusto.Tests.ErrorHandling;
using SyncKusto.Core.Exceptions;
using SyncKusto.Services;
using SyncKusto.ErrorHandling;
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
                It.IsAny<SchemaSourceInfo>(),
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

    [Test]
    public void PartialCompare_ShowsAndReopensReport_ThenUpdatesNormally()
    {
        ConfigureTemporaryDatabase();
        _comparison = CreateComparison() with
        {
            SkippedFiles = new[]
            {
                new SkippedSchemaFile("Source", @"Materialized Views\Example.csl",
                    "must contain a single table definition", "Use a supported definition."),
                new SkippedSchemaFile("Target", @"Functions\Broken.csl",
                    "has a syntax error", "Complete the expression.", 3, 4)
            }
        };
        int reports = 0;
        void CloseReport(SkippedFilesForm report)
        {
            reports++;
            var text = report.Controls.Find("txtSkippedFiles", true).Cast<TextBox>().Single().Text;
            foreach (var file in _comparison.SkippedFiles)
            {
                Assert.That(text, Does.Contain($"{file.Side}: {file.RelativePath}"));
                Assert.That(text, Does.Contain(file.Reason).And.Contain(file.Hint));
                if (file.Line.HasValue)
                    Assert.That(text, Does.Contain($"line {file.Line}, column {file.Column}"));
            }
            report.DialogResult = DialogResult.OK;
        }

        WithModal<SkippedFilesForm>(() => Field<ToolStripButton>(_form, "btnCompare").PerformClick(), CloseReport);
        Assert.That(reports, Is.EqualTo(1));
        var skipped = Field<ToolStripButton>(_form, "btnSkippedFiles");
        Assert.That(skipped.Visible, Is.True);
        Assert.That(skipped.Enabled, Is.True);
        Assert.That(skipped.Text, Is.EqualTo("Skipped files (2)"));
        Assert.That(Field<GroupBox>(_form, "grpComparison").Text, Is.EqualTo("Comparison (incomplete)"));
        Assert.That(Field<ToolStripButton>(_form, "btnUpdate").Enabled, Is.True);

        WithModal<SkippedFilesForm>(() => skipped.PerformClick(), CloseReport);
        Assert.That(reports, Is.EqualTo(2));
        Assert.That(Field<ComparisonResult>(_form, "_lastComparison"), Is.SameAs(_comparison));
        DifferenceNode().Checked = true;
        WithMessageBox(() => Field<ToolStripButton>(_form, "btnUpdate").PerformClick(),
            "Complete", DialogResult.OK, "Target update is complete.");
        _presenter.Verify(p => p.SynchronizeAsync(
            It.Is<IEnumerable<SchemaDifference>>(differences =>
                differences.Single().Name == "SettingsProbe"),
            It.IsAny<SchemaSourceInfo>(),
            It.IsAny<IProgress<SyncProgress>>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(skipped.Visible, Is.False);
        Assert.That(skipped.Enabled, Is.False);
        Assert.That(Field<GroupBox>(_form, "grpComparison").Text, Is.EqualTo("Comparison"));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void PartialComparison_SelectionOrSavedSettingsClearsSkippedReport(bool changeSettings)
    {
        ConfigureTemporaryDatabase();
        _comparison = CreateComparison() with
        {
            SkippedFiles = new[]
            {
                new SkippedSchemaFile("Source", "unsupported.csl",
                    "must contain a single table definition", "Use a supported definition.")
            }
        };
        WithModal<SkippedFilesForm>(() => Field<ToolStripButton>(_form, "btnCompare").PerformClick(),
            report => report.DialogResult = DialogResult.OK);

        if (changeSettings)
        {
            WithModal<SettingsForm>(() => Field<ToolStripButton>(_form, "btnSettings").PerformClick(), settings =>
            {
                Field<CheckBox>(settings, "cbCreateMerge").Checked = true;
                Field<Button>(settings, "btnOk").PerformClick();
            });
        }
        else
        {
            Field<TextBox>(Field<SchemaPickerControl>(_form, "spcSource"), "txtFilePath").Text = @"C:\new-source";
        }

        Assert.That(Field<ComparisonResult?>(_form, "_lastComparison"), Is.Null);
        Assert.That(Field<ToolStripButton>(_form, "btnSkippedFiles").Visible, Is.False);
        Assert.That(Field<ToolStripButton>(_form, "btnSkippedFiles").Enabled, Is.False);
        Assert.That(Field<GroupBox>(_form, "grpComparison").Text, Is.EqualTo("Comparison"));
        Assert.That(Field<ToolStripButton>(_form, "btnUpdate").Enabled, Is.False);
    }

    [TestCase("spcSource", "cbCluster")]
    [TestCase("spcTarget", "cbDatabase")]
    [TestCase("spcTarget", "txtFilePath")]
    [TestCase("spcTarget", "rbKusto")]
    public void PartialComparison_EditingSelectionClearsSkippedReport(string pickerName, string inputName)
    {
        _comparison = CreateComparison() with
        {
            SkippedFiles = new[]
            {
                new SkippedSchemaFile("Source", "unsupported.csl", "is unsupported", "Use a supported definition.")
            }
        };
        _form.CompareSchemasAsync(new SchemaSourceInfo(SourceSelection.FilePath(), @"C:\source"),
            new SchemaSourceInfo(SourceSelection.FilePath(), @"C:\target")).GetAwaiter().GetResult();
        var picker = Field<SchemaPickerControl>(_form, pickerName);
        if (inputName == "rbKusto")
            Field<RadioButton>(picker, inputName).Checked = true;
        else
            Field<Control>(picker, inputName).Text = "changed";

        Assert.That(Field<ComparisonResult?>(_form, "_lastComparison"), Is.Null);
        Assert.That(Field<ToolStripButton>(_form, "btnSkippedFiles").Visible, Is.False);
        Assert.That(Field<ToolStripButton>(_form, "btnUpdate").Enabled, Is.False);
    }

    [Test]
    public void Comparing_RefreshesHistoryWithoutChangingSelectionsOrClearingWarnings()
    {
        var picker = Field<SchemaPickerControl>(_form, "spcSource");
        _provider.AddRecentValue("RecentClusters", "https://selected.invalid");
        picker.ReloadRecentValues();
        var cluster = Field<ComboBox>(picker, "cbCluster");
        cluster.SelectedIndex = 0;
        _comparison = CreateComparison() with
        {
            SkippedFiles = new[]
            {
                new SkippedSchemaFile("Source", "unsupported.csl", "is unsupported", "Use a supported definition.")
            }
        };

        _form.CompareSchemasAsync(new SchemaSourceInfo(SourceSelection.FilePath(), @"C:\source"),
            new SchemaSourceInfo(SourceSelection.FilePath(), @"C:\target")).GetAwaiter().GetResult();

        Assert.That(cluster.Text, Is.EqualTo("https://selected.invalid"));
        Assert.That(Field<ComparisonResult>(_form, "_lastComparison"), Is.SameAs(_comparison));
        Assert.That(Field<ToolStripButton>(_form, "btnSkippedFiles").Visible, Is.True);
        Assert.That(Field<ToolStripButton>(_form, "btnUpdate").Enabled, Is.True);
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
    public void DiffRendering_WithNoNode_LeavesExistingTextUnchanged()
    {
        ConfigureTemporaryDatabase();
        Field<ToolStripButton>(_form, "btnCompare").PerformClick();
        Field<RichTextBox>(_form, "rtbSourceText").Text = "Previous comparison";

        typeof(MainForm).GetMethod("tvComparison_NodeMouseClick", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(_form, new object[] { Field<TreeView>(_form, "tvComparison"),
                new TreeNodeMouseClickEventArgs(null, MouseButtons.Left, 1, 0, 0) });

        Assert.That(Field<RichTextBox>(_form, "rtbSourceText").Text, Is.EqualTo("Previous comparison"));
    }

    [Test]
    public void Update_WithMissingDifference_ShowsErrorAndInvalidatesComparison()
    {
        ConfigureTemporaryDatabase();
        Field<ToolStripButton>(_form, "btnCompare").PerformClick();
        var node = DifferenceNode();
        node.Checked = true;
        node.Tag = null;

        WithMessageBox(() => Field<ToolStripButton>(_form, "btnUpdate").PerformClick(),
            "Error", DialogResult.OK, "Selected comparison node does not contain a schema difference.");

        Assert.That(Field<ComparisonResult?>(_form, "_lastComparison"), Is.Null);
        Assert.That(Field<ToolStripButton>(_form, "btnUpdate").Enabled, Is.False);
        _presenter.Verify(p => p.SynchronizeAsync(It.IsAny<IEnumerable<SchemaDifference>>(),
            It.IsAny<SchemaSourceInfo>(),
            It.IsAny<IProgress<SyncProgress>>(), It.IsAny<CancellationToken>()), Times.Never);
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
            It.IsAny<SchemaSourceInfo>(),
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
                It.IsAny<SchemaSourceInfo>(),
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

    private static IEnumerable<TestCaseData> CredentialEdits() =>
        from picker in new[] { "spcSource", "spcTarget" }
        from field in new[] { "cmbAuthentication", "cbAppId", "txtAppKey", "cbAppIdSni", "txtCertificate" }
        select new TestCaseData(picker, field);

    [TestCaseSource(nameof(CredentialEdits))]
    public void AuthenticationEdits_PreserveComparisonSelectionsDiffAndSkippedState(string pickerName, string inputName)
    {
        _comparison = CreateComparison() with
        {
            SkippedFiles = new[] { new SkippedSchemaFile("Source", "skipped.csl", "is unsupported", "Use a supported definition.") }
        };
        _form.CompareSchemasAsync(new SchemaSourceInfo(SourceSelection.FilePath(), @"C:\source"),
            new SchemaSourceInfo(SourceSelection.FilePath(), @"C:\target")).GetAwaiter().GetResult();
        var node = DifferenceNode();
        node.Checked = true;
        var tree = Field<TreeView>(_form, "tvComparison");
        tree.SelectedNode = node;
        var diff = Field<RichTextBox>(_form, "rtbSourceText");
        diff.Text = "Compared schema text";
        var picker = Field<SchemaPickerControl>(_form, pickerName);

        if (inputName == "cmbAuthentication")
            Field<ComboBox>(picker, inputName).SelectedIndex = 2;
        else
            Field<Control>(picker, inputName).Text = "new value";

        Assert.That(Field<ComparisonResult>(_form, "_lastComparison"), Is.SameAs(_comparison));
        Assert.That(Field<DatabaseSchema>(_form, "_sourceSchema"), Is.SameAs(_comparison.SourceSchema));
        Assert.That(Field<DatabaseSchema>(_form, "_targetSchema"), Is.SameAs(_comparison.TargetSchema));
        Assert.That(DifferenceNode(), Is.SameAs(node));
        Assert.That(node.Checked, Is.True);
        Assert.That(tree.SelectedNode, Is.SameAs(node));
        Assert.That(diff.Text, Is.EqualTo("Compared schema text"));
        Assert.That(Field<ToolStripButton>(_form, "btnUpdate").Enabled, Is.True);
        Assert.That(Field<ToolStripButton>(_form, "btnSkippedFiles").Enabled, Is.True);
        Assert.That(Field<ToolStripButton>(_form, "btnSkippedFiles").Text, Is.EqualTo("Skipped files (1)"));
        Assert.That(Field<GroupBox>(_form, "grpComparison").Text, Is.EqualTo("Comparison (incomplete)"));
        _presenter.Verify(p => p.CompareAsync(It.IsAny<SchemaSourceInfo>(), It.IsAny<SchemaSourceInfo>(),
            It.IsAny<IProgress<SyncProgress>>(), default), Times.Once);
    }

    [Test]
    public void Update_IncompleteEditedCredentials_LeavesComparisonAndDoesNotCallPresenter()
    {
        ConfigureTemporaryDatabase();
        var target = Field<SchemaPickerControl>(_form, "spcTarget");
        Field<RadioButton>(target, "rbKusto").Checked = true;
        Field<ComboBox>(target, "cbCluster").Text = Cluster;
        Field<ComboBox>(target, "cbDatabase").Text = Database;
        Field<ToolStripButton>(_form, "btnCompare").PerformClick();
        DifferenceNode().Checked = true;
        Field<ComboBox>(target, "cmbAuthentication").SelectedIndex = 1;
        Field<ComboBox>(target, "cbAppId").Text = "offline-app";

        WithMessageBox(() => Field<ToolStripButton>(_form, "btnUpdate").PerformClick(),
            "Error", DialogResult.OK, "Target schema is not correctly specified.");

        Assert.That(Field<ComparisonResult>(_form, "_lastComparison"), Is.SameAs(_comparison));
        Assert.That(DifferenceNode().Checked, Is.True);
        Assert.That(Field<ToolStripButton>(_form, "btnUpdate").Enabled, Is.True);
        _presenter.Verify(p => p.SynchronizeAsync(It.IsAny<IEnumerable<SchemaDifference>>(), It.IsAny<SchemaSourceInfo>(),
            It.IsAny<IProgress<SyncProgress>>(), default), Times.Never);
        AssertIdle();
    }

    [TestCase("compare")]
    [TestCase("update")]
    [TestCase("result")]
    [TestCase("mixed-result")]
    public void KustoFailure_ActualOperationShowsFullOwnedDiagnosticsAndRecoversControls(string operation)
    {
        ConfigureTemporaryDatabase();
        var failure = KustoErrorDetailsTests.Forbidden();
        if (operation == "compare")
        {
            _presenter.Setup(p => p.CompareAsync(It.IsAny<SchemaSourceInfo>(), It.IsAny<SchemaSourceInfo>(),
                It.IsAny<IProgress<SyncProgress>>(), default)).ThrowsAsync(new SchemaLoadException("Compare failed", failure));
        }
        else
        {
            Field<ToolStripButton>(_form, "btnCompare").PerformClick();
            DifferenceNode().Checked = true;
            var setup = _presenter.Setup(p => p.SynchronizeAsync(It.IsAny<IEnumerable<SchemaDifference>>(),
                It.IsAny<SchemaSourceInfo>(), It.IsAny<IProgress<SyncProgress>>(), default));
            if (operation == "update")
                setup.ThrowsAsync(failure);
            else if (operation == "mixed-result")
                setup.ReturnsAsync(SyncResult.Failed(new[] { "Wrapper rejected_file_payload" },
                    new[] { failure, new SchemaLoadException("Wrapper rejected_file_payload",
                        new SchemaInputException("Target", "bad.csl", "has a syntax error", "Complete the expression.",
                            new InvalidOperationException("rejected_file_payload"))) }));
            else
                setup.ReturnsAsync(SyncResult.Failed(new[] { "Save failed", "Delete failed" },
                    new[] { failure, KustoErrorDetailsTests.Forbidden("SecondTable") }));
        }

        WithModal<ErrorDetailsForm>(() => Field<ToolStripButton>(_form,
            operation == "compare" ? "btnCompare" : "btnUpdate").PerformClick(), dialog =>
        {
            Assert.That(dialog.Owner, Is.SameAs(_form));
            Assert.That(dialog.Controls.Find("lblHeading", true).Single().Text,
                Is.EqualTo(operation == "compare" ? "Couldn't compare schemas." : "Couldn't update the target schema."));
            var details = dialog.Controls.Find("txtErrorDetails", true).Single().Text;
            Assert.That(details, Does.Contain(failure.ToString()).And.Contain("Failed object: FailedTable"));
            Assert.That(details, Does.Contain("403-Forbidden").And.Contain("activity-id=offline-probe"));
            if (operation == "result")
                Assert.That(details, Does.Contain("Failed object: SecondTable"));
            if (operation == "mixed-result")
                Assert.That(details, Does.Contain("Target file 'bad.csl' has a syntax error.")
                    .And.Not.Contain("rejected_file_payload"));
            dialog.DialogResult = DialogResult.OK;
        });

        AssertIdle();
        Assert.That(Field<ComparisonResult?>(_form, "_lastComparison"), Is.Null);
        Assert.That(Field<ToolStripButton>(_form, "btnUpdate").Enabled, Is.False);
    }

    [Test]
    public void Compare_SchemaInputFailure_ShowsConciseSafeMessage()
    {
        ConfigureTemporaryDatabase();
        var input = new SchemaInputException("Target", @"Functions\bad.csl", "has a syntax error",
            "Complete the expression.", new InvalidOperationException("rejected_file_payload"));
        _presenter.Setup(p => p.CompareAsync(It.IsAny<SchemaSourceInfo>(), It.IsAny<SchemaSourceInfo>(),
            It.IsAny<IProgress<SyncProgress>>(), default)).ThrowsAsync(new SchemaLoadException("outer", input));
        using var form = new MainForm(_presenter.Object, ErrorMessageResolverFactory.CreateDefault(),
            _provider, Mock.Of<ISchemaValidationService>(), Mock.Of<IKustoValidationService>());
        form.Show();
        foreach (var name in new[] { "spcSource", "spcTarget" })
            Field<TextBox>(Field<SchemaPickerControl>(form, name), "txtFilePath").Text = @"C:\schema";

        WithMessageBox(() => Field<ToolStripButton>(form, "btnCompare").PerformClick(),
            "Error", DialogResult.OK, "Failed to compare schemas: " + input.Message);
        Assert.That(Application.OpenForms.OfType<ErrorDetailsForm>(), Is.Empty);
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Compare_EmptyFolderDoesNotOverwriteHistoryOrCallPresenter(string empty)
    {
        ConfigureTemporaryDatabase();
        _provider.SetSetting("PreviousFilePath", @"C:\previous");
        Field<TextBox>(Field<SchemaPickerControl>(_form, "spcSource"), "txtFilePath").Text = empty;

        WithMessageBox(() => Field<ToolStripButton>(_form, "btnCompare").PerformClick(),
            "Error", DialogResult.OK, "Source schema is not correctly specified.");

        Assert.That(_provider.GetSetting("PreviousFilePath"), Is.EqualTo(@"C:\previous"));
        _presenter.Verify(p => p.CompareAsync(It.IsAny<SchemaSourceInfo>(), It.IsAny<SchemaSourceInfo>(),
            It.IsAny<IProgress<SyncProgress>>(), default), Times.Never);
    }

    [TestCase(true, false, false)]
    [TestCase(false, true, false)]
    [TestCase(true, true, false)]
    [TestCase(true, false, true)]
    [TestCase(true, true, true)]
    [TestCase(false, false, true)]
    public void Compare_RemembersUsedFoldersBeforeFailure_AndRestoresSharedValueOnNextLaunch(
        bool sourceFile, bool targetFile, bool fail)
    {
        var directory = Path.Combine(Path.GetTempPath(), $"SyncKustoHistory_{Guid.NewGuid()}");
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "settings.json");
            var provider = new JsonFileSettingsProvider(path);
            provider.SetSetting("PreviousFilePath", @"C:\previous");
            if (fail)
                _presenter.Setup(p => p.CompareAsync(It.IsAny<SchemaSourceInfo>(), It.IsAny<SchemaSourceInfo>(),
                    It.IsAny<IProgress<SyncProgress>>(), default)).ThrowsAsync(new InvalidOperationException("offline failure"));
            using var form = new MainForm(_presenter.Object, Mock.Of<IErrorMessageResolver>(), provider,
                Mock.Of<ISchemaValidationService>(), Mock.Of<IKustoValidationService>());
            var source = new SchemaSourceInfo(sourceFile ? SourceSelection.FilePath() : SourceSelection.Kusto(), @"C:\source");
            var target = new SchemaSourceInfo(targetFile ? SourceSelection.FilePath() : SourceSelection.Kusto(), @"C:\target");
            _presenter.Setup(p => p.CompareAsync(source, target, It.IsAny<IProgress<SyncProgress>>(), default))
                .Callback(() => Assert.That(new JsonFileSettingsProvider(path).GetSetting("PreviousFilePath"),
                    Is.EqualTo(targetFile ? @"C:\target" : sourceFile ? @"C:\source" : @"C:\previous")))
                .Returns(() => fail ? Task.FromException<ComparisonResult>(new InvalidOperationException("offline failure")) :
                    Task.FromResult(_comparison));

            if (fail)
                Assert.Throws<InvalidOperationException>(() => form.CompareSchemasAsync(source, target).GetAwaiter().GetResult());
            else
                form.CompareSchemasAsync(source, target).GetAwaiter().GetResult();

            foreach (var name in new[] { "spcSource", "spcTarget" })
                Assert.That(Field<TextBox>(Field<SchemaPickerControl>(form, name), "txtFilePath").Text, Is.EqualTo(@"C:\previous"),
                    "Saving history must not change either current picker.");
            using var restarted = new MainForm(_presenter.Object, Mock.Of<IErrorMessageResolver>(),
                new JsonFileSettingsProvider(path), Mock.Of<ISchemaValidationService>(), Mock.Of<IKustoValidationService>());
            foreach (var name in new[] { "spcSource", "spcTarget" })
                Assert.That(Field<TextBox>(Field<SchemaPickerControl>(restarted, name), "txtFilePath").Text,
                    Is.EqualTo(targetFile ? @"C:\target" : sourceFile ? @"C:\source" : @"C:\previous"));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
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
