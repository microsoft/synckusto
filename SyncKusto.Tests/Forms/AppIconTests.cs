// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Moq;
using Kusto.Data.Common;
using NUnit.Framework;
using SyncKusto.Abstractions;
using SyncKusto.Core.Abstractions;
using SyncKusto.Core.Exceptions;
using SyncKusto.Core.Models;
using SyncKusto.Core.Services;
using SyncKusto.ErrorHandling;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using static SyncKusto.Tests.Forms.FormsTestHelper;

namespace SyncKusto.Tests.Forms;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public class AppIconTests
{
    private static IEnumerable<Type> ApplicationForms() =>
        typeof(MainForm).Assembly.GetTypes().Where(type => type.IsSubclassOf(typeof(Form)) && !type.IsAbstract);

    [TestCaseSource(nameof(ApplicationForms))]
    public void EveryApplicationForm_UsesSharedOriginalIconIncludingDisplayedWindow(Type type)
    {
        using var form = CreateForm(type);
        Assert.That(form.Icon, Is.SameAs(AppIcon.Value));
        Assert.That(form.ShowIcon, Is.True);
        form.Show();
        Application.DoEvents();

        foreach (int size in new[] { 0, 1 }) // WM_GETICON: small caption icon and large application icon.
        {
            nint handle = SendMessage(form.Handle, 0x007F, size, 0);
            Assert.That(handle, Is.Not.EqualTo(0), $"{type.Name} has no native icon for size {size}.");
            using var displayed = Icon.FromHandle(handle);
            AssertIconImage(displayed);
        }
    }

    [Test]
    public void SharedIcon_MatchesExistingMainFormResourceAndBuiltExecutable_AndSurvivesFormDisposal()
    {
        using var original = (Icon)new ComponentResourceManager(typeof(MainForm)).GetObject("$this.Icon")!;
        using var expected = new MemoryStream();
        using var actual = new MemoryStream();
        original.Save(expected);
        AppIcon.Value.Save(actual);
        Assert.That(actual.ToArray(), Is.EqualTo(expected.ToArray()), "Reuse the original asset, not a lookalike.");
        var executable = Path.Combine(Path.GetDirectoryName(typeof(MainForm).Assembly.Location)!, "SyncKusto.exe");
        Assert.That(File.Exists(executable), Is.True);
        using var executableIcon = Icon.ExtractAssociatedIcon(executable);
        Assert.That(executableIcon, Is.Not.Null);
        AssertIconImage(executableIcon!);

        using (var dialog = new ErrorDetailsForm("Offline check", "Synthetic diagnostics"))
            _ = dialog.Handle;
        using var next = new SkippedFilesForm(Array.Empty<SkippedSchemaFile>());
        next.Show();
        using var nextIcon = Icon.FromHandle(SendMessage(next.Handle, 0x007F, 0, 0));
        AssertIconImage(nextIcon);
    }

    [Test]
    public void FolderPicker_IsOwnedByMainForm_AndRetainsNativeCaptionBehavior()
    {
        using var form = CreateMainForm();
        form.Show();
        var picker = Field<SchemaPickerControl>(form, "spcSource");
        var method = typeof(SchemaPickerControl).GetMethod("ShowFolderDialog", BindingFlags.NonPublic | BindingFlags.Instance)!;
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select schema folder",
            UseDescriptionForTitle = true,
            SelectedPath = TestContext.CurrentContext.TestDirectory
        };

        WithNativeDialog(() =>
        {
            var result = (DialogResult)method.Invoke(picker, new object[] { dialog })!;
            Assert.That(result, Is.EqualTo(DialogResult.Cancel));
        }, "Select schema folder", window => AssertNativeOwner(window, form));
    }

    [Test]
    public void NativePickers_ShareContainingFormOwner_WithStandaloneControlFallback()
    {
        using var form = CreateMainForm();
        form.Show();
        foreach (string field in new[] { "spcSource", "spcTarget" })
        {
            var picker = Field<SchemaPickerControl>(form, field);
            Assert.That(picker.DialogOwner, Is.SameAs(form));
            Assert.That(picker.DialogOwner.Handle, Is.EqualTo(form.Handle));
        }
        using var standalone = new SchemaPickerControl();
        Assert.That(standalone.DialogOwner, Is.SameAs(standalone));
    }

    [Test]
    public void MainFormMissingInputMessage_IsOwnedAndKeepsStandardErrorSymbol()
    {
        using var form = CreateMainForm();
        form.Show();

        WithNativeDialog(() => Field<ToolStripButton>(form, "btnCompare").PerformClick(), "Error", window =>
        {
            AssertNativeOwner(window, form);
            AssertMessageSymbol(window, SystemIcons.Error);
        });
    }

    [TestCase(true)]
    [TestCase(false)]
    public void UpdateMessages_AreOwnedAndKeepStandardInformationOrWarningSymbols(bool success)
    {
        var presenter = new Mock<IMainFormPresenter>();
        var difference = new TableSchemaDifference(new OnlyInSource(), new Core.TestKustoSchema("OfflineTable"));
        presenter.Setup(service => service.CompareAsync(It.IsAny<SchemaSourceInfo>(), It.IsAny<SchemaSourceInfo>(),
                It.IsAny<IProgress<SyncProgress>>(), default))
            .ReturnsAsync(new ComparisonResult(
                new SchemaDifferenceResult(new[] { difference }, Array.Empty<SchemaDifference>()),
                new DatabaseSchema("source", ""),
                new DatabaseSchema("target", "")));
        presenter.Setup(service => service.SynchronizeAsync(It.IsAny<IEnumerable<SchemaDifference>>(), It.IsAny<SchemaSourceInfo>(),
                It.IsAny<IProgress<SyncProgress>>(), default))
            .ReturnsAsync(success ? SyncResult.Successful(1) : SyncResult.Failed(new[] { "Offline partial failure" }));
        using var form = new MainForm(presenter.Object, ErrorMessageResolverFactory.CreateDefault(), new InMemorySettingsProvider(),
            Mock.Of<ISchemaValidationService>(), Mock.Of<IKustoValidationService>());
        form.Show();
        foreach (string pickerName in new[] { "spcSource", "spcTarget" })
            Field<TextBox>(Field<SchemaPickerControl>(form, pickerName), "txtFilePath").Text = @"C:\offline-schema";
        form.CompareSchemasAsync(new SchemaSourceInfo(SourceSelection.FilePath(), @"C:\offline-schema"),
            new SchemaSourceInfo(SourceSelection.FilePath(), @"C:\offline-schema")).GetAwaiter().GetResult();
        var node = Field<TreeView>(form, "tvComparison").Nodes.Cast<TreeNode>()
            .SelectMany(root => root.Nodes.Cast<TreeNode>()).SelectMany(group => group.Nodes.Cast<TreeNode>()).Single();
        node.Checked = true;

        WithNativeDialog(() => Field<ToolStripButton>(form, "btnUpdate").PerformClick(),
            success ? "Complete" : "Partial Success", window =>
            {
                AssertNativeOwner(window, form);
                AssertMessageSymbol(window, success ? SystemIcons.Information : SystemIcons.Warning);
            });
    }

    [Test]
    public void SettingsMissingInputMessage_IsOwnedAndKeepsStandardErrorSymbol()
    {
        var validation = new Mock<IKustoValidationService>();
        validation.Setup(service => service.ValidateKustoSettingsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new KustoSettingsException("Offline missing-input check"));
        using var form = new SettingsForm(new InMemorySettingsProvider(), validation.Object);
        form.Show();
        Field<TextBox>(form, "txtKustoCluster").Text = "https://offline.invalid";

        WithNativeDialog(() => Field<Button>(form, "btnOk").PerformClick(), "Missing Info", window =>
        {
            AssertNativeOwner(window, form);
            AssertMessageSymbol(window, SystemIcons.Error);
        });
    }

    private static Form CreateForm(Type type)
    {
        if (type == typeof(MainForm))
            return CreateMainForm();
        if (type == typeof(SettingsForm))
            return new SettingsForm(new InMemorySettingsProvider(), Mock.Of<IKustoValidationService>());
        if (type == typeof(DropWarningForm))
            return new DropWarningForm(new InMemorySettingsProvider());
        if (type == typeof(ErrorDetailsForm))
            return new ErrorDetailsForm("Couldn't update the target schema.", "403-Forbidden: offline diagnostic");
        if (type == typeof(SkippedFilesForm))
            return new SkippedFilesForm(new[] { new SkippedSchemaFile("Source", "Example.kql", "is unsupported", "Use a supported definition.") });
        throw new AssertionException($"Add a constructor for application form '{type.Name}' to the branding regression.");
    }

    private static MainForm CreateMainForm() =>
        new(Mock.Of<IMainFormPresenter>(), ErrorMessageResolverFactory.CreateDefault(), new InMemorySettingsProvider(),
            Mock.Of<ISchemaValidationService>(), Mock.Of<IKustoValidationService>());

    private static void AssertNativeOwner(nint window, Form owner)
    {
        Assert.That(GetWindow(window, 4), Is.EqualTo(owner.Handle), "The native popup must be owned by its SyncKusto form.");
        Assert.That(IsWindowEnabled(owner.Handle), Is.False, "The owned form must stay disabled during the modal popup.");
        TestContext.WriteLine($"Native caption icons: small={SendMessage(window, 0x007F, 0, 0)}, large={SendMessage(window, 0x007F, 1, 0)}; OS-managed.");
    }

    private static void AssertMessageSymbol(nint window, Icon expected)
    {
        nint handle = 0;
        EnumChildWindows(window, (child, _) =>
        {
            nint icon = SendMessage(child, 0x0171, 0, 0); // STM_GETICON
            if (icon != 0)
                handle = icon;
            return true;
        }, 0);
        Assert.That(handle, Is.Not.EqualTo(0));
        using var displayed = Icon.FromHandle(handle);
        AssertIconImage(displayed, expected);
    }

    private static void AssertIconImage(Icon actual, Icon? expected = null)
    {
        using var expectedSize = new Icon(expected ?? AppIcon.Value, actual.Size);
        using var expectedBitmap = expectedSize.ToBitmap();
        using var actualBitmap = actual.ToBitmap();
        Assert.That(actualBitmap.Size, Is.EqualTo(expectedBitmap.Size));
        for (int y = 0; y < actualBitmap.Height; y++)
        for (int x = 0; x < actualBitmap.Width; x++)
            Assert.That(actualBitmap.GetPixel(x, y), Is.EqualTo(expectedBitmap.GetPixel(x, y)), $"Icon pixel ({x}, {y}) differs.");
    }

    private delegate bool WindowCallback(nint window, nint parameter);
    [DllImport("user32.dll")] private static extern nint SendMessage(nint window, uint message, nint wParam, nint lParam);
    [DllImport("user32.dll")] private static extern nint GetWindow(nint window, uint command);
    [DllImport("user32.dll")] private static extern bool IsWindowEnabled(nint window);
    [DllImport("user32.dll")] private static extern bool EnumChildWindows(nint window, WindowCallback callback, nint parameter);
}
