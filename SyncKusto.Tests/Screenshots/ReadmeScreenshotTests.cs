// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Kusto.Data.Common;
using Moq;
using NUnit.Framework;
using SyncKusto.Abstractions;
using SyncKusto.Core.Abstractions;
using SyncKusto.Core.Models;
using SyncKusto.Core.Services;
using SyncKusto.ErrorHandling;
using SyncKusto.Kusto.Models;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;
using static SyncKusto.Tests.Forms.FormsTestHelper;

namespace SyncKusto.Tests.Screenshots;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public class ReadmeScreenshotTests
{
    [Test]
    [Explicit("Run scripts\\Update-ReadmeScreenshot.ps1 to refresh the README image.")]
    public void Capture()
    {
        string? outputPath = Environment.GetEnvironmentVariable("SYNCKUSTO_SCREENSHOT_PATH");
        Assert.That(outputPath, Is.Not.Null.And.Not.Empty, "Use scripts\\Update-ReadmeScreenshot.ps1.");
        Assert.That(Environment.UserInteractive, Is.True, "Screenshot capture requires a Windows desktop session.");

        Application.SetHighDpiMode(HighDpiMode.DpiUnaware);
        Application.SetDefaultFont(new Font("Microsoft Sans Serif", 8.25f));
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var source = new DatabaseSchema("demo-source", "");
        var target = new DatabaseSchema("demo-target", "");
        var current = CreateFunction("{\r\n    print now(), Yesterday=ago(1d)\r\n}");
        source.Functions.Add(current.Name, current);
        target.Functions.Add(current.Name, CreateFunction("{\r\n    print now()\r\n}"));
        var comparison = new ComparisonResult(
            new SchemaDifferenceResult(Array.Empty<SchemaDifference>(),
                new[] { new FunctionSchemaDifference(new Modified(), new KustoFunctionSchema(current)) }),
            source, target);
        var presenter = new Mock<IMainFormPresenter>(MockBehavior.Strict);
        presenter.Setup(p => p.CompareAsync(It.IsAny<SchemaSourceInfo>(), It.IsAny<SchemaSourceInfo>(),
            It.IsAny<IProgress<SyncProgress>>(), default)).ReturnsAsync(comparison);
        var settings = new InMemorySettingsProvider();
        var validation = new SchemaValidationService(settings);
        var kustoValidation = new Mock<IKustoValidationService>(MockBehavior.Strict);
        using var form = new CaptureForm(presenter.Object, settings, validation, kustoValidation.Object)
        {
            StartPosition = FormStartPosition.Manual,
            Location = new Point(0, 0),
            Size = new Size(655, 854),
            ShowInTaskbar = false
        };
        var sourcePicker = form.MainField<SchemaPickerControl>("spcSource");
        Field<TextBox>(sourcePicker, "txtFilePath").Text = @"C:\Demo\Weather";
        var targetPicker = form.MainField<SchemaPickerControl>("spcTarget");
        Field<RadioButton>(targetPicker, "rbKusto").Checked = true;
        Field<ComboBox>(targetPicker, "cbCluster").Text = "https://demo.kusto.windows.net";
        Field<ComboBox>(targetPicker, "cbDatabase").Text = "Weather";
        form.MainField<Label>("label2").Visible = false;
        form.Show();

        var compare = form.CompareSchemasAsync(sourcePicker.GetSourceInfo(), targetPicker.GetSourceInfo());
        PumpUntil(() => compare.IsCompleted);
        compare.GetAwaiter().GetResult();
        var tree = form.MainField<TreeView>("tvComparison");
        tree.ExpandAll();
        var functionNode = tree.Nodes[0].Nodes[0].Nodes[0];
        functionNode.Checked = true;
        tree.HideSelection = false;
        tree.SelectedNode = functionNode;
        typeof(MainForm).GetMethod("tvComparison_NodeMouseClick", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(form, new object[] { tree, new TreeNodeMouseClickEventArgs(functionNode, MouseButtons.Left, 1, 0, 0) });
        var diff = form.MainField<RichTextBox>("rtbSourceText");
        diff.SelectionStart = 0;
        diff.SelectionLength = 0;
        diff.ScrollToCaret();
        form.Refresh();
        Application.DoEvents();

        Assert.That(form.Size, Is.EqualTo(new Size(655, 854)));
        Assert.That(functionNode.Text, Is.EqualTo("CurrentTime"));
        Assert.That(functionNode.Checked, Is.True);
        Assert.That(diff.Text, Does.Contain("print now()").And.Contain("Yesterday=ago(1d)"));
        Assert.That(form.MainField<ToolStripButton>("btnUpdate").Enabled, Is.True);
        presenter.Verify(p => p.CompareAsync(It.IsAny<SchemaSourceInfo>(), It.IsAny<SchemaSourceInfo>(),
            It.IsAny<IProgress<SyncProgress>>(), default), Times.Once);
        presenter.VerifyNoOtherCalls();
        kustoValidation.VerifyNoOtherCalls();

        Assert.That(form.MainField<Label>("label2").Visible, Is.False, "Screenshots must omit the build-version label.");
        using var bitmap = new Bitmap(form.Width, form.Height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            nint deviceContext = graphics.GetHdc();
            try
            {
                Assert.That(PrintWindow(form.Handle, deviceContext, 0), Is.True, "Windows could not render the demo window.");
            }
            finally
            {
                graphics.ReleaseHdc(deviceContext);
            }
        }

        // RichTextBox is not reliably rendered by Control.DrawToBitmap.
        Assert.That(CountPixels(bitmap, Color.Red), Is.GreaterThan(500), "The deleted diff line was not captured.");
        Assert.That(CountPixels(bitmap, Color.Yellow), Is.GreaterThan(500), "The inserted diff line was not captured.");
        bitmap.Save(outputPath!, ImageFormat.Png);
        using var saved = new Bitmap(outputPath!);
        Assert.That(saved.Size, Is.EqualTo(bitmap.Size));
    }

    private static FunctionSchema CreateFunction(string body) =>
        new("CurrentTime", new List<FunctionParameterSchema>(), body, "", "",
            FunctionSchema.FunctionKind.Unknown, Array.Empty<ColumnSchema>());

    private static int CountPixels(Bitmap bitmap, Color color)
    {
        int count = 0;
        for (int y = 0; y < bitmap.Height; y++)
            for (int x = 0; x < bitmap.Width; x++)
                if (bitmap.GetPixel(x, y).ToArgb() == color.ToArgb())
                    count++;
        return count;
    }

    private sealed class CaptureForm(
        IMainFormPresenter presenter,
        ISettingsProvider settings,
        ISchemaValidationService validation,
        IKustoValidationService kustoValidation)
        : MainForm(presenter, ErrorMessageResolverFactory.CreateDefault(), settings, validation, kustoValidation)
    {
        protected override bool ShowWithoutActivation => true;

        public T MainField<T>(string name) =>
            (T)typeof(MainForm).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(this)!;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PrintWindow(nint window, nint deviceContext, uint flags);
}
