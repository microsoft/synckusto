// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Moq;
using NUnit.Framework;
using SyncKusto.Core.Abstractions;
using SyncKusto.Core.Exceptions;
using SyncKusto.Core.Models;
using SyncKusto.Kusto.Exceptions;
using SyncKusto.Services;
using static SyncKusto.Tests.Forms.FormsTestHelper;

namespace SyncKusto.Tests.Forms;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public class SettingsFormTests
{
    private const string Cluster = "https://temp.invalid";
    private const string Database = "TempDatabase";
    private string _directory = null!;
    private string _settingsPath = null!;
    private JsonFileSettingsProvider _provider = null!;
    private Mock<IKustoValidationService> _validation = null!;

    [SetUp]
    public void SetUp()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"SyncKustoFormSettings_{Guid.NewGuid()}");
        Directory.CreateDirectory(_directory);
        _settingsPath = Path.Combine(_directory, "settings.json");
        _provider = new JsonFileSettingsProvider(_settingsPath);
        _validation = new Mock<IKustoValidationService>();
        _validation.Setup(v => v.ValidateKustoSettingsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Cluster);
        _validation.Setup(v => v.CheckDatabaseEmptyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((0L, 0L));
    }

    [TearDown]
    public void TearDown()
    {
        Directory.Delete(_directory, true);
    }

    [Test]
    public void SuccessfulOK_PersistsAllSettingsAndReturnsOK()
    {
        using var form = new SettingsForm(_provider, _validation.Object);
        var result = DialogResult.None;

        WithModal<SettingsForm>(() => result = form.ShowDialog(), dialog =>
        {
            FillSettings(dialog);
            Field<Button>(dialog, "btnOk").PerformClick();
        });

        var settings = _provider.GetSyncKustoSettings();
        Assert.That(result, Is.EqualTo(DialogResult.OK));
        Assert.That(settings.TempCluster, Is.EqualTo(Cluster));
        Assert.That(settings.TempDatabase, Is.EqualTo(Database));
        Assert.That(settings.AADAuthority, Is.EqualTo("organizations"));
        Assert.That(settings.TableFieldsOnNewLine, Is.True);
        Assert.That(settings.CreateMergeEnabled, Is.True);
        Assert.That(settings.KustoObjectDropWarning, Is.False);
        Assert.That(settings.UseLegacyCslExtension, Is.False);
        Assert.That(settings.LineEndingMode, Is.EqualTo(LineEndingMode.UnixStyle));
        Assert.That(settings.CertificateLocation, Is.EqualTo(StoreLocation.LocalMachine));
        Assert.That(new JsonFileSettingsProvider(_settingsPath).GetSettings(), Is.EqualTo(settings));
        _validation.Verify(v => v.CheckDatabaseEmptyAsync(Cluster, Database, "organizations"), Times.Once);
    }

    [Test]
    public void Cancel_DoesNotWriteSettingsOrValidate()
    {
        using var form = new SettingsForm(_provider, _validation.Object);
        var result = DialogResult.None;

        WithModal<SettingsForm>(() => result = form.ShowDialog(), dialog =>
        {
            FillSettings(dialog);
            Field<Button>(dialog, "btnCancel").PerformClick();
        });

        Assert.That(result, Is.EqualTo(DialogResult.Cancel));
        Assert.That(File.Exists(_settingsPath), Is.False);
        _validation.VerifyNoOtherCalls();
    }

    [Test]
    public void FailedValidationThenCancel_DoesNotSaveAnyChanges()
    {
        _provider.SetSetting("TempCluster", Cluster);
        _provider.SetSetting("TempDatabase", Database);
        var originalFile = File.ReadAllText(_settingsPath);
        _validation.Setup(v => v.ValidateKustoSettingsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new KustoSettingsException("No Kusto database was specified."));
        using var form = new SettingsForm(_provider, _validation.Object);

        WithModal<SettingsForm>(() => form.ShowDialog(), dialog =>
        {
            FillSettings(dialog);
            Field<TextBox>(dialog, "txtKustoDatabase").Text = "";
            WithMessageBox(() => Field<Button>(dialog, "btnOk").PerformClick(),
                "Missing Info", DialogResult.OK, "No Kusto database was specified.");
            Assert.That(Field<Button>(dialog, "btnOk").Enabled, Is.True);
            Assert.That(Field<Button>(dialog, "btnCancel").Enabled, Is.True);
            Assert.That(Field<TabControl>(dialog, "tabControl").Enabled, Is.True);
            Field<Button>(dialog, "btnCancel").PerformClick();
        });

        Assert.That(File.ReadAllText(_settingsPath), Is.EqualTo(originalFile));
        Assert.That(_provider.GetSyncKustoSettings().TableFieldsOnNewLine, Is.False);
    }

    [TestCase(DialogResult.Yes)]
    [TestCase(DialogResult.No)]
    public void NonEmptyDatabase_SavesOnlyAfterConfirmation(DialogResult response)
    {
        _validation.Setup(v => v.CheckDatabaseEmptyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new KustoDatabaseValidationException(Cluster, Database, 1, 1, "Not empty."));
        using var form = new SettingsForm(_provider, _validation.Object);
        var result = DialogResult.None;

        WithModal<SettingsForm>(() => result = form.ShowDialog(), dialog =>
        {
            FillSettings(dialog);
            WithMessageBox(() => Field<Button>(dialog, "btnOk").PerformClick(),
                "Non-Empty Database", response, $"'{Database}'");
            if (response == DialogResult.No)
            {
                Assert.That(File.Exists(_settingsPath), Is.False);
                Field<Button>(dialog, "btnCancel").PerformClick();
            }
        });

        Assert.That(result, Is.EqualTo(response == DialogResult.Yes ? DialogResult.OK : DialogResult.Cancel));
        Assert.That(File.Exists(_settingsPath), Is.EqualTo(response == DialogResult.Yes));
    }

    [Test]
    public void AuthorityChange_RevalidatesExistingTemporaryDatabase()
    {
        _provider.SetSetting("TempCluster", Cluster);
        _provider.SetSetting("TempDatabase", Database);
        _provider.SetSetting("AADAuthority", "old-authority");
        using var form = new SettingsForm(_provider, _validation.Object);

        WithModal<SettingsForm>(() => form.ShowDialog(), dialog =>
        {
            Field<TextBox>(dialog, "txtAuthority").Text = "new-authority";
            Field<Button>(dialog, "btnOk").PerformClick();
        });

        _validation.Verify(v => v.ValidateKustoSettingsAsync(Cluster, Database, "new-authority"), Times.Once);
        Assert.That(_provider.GetSetting("AADAuthority"), Is.EqualTo("new-authority"));
    }

    [Test]
    public void PendingValidation_DisablesEditsAndPreventsClosingUntilComplete()
    {
        var pending = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        _validation.Setup(v => v.ValidateKustoSettingsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(pending.Task);
        using var form = new SettingsForm(_provider, _validation.Object);
        form.Show();
        FillSettings(form);

        Field<Button>(form, "btnOk").PerformClick();

        Assert.That(Field<Button>(form, "btnOk").Enabled, Is.False);
        Assert.That(Field<Button>(form, "btnCancel").Enabled, Is.False);
        Assert.That(Field<TabControl>(form, "tabControl").Enabled, Is.False);
        form.Close();
        Assert.That(form.Visible, Is.True);
        Assert.That(File.Exists(_settingsPath), Is.False);

        pending.SetResult(Cluster);
        PumpUntil(() => form.IsDisposed);
        Assert.That(_provider.GetSetting("TempDatabase"), Is.EqualTo(Database));
    }

    private static void FillSettings(SettingsForm form)
    {
        Field<TextBox>(form, "txtKustoCluster").Text = "temp";
        Field<TextBox>(form, "txtKustoDatabase").Text = Database;
        Field<TextBox>(form, "txtAuthority").Text = "organizations";
        Field<CheckBox>(form, "cbTableFieldsOnNewLine").Checked = true;
        Field<CheckBox>(form, "cbCreateMerge").Checked = true;
        Field<CheckBox>(form, "chkTableDropWarning").Checked = false;
        Field<CheckBox>(form, "cbUseLegacyCslExtension").Checked = false;
        Field<RadioButton>(form, "rbLineEndingsUnix").Checked = true;
        Field<ComboBox>(form, "cbCertLocation").SelectedItem = StoreLocation.LocalMachine;
    }
}
