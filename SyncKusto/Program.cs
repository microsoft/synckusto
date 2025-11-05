// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Windows.Forms;
using System.Drawing;

namespace SyncKusto
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Revert some changes that came from the upgrade to .NET 8 regarding high DPI settings
            Application.SetHighDpiMode(HighDpiMode.DpiUnaware);
            Application.SetDefaultFont(new Font("Microsoft Sans Serif", 8.25f));

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}