// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using NUnit.Framework;
using SyncKusto.Core.Models;
using System.Drawing;

namespace SyncKusto.Tests.Forms;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public class DiagnosticDialogTests
{
    [TestCase(false, 1)]
    [TestCase(false, 200)]
    [TestCase(true, 1)]
    [TestCase(true, 200)]
    public void Dialogs_KeepCompleteCopyableReportsBoundedAndResizeAtLargerFont(bool errors, int count)
    {
        var longText = string.Join(Environment.NewLine,
            Enumerable.Range(0, count).Select(index => $"{index}: " + new string('x', 400)));
        var files = Enumerable.Range(0, count).Select(index => new SkippedSchemaFile(
            index % 2 == 0 ? "Source" : "Target", @"Functions\" + new string('a', 260) + index + ".kql",
            "has a syntax error", "Complete the expression and compare again.", 30, 40)).ToArray();
        using Form form = errors ? new ErrorDetailsForm("Couldn't compare schemas.", longText) : new SkippedFilesForm(files);
        form.Show();
        var report = (TextBox)form.Controls.Find(errors ? "txtErrorDetails" : "txtSkippedFiles", true).Single();
        var close = (Button)form.Controls.Find("btnClose", true).Single();
        Assert.That(form.ClientSize.Width, Is.LessThanOrEqualTo(740));
        Assert.That(form.ClientSize.Height, Is.LessThanOrEqualTo(420));
        if (errors)
            Assert.That(report.Text, Is.EqualTo(longText));
        else
            foreach (var file in files)
                Assert.That(report.Text, Does.Contain(file.RelativePath).And.Contain(file.Reason).And.Contain(file.Hint)
                    .And.Contain($"line {file.Line}, column {file.Column}"));
        if (count == 200)
            Assert.That(report.TextLength, Is.GreaterThan(32767));
        Assert.That(report.MaxLength, Is.Zero);
        Assert.That(report.ReadOnly, Is.True);
        Assert.That(report.ShortcutsEnabled, Is.True);
        Assert.That(report.Multiline, Is.True);
        Assert.That(report.ScrollBars, Is.EqualTo(ScrollBars.Both));
        Assert.That(report.WordWrap, Is.False);
        Assert.That(form.AcceptButton, Is.SameAs(close));
        Assert.That(form.CancelButton, Is.SameAs(close));
        report.SelectAll();
        Assert.That(report.SelectedText, Is.EqualTo(report.Text));

        int initialWidth = report.Width;
        int initialHeight = report.Height;
        form.ClientSize = new Size(form.ClientSize.Width + 200, form.ClientSize.Height + 150);
        form.PerformLayout();
        Assert.That(report.Width, Is.GreaterThan(initialWidth));
        Assert.That(report.Height, Is.GreaterThan(initialHeight));
        form.Size = form.MinimumSize;
        form.PerformLayout();
        AssertLayout(form, report, close);
        using var largerFont = new Font(form.Font.FontFamily, 14f);
        form.Font = largerFont;
        form.PerformLayout();
        AssertLayout(form, report, close);
        form.Size = form.MinimumSize;
        form.PerformLayout();
        AssertLayout(form, report, close);
    }

    private static void AssertLayout(Form form, TextBox report, Button close)
    {
        Assert.That(report.Width, Is.GreaterThan(100));
        Assert.That(report.ClientSize.Height - SystemInformation.HorizontalScrollBarHeight,
            Is.GreaterThanOrEqualTo(report.Font.Height * 3),
            "At least three complete report lines must remain readable at minimum size and larger fonts.");
        Assert.That(close.Width, Is.LessThan(report.Width / 2));
        Assert.That(report.Bottom, Is.LessThan(close.Top));
        Assert.That(close.Right, Is.LessThanOrEqualTo(close.Parent!.ClientSize.Width));
        Assert.That(close.Bottom, Is.LessThanOrEqualTo(close.Parent!.ClientSize.Height));
        Assert.That(form.FormBorderStyle, Is.EqualTo(FormBorderStyle.Sizable));
        if (form.Controls.Find("lblWarning", true).SingleOrDefault() is Label warning)
        {
            Assert.That(warning.Bottom, Is.LessThan(report.Top));
            Assert.That(warning.Text, Does.Contain("deletion").And.Contain("overwrite or remove"));
            Assert.That(warning.MaximumSize, Is.EqualTo(Size.Empty));
        }
    }
}
