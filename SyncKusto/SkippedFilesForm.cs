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

        Text = $"Comparison incomplete: {skippedFiles.Count} file(s) skipped";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(640, 360);
        Size = new Size(900, 520);
        ShowInTaskbar = false;
        MinimizeBox = false;

        var warning = new Label
        {
            Name = "lblWarning",
            Dock = DockStyle.Top,
            AutoSize = true,
            MaximumSize = new Size(600, 0),
            Padding = new Padding(8),
            Text = "Skipped files were not loaded into the temporary database. This comparison covers only " +
                "accepted table and function definitions.\n\n" +
                "Update behavior is unchanged: skipped source definitions can make target objects appear " +
                "eligible for deletion, and file-target updates can overwrite or remove skipped files. " +
                "Review selected changes before Update."
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
            Text = string.Join(Environment.NewLine + Environment.NewLine,
                skippedFiles.Select(file => file.Message))
        };
        var close = new Button
        {
            Text = "Close",
            Dock = DockStyle.Bottom,
            Height = 32,
            DialogResult = DialogResult.OK
        };
        Controls.Add(report);
        Controls.Add(close);
        Controls.Add(warning);
        AcceptButton = close;
        CancelButton = close;
    }
}
