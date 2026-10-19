// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using NUnit.Framework;
using System.ComponentModel;
using System.Reflection;

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

    [Test]
    public void ResetCallback_IsNotBrowsableOrDesignerSerialized()
    {
        var property = typeof(SchemaPickerControl).GetProperty(nameof(SchemaPickerControl.ResetMainFormValueHolders))!;

        Assert.That(property.GetCustomAttribute<BrowsableAttribute>()?.Browsable, Is.False);
        Assert.That(property.GetCustomAttribute<DesignerSerializationVisibilityAttribute>()?.Visibility,
            Is.EqualTo(DesignerSerializationVisibility.Hidden));
    }
}
