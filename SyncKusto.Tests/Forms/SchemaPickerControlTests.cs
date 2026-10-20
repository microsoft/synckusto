// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using NUnit.Framework;
using System.ComponentModel;
using System.Reflection;
using SyncKusto.Core.Services;

namespace SyncKusto.Tests.Forms;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public class SchemaPickerControlTests
{
    [Test]
    public void Title_DesignerSerializesOnlyNonDefaultValues()
    {
        using var control = new SchemaPickerControl();
        var property = TypeDescriptor.GetProperties(control)[nameof(SchemaPickerControl.Title)]!;

        Assert.That(control.Title, Is.EqualTo("Source Schema"));
        Assert.That(property.ShouldSerializeValue(control), Is.False);

        property.SetValue(control, "Target Schema");

        Assert.That(control.Title, Is.EqualTo("Target Schema"));
        Assert.That(property.ShouldSerializeValue(control), Is.True);

        property.ResetValue(control);

        Assert.That(control.Title, Is.EqualTo("Source Schema"));
        Assert.That(property.ShouldSerializeValue(control), Is.False);
    }

    [TestCase(DialogResult.Cancel)]
    [TestCase(DialogResult.OK)]
    public void FolderPicker_UsesShortNativeTitleCurrentPathAndPreservesCancelWithoutSavingHistory(DialogResult result)
    {
        var provider = new InMemorySettingsProvider();
        provider.SetSetting("PreviousFilePath", @"C:\previous");
        using var control = new TestPicker(result);
        control.Initialize(provider);
        int resets = 0;
        control.ResetMainFormValueHolders = () => resets++;

        typeof(SchemaPickerControl).GetMethod("btnChooseDirectory_Click", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(control, new object[] { control, EventArgs.Empty });

        Assert.That(control.WasShown, Is.True);
        Assert.That(control.WasDisposed, Is.True);
        Assert.That(Field<TextBox>(control, "txtFilePath").Text,
            Is.EqualTo(result == DialogResult.OK ? @"C:\selected" : @"C:\previous"));
        Assert.That(provider.GetSetting("PreviousFilePath"), Is.EqualTo(@"C:\previous"),
            "The picker must not save history before a comparison actually uses the folder.");
        Assert.That(resets, Is.EqualTo(result == DialogResult.OK ? 1 : 0));
    }

    private sealed class TestPicker(DialogResult result) : SchemaPickerControl
    {
        public bool WasShown { get; private set; }
        public bool WasDisposed { get; private set; }

        protected override DialogResult ShowFolderDialog(FolderBrowserDialog dialog)
        {
            WasShown = true;
            dialog.Disposed += (_, _) => WasDisposed = true;
            Assert.That(dialog.UseDescriptionForTitle, Is.True);
            Assert.That(dialog.Description, Is.EqualTo("Select schema folder"));
            Assert.That(dialog.SelectedPath, Is.EqualTo(@"C:\previous"));
            Assert.That(dialog.AutoUpgradeEnabled, Is.True);
            Assert.That(dialog.ShowNewFolderButton, Is.False);
            dialog.SelectedPath = @"C:\selected";
            return result;
        }
    }

    private static T Field<T>(SchemaPickerControl picker, string name) =>
        (T)typeof(SchemaPickerControl).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(picker)!;

    [Test]
    public void ResetCallback_IsNotBrowsableOrDesignerSerialized()
    {
        var property = typeof(SchemaPickerControl).GetProperty(nameof(SchemaPickerControl.ResetMainFormValueHolders))!;

        Assert.That(property.GetCustomAttribute<BrowsableAttribute>()?.Browsable, Is.False);
        Assert.That(property.GetCustomAttribute<DesignerSerializationVisibilityAttribute>()?.Visibility,
            Is.EqualTo(DesignerSerializationVisibility.Hidden));
    }
}
