// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Drawing;
using System.Windows.Forms;

namespace SyncKusto;

public sealed class ErrorDetailsForm : Form
{
    public ErrorDetailsForm(string heading, string details)
    {
        Text = "Kusto error";
        AutoScaleDimensions = new SizeF(7, 15);
        AutoScaleMode = AutoScaleMode.Font;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(720, 400);
        MinimumSize = new Size(480, 380);
        ShowInTaskbar = false;
        MinimizeBox = false;
        Padding = new Padding(12);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var summary = new Label
        {
            Name = "lblHeading",
            Text = heading,
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 10)
        };
        var report = new TextBox
        {
            Name = "txtErrorDetails",
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            WordWrap = false,
            ScrollBars = ScrollBars.Both,
            MaxLength = 0,
            Margin = Padding.Empty,
            Text = details
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
        layout.Controls.Add(report, 0, 1);
        layout.Controls.Add(close, 0, 2);
        Controls.Add(layout);
        AcceptButton = close;
        CancelButton = close;
    }
}
