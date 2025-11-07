// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using SyncKusto.Core.Abstractions;
using System;
using System.Windows.Forms;

namespace SyncKusto
{
    /// <summary>
    /// Drop warning dialog form
    /// </summary>
    public partial class DropWarningForm : Form
    {
        private readonly ISettingsProvider? _settingsProvider;

        /// <summary>
        /// Default constructor for designer support
        /// </summary>
        public DropWarningForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Constructor with dependency injection
        /// </summary>
        /// <param name="settingsProvider">Settings provider for saving user preferences</param>
        public DropWarningForm(ISettingsProvider settingsProvider) : this()
        {
            _settingsProvider = settingsProvider ?? throw new ArgumentNullException(nameof(settingsProvider));
        }

        /// <summary>
        /// Close the form without saving anything
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.No;
            this.Close();
        }

        /// <summary>
        /// Save the user's preference and close the form
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnOk_Click(object sender, EventArgs e)
        {
            // Save the drop warning setting if settings provider is available
            if (_settingsProvider != null)
            {
                _settingsProvider.SetSetting("KustoObjectDropWarning", chkNextTime.Checked.ToString());
            }
            
            this.DialogResult = DialogResult.Yes;
            this.Close();
        }
    }
}
