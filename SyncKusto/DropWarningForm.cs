// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Windows.Forms;

namespace SyncKusto
{
    /// <summary>
    /// Collect some settings from the user
    /// </summary>
    public partial class DropWarningForm : Form
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public DropWarningForm()
        {
            InitializeComponent();
        }

        ///// <summary>
        ///// Close the form without saving anything
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.No;
            this.Close();
        }

        /// <summary>
        /// Test out the settings before saving them.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnOk_Click(object sender, EventArgs e)
        {
            SettingsWrapper.KustoObjectDropWarning = chkNextTime.Checked;
            this.DialogResult = DialogResult.Yes;
            this.Close();
        }
    }
}
