// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Kusto.Data;
using Kusto.Data.Common;
using Moq;
using NUnit.Framework;
using SyncKusto.Core.Abstractions;
using SyncKusto.Core.Models;
using SyncKusto.Core.Services;
using SyncKusto.ErrorHandling;
using SyncKusto.Kusto.Models;
using SyncKusto.Services;
using static SyncKusto.Tests.Forms.FormsTestHelper;

namespace SyncKusto.Tests.Forms;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public class AuthenticationSwitchFlowTests
{
    [TestCase(0, 1)]
    [TestCase(0, 2)]
    [TestCase(1, 2)]
    [TestCase(2, 1)]
    [TestCase(1, 1)]
    [TestCase(2, 2)]
    public void Compare_EditCredentials_UpdatePassesCurrentOptionsToFreshClientWithoutRecompare(int comparedMode, int updatedMode)
    {
        var settings = new InMemorySettingsProvider();
        settings.SetSetting("TempCluster", "https://temp.invalid");
        settings.SetSetting("TempDatabase", "scratch");
        settings.SetSetting("AADAuthority", "offline-tenant");
        settings.SetSetting("CertificateLocation", StoreLocation.LocalMachine.ToString());
        var options = new List<KustoConnectionOptions>();
        var connections = new Mock<IKustoConnectionFactory>(MockBehavior.Strict);
        connections.Setup(c => c.CreateConnectionString(It.IsAny<KustoConnectionOptions>()))
            .Callback<KustoConnectionOptions>(options.Add)
            .Returns(new KustoConnectionStringBuilder("https://target.invalid") { InitialCatalog = "db" });
        var factory = new TrackingFactory(settings, connections.Object);
        var sync = new Mock<ISchemaSyncService>(MockBehavior.Strict);
        var table = new TableSchema("SelectedTable", new[] { new ColumnSchema("Id", "System.Int32") }, "", "");
        var difference = new TableSchemaDifference(new OnlyInSource(), new KustoTableSchema(table));
        var comparison = new ComparisonResult(new SchemaDifferenceResult(new[] { difference }, Array.Empty<SchemaDifference>()),
            new DatabaseSchema("source", ""), new DatabaseSchema("target", ""))
        {
            SkippedFiles = new[] { new SkippedSchemaFile("Source", "skipped.csl", "is unsupported", "Use a supported definition.") }
        };
        sync.Setup(s => s.CompareAsync(It.IsAny<ISchemaRepository>(), It.IsAny<ISchemaRepository>(),
            It.IsAny<IProgress<SyncProgress>>(), default)).ReturnsAsync(comparison);
        sync.Setup(s => s.SynchronizeAsync(It.IsAny<ISchemaRepository>(), It.IsAny<ISchemaRepository>(),
                It.Is<IEnumerable<SchemaDifference>>(selected => selected.Single() == difference),
                It.IsAny<IProgress<SyncProgress>>(), default))
            .Callback<ISchemaRepository, ISchemaRepository, IEnumerable<SchemaDifference>, IProgress<SyncProgress>, CancellationToken>(
                (source, target, _, _, _) =>
                {
                    Assert.That(source, Is.SameAs(factory.Repositories[0]));
                    Assert.That(target, Is.SameAs(factory.Repositories[2]));
                    Assert.That(target, Is.Not.SameAs(factory.Repositories[1]));
                    Assert.That(Field<bool>(target, "_disposed"), Is.False);
                })
            .ReturnsAsync(SyncResult.Successful(1));
        var validation = new SchemaValidationService(settings);
        using var presenter = new MainFormPresenter(sync.Object, validation, factory);
        using var form = new MainForm(presenter, ErrorMessageResolverFactory.CreateDefault(), settings,
            validation, Mock.Of<IKustoValidationService>());
        form.Show();
        var sourcePicker = Field<SchemaPickerControl>(form, "spcSource");
        Field<TextBox>(sourcePicker, "txtFilePath").Text = @"C:\offline-source";
        var targetPicker = Field<SchemaPickerControl>(form, "spcTarget");
        Field<RadioButton>(targetPicker, "rbKusto").Checked = true;
        Field<ComboBox>(targetPicker, "cbCluster").Text = "https://target.invalid";
        Field<ComboBox>(targetPicker, "cbDatabase").Text = "db";
        SetCredentials(targetPicker, comparedMode, "compared");

        WithModal<SkippedFilesForm>(() => Field<ToolStripButton>(form, "btnCompare").PerformClick(),
            report => report.DialogResult = DialogResult.OK);
        var tree = Field<TreeView>(form, "tvComparison");
        var node = tree.Nodes.Cast<TreeNode>().SelectMany(root => root.Nodes.Cast<TreeNode>())
            .SelectMany(group => group.Nodes.Cast<TreeNode>()).Single();
        node.Checked = true;
        SetCredentials(sourcePicker, 2, "source-edited");
        SetCredentials(targetPicker, updatedMode, "current");

        Assert.That(Field<ComparisonResult>(form, "_lastComparison"), Is.SameAs(comparison));
        Assert.That(node.Checked, Is.True);
        Assert.That(Field<GroupBox>(form, "grpComparison").Text, Is.EqualTo("Comparison (incomplete)"));
        Assert.That(Field<ToolStripButton>(form, "btnSkippedFiles").Enabled, Is.True);
        Assert.That(options.Count, Is.EqualTo(1), "Editing credentials must not create a client.");
        Field<RichTextBox>(form, "rtbSourceText").Text = "Compared schema text";
        settings.SetSetting("AADAuthority", "");
        WithMessageBox(() => Field<ToolStripButton>(form, "btnUpdate").PerformClick(),
            "Error", DialogResult.OK, "Application authentication requires an Entra ID authority in Settings.");
        Assert.That(Field<ComparisonResult>(form, "_lastComparison"), Is.SameAs(comparison));
        Assert.That(Field<RichTextBox>(form, "rtbSourceText").Text, Is.EqualTo("Compared schema text"));
        Assert.That(node.Checked, Is.True);
        Assert.That(options.Count, Is.EqualTo(1));
        Assert.That(Field<ToolStripButton>(form, "btnUpdate").Enabled, Is.True);
        Assert.That(Field<ToolStripButton>(form, "btnCompare").Enabled, Is.True);
        settings.SetSetting("AADAuthority", "offline-tenant");
        WithMessageBox(() => Field<ToolStripButton>(form, "btnUpdate").PerformClick(),
            "Complete", DialogResult.OK, "Target update is complete.");

        Assert.That(options.Count, Is.EqualTo(2));
        var current = options[1];
        Assert.That(current.AuthMode, Is.EqualTo(updatedMode == 1 ? AuthenticationMode.AadApplication : AuthenticationMode.AadApplicationSni));
        Assert.That(current.AppId, Is.EqualTo(updatedMode == 1 ? "current-app" : "current-sni-app"));
        Assert.That(current.AppKey, updatedMode == 1 ? Is.EqualTo("current-offline-key") : Is.Null);
        Assert.That(current.CertificateThumbprint, updatedMode == 2 ? Is.EqualTo("current-offline-thumbprint") : Is.Null);
        Assert.That(current.Authority, Is.EqualTo("offline-tenant"));
        Assert.That(current.CertificateLocation, Is.EqualTo(StoreLocation.LocalMachine));
        Assert.That(current.Cluster, Is.EqualTo("https://target.invalid"));
        Assert.That(current.Database, Is.EqualTo("db"));
        Assert.That(factory.Repositories.Count, Is.EqualTo(3));
        Assert.That(Field<bool>(factory.Repositories[2], "_disposed"), Is.True);
        sync.Verify(s => s.CompareAsync(It.IsAny<ISchemaRepository>(), It.IsAny<ISchemaRepository>(),
            It.IsAny<IProgress<SyncProgress>>(), default), Times.Once);
        sync.Verify(s => s.SynchronizeAsync(It.IsAny<ISchemaRepository>(), It.IsAny<ISchemaRepository>(),
            It.IsAny<IEnumerable<SchemaDifference>>(), It.IsAny<IProgress<SyncProgress>>(), default), Times.Once);
        Assert.That(Field<ComparisonResult?>(form, "_lastComparison"), Is.Null);
        Assert.That(Field<bool>(factory.Repositories[1], "_disposed"), Is.True);
    }

    [Test]
    public void SourceCredentialEdits_AreNotUsedByFileTargetUpdate_ButReachNextCompare()
    {
        var settings = new InMemorySettingsProvider();
        settings.SetSetting("TempCluster", "https://temp.invalid");
        settings.SetSetting("TempDatabase", "scratch");
        settings.SetSetting("AADAuthority", "tenant");
        var sourceRepository = new Mock<ISchemaRepository>(MockBehavior.Strict);
        var targetRepository = new Mock<ISchemaRepository>(MockBehavior.Strict);
        var factory = new Mock<SchemaRepositoryFactory>(settings, Mock.Of<IKustoConnectionFactory>());
        var sources = new List<SchemaSourceInfo>();
        factory.Setup(f => f.CreateRepository(It.Is<SchemaSourceInfo>(info => info.SourceType == SourceSelection.Kusto())))
            .Callback<SchemaSourceInfo>(sources.Add).Returns(sourceRepository.Object);
        factory.Setup(f => f.CreateRepository(It.Is<SchemaSourceInfo>(info => info.SourceType == SourceSelection.FilePath())))
            .Returns(targetRepository.Object);
        var difference = new TableSchemaDifference(new OnlyInSource(), new Core.TestKustoSchema("SelectedTable"));
        var comparison = new ComparisonResult(new SchemaDifferenceResult(new[] { difference }, Array.Empty<SchemaDifference>()),
            new DatabaseSchema("source", ""), new DatabaseSchema("target", ""));
        var sync = new Mock<ISchemaSyncService>(MockBehavior.Strict);
        sync.Setup(s => s.CompareAsync(sourceRepository.Object, targetRepository.Object,
            It.IsAny<IProgress<SyncProgress>>(), default)).ReturnsAsync(comparison);
        sync.Setup(s => s.SynchronizeAsync(sourceRepository.Object, targetRepository.Object,
            It.Is<IEnumerable<SchemaDifference>>(selected => selected.Single() == difference),
            It.IsAny<IProgress<SyncProgress>>(), default)).ReturnsAsync(SyncResult.Successful(1));
        var validation = new SchemaValidationService(settings);
        using var presenter = new MainFormPresenter(sync.Object, validation, factory.Object);
        using var form = new MainForm(presenter,
            ErrorMessageResolverFactory.CreateDefault(), settings, validation, Mock.Of<IKustoValidationService>());
        form.Show();
        var sourcePicker = Field<SchemaPickerControl>(form, "spcSource");
        Field<RadioButton>(sourcePicker, "rbKusto").Checked = true;
        Field<ComboBox>(sourcePicker, "cbCluster").Text = "https://source.invalid";
        Field<ComboBox>(sourcePicker, "cbDatabase").Text = "db";
        Field<TextBox>(Field<SchemaPickerControl>(form, "spcTarget"), "txtFilePath").Text = @"C:\offline-target";
        Field<ToolStripButton>(form, "btnCompare").PerformClick();
        var node = Field<TreeView>(form, "tvComparison").Nodes.Cast<TreeNode>()
            .SelectMany(root => root.Nodes.Cast<TreeNode>()).SelectMany(group => group.Nodes.Cast<TreeNode>()).Single();
        node.Checked = true;
        SetCredentials(sourcePicker, 2, "current");

        WithMessageBox(() => Field<ToolStripButton>(form, "btnUpdate").PerformClick(),
            "Complete", DialogResult.OK, "Target update is complete.");

        Assert.That(sources.Count, Is.EqualTo(1));
        factory.Verify(f => f.CreateRepository(It.IsAny<SchemaSourceInfo>()), Times.Exactly(2));
        Field<ToolStripButton>(form, "btnCompare").PerformClick();
        Assert.That(sources.Count, Is.EqualTo(2));
        Assert.That(sources[1].KustoInfo!.AuthMode, Is.EqualTo(AuthenticationMode.AadApplicationSni));
        Assert.That(sources[1].KustoInfo!.AppId, Is.EqualTo("current-sni-app"));
        Assert.That(sources[1].KustoInfo!.CertificateThumbprint, Is.EqualTo("current-offline-thumbprint"));
        sourceRepository.VerifyNoOtherCalls();
        targetRepository.VerifyNoOtherCalls();
    }

    private static void SetCredentials(SchemaPickerControl picker, int mode, string prefix)
    {
        Field<ComboBox>(picker, "cmbAuthentication").SelectedIndex = mode;
        Field<ComboBox>(picker, "cbAppId").Text = prefix + "-app";
        Field<TextBox>(picker, "txtAppKey").Text = prefix + "-offline-key";
        Field<ComboBox>(picker, "cbAppIdSni").Text = prefix + "-sni-app";
        Field<TextBox>(picker, "txtCertificate").Text = prefix + "-offline-thumbprint";
    }

    private sealed class TrackingFactory(ISettingsProvider settings, IKustoConnectionFactory connections)
        : SchemaRepositoryFactory(settings, connections)
    {
        public List<ISchemaRepository> Repositories { get; } = new();

        public override ISchemaRepository CreateRepository(SchemaSourceInfo info)
        {
            var repository = base.CreateRepository(info);
            Repositories.Add(repository);
            return repository;
        }
    }
}
