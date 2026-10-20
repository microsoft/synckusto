// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using SyncKusto.Core.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SyncKusto;

public sealed class SkippedFilesForm : Form
{
    public SkippedFilesForm(IReadOnlyList<SkippedSchemaFile> skippedFiles)
    {
        ArgumentNullException.ThrowIfNull(skippedFiles);

        Icon = AppIcon.Value;
        Text = $"Comparison incomplete: {skippedFiles.Count} file(s) skipped";
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleDimensions = new SizeF(7, 15);
        AutoScaleMode = AutoScaleMode.Font;
        MinimumSize = new Size(480, 380);
        ClientSize = new Size(720, 380);
        Padding = new Padding(12);
        ShowInTaskbar = false;
        MinimizeBox = false;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var summary = new Label
        {
            Name = "lblSummary",
            Text = $"{skippedFiles.Count} file(s) skipped. Comparison is incomplete.",
            Dock = DockStyle.Fill,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 6)
        };
        var warning = new Label
        {
            Name = "lblWarning",
            Dock = DockStyle.Fill,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 10),
            Text = "Review changes before Update: skipped source definitions can cause target object deletion. " +
                "File-target updates can overwrite or remove skipped files."
        };
        var report = new TextBox
        {
            Name = "txtSkippedFiles",
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            WordWrap = false,
            ScrollBars = ScrollBars.Both,
            MaxLength = 0,
            Margin = Padding.Empty,
            Text = string.Join(Environment.NewLine + Environment.NewLine,
                skippedFiles.Select(file =>
                    $"{file.Side}: {file.RelativePath}{Environment.NewLine}  {file.Reason}." +
                    (file.Line.HasValue && file.Column.HasValue
                        ? $" (line {file.Line}, column {file.Column})" : "") +
                    $"{Environment.NewLine}  {file.Hint}"))
        };
        var close = new Button
        {
            Name = "btnClose",
            Text = "Close",
            AutoSize = true,
            MinimumSize = new Size(88, 28),
            Anchor = AnchorStyles.Right,
            Margin = new Padding(0, 10, 0, 0),
            DialogResult = DialogResult.OK
        };
        layout.Controls.Add(summary, 0, 0);
        layout.Controls.Add(warning, 0, 1);
        layout.Controls.Add(report, 0, 2);
        layout.Controls.Add(close, 0, 3);
        Controls.Add(layout);
        AcceptButton = close;
        CancelButton = close;
    }
}
