// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using SyncKusto.Core.Abstractions;
using SyncKusto.Core.Models;
using SyncKusto.Kusto.Exceptions;
using SyncKusto.ErrorHandling;
using System;
using System.Linq;
using System.Windows.Forms;

namespace SyncKusto
{
    /// <summary>
    /// Collect some settings from the user
    /// </summary>
    public partial class SettingsForm : Form
    {
        private RadioButton[] lineEndingRadioButtons;
        private readonly ISettingsProvider _settingsProvider;
        private readonly IKustoValidationService _kustoValidationService;
        private bool _isSaving;

        /// <summary>
        /// Default constructor for designer support
        /// </summary>
        public SettingsForm()
        {
            InitializeComponent();
            _settingsProvider = null!; // Will be set by public constructor
            _kustoValidationService = null!; // Will be set by public constructor

            cbCertLocation.DataSource = Enum.GetValues(typeof(Core.Models.StoreLocation));

            // Set the radiobutton tag fields to each of the corresponding LineEndingMode enum values
            rbLineEndingsLeave.Tag = LineEndingMode.LeaveAsIs;
            rbLineEndingsWindows.Tag = LineEndingMode.WindowsStyle;
            rbLineEndingsUnix.Tag = LineEndingMode.UnixStyle;

            lineEndingRadioButtons = new[]
            {
                rbLineEndingsLeave,
                rbLineEndingsWindows,
                rbLineEndingsUnix
            };
        }

        /// <summary>
        /// Constructor with dependency injection
        /// </summary>
        public SettingsForm(ISettingsProvider settingsProvider, IKustoValidationService kustoValidationService) : this()
        {
            _settingsProvider = settingsProvider ?? throw new ArgumentNullException(nameof(settingsProvider));
            _kustoValidationService = kustoValidationService ?? throw new ArgumentNullException(nameof(kustoValidationService));
        }

        /// <summary>
        /// Populate the existing settings into the text boxes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SettingsForm_Load(object sender, EventArgs e)
        {
            txtKustoCluster.Text = _settingsProvider.GetSetting("TempCluster") ?? string.Empty;
            txtKustoDatabase.Text = _settingsProvider.GetSetting("TempDatabase") ?? string.Empty;
            txtAuthority.Text = _settingsProvider.GetSetting("AADAuthority") ?? string.Empty;
            chkTableDropWarning.Checked = bool.Parse(_settingsProvider.GetSetting("KustoObjectDropWarning") ?? "true");
            cbTableFieldsOnNewLine.Checked = bool.Parse(_settingsProvider.GetSetting("TableFieldsOnNewLine") ?? "false");
            cbCreateMerge.Checked = bool.Parse(_settingsProvider.GetSetting("CreateMergeEnabled") ?? "false");
            cbUseLegacyCslExtension.Checked = bool.Parse(_settingsProvider.GetSetting("UseLegacyCslExtension") ?? "true");

            var certLocationStr = _settingsProvider.GetSetting("CertificateLocation");
            if (Enum.TryParse<Core.Models.StoreLocation>(certLocationStr, out var certLocation))
            {
                cbCertLocation.SelectedItem = certLocation;
            }
            else
            {
                cbCertLocation.SelectedItem = Core.Models.StoreLocation.CurrentUser;
            }

            var lineEndingModeStr = _settingsProvider.GetSetting("LineEndingMode");
            var lineEndingMode = LineEndingMode.LeaveAsIs;
            if (int.TryParse(lineEndingModeStr, out var lineEndingInt) &&
                Enum.IsDefined(typeof(LineEndingMode), lineEndingInt))
            {
                lineEndingMode = (LineEndingMode)lineEndingInt;
            }

            foreach (var radioButton in lineEndingRadioButtons)
            {
                var tag = radioButton.Tag;
                if (tag != null)
                {
                    radioButton.Checked = (LineEndingMode)tag == lineEndingMode;
                }
            }
        }

        /// <summary>
        /// Test out the settings before saving them.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnOk_Click(object sender, System.EventArgs e)
        {
            Cursor? lastCursor = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
            _isSaving = true;
            btnOk.Enabled = false;
            btnCancel.Enabled = false;
            tabControl.Enabled = false;

            try
            {
                var cluster = txtKustoCluster.Text;
                var database = txtKustoDatabase.Text;
                var authority = txtAuthority.Text;
                var currentCluster = _settingsProvider.GetSetting("TempCluster") ?? string.Empty;
                var currentDatabase = _settingsProvider.GetSetting("TempDatabase") ?? string.Empty;
                var currentAuthority = _settingsProvider.GetSetting("AADAuthority") ?? string.Empty;

                if (currentCluster != cluster || currentDatabase != database ||
                    (!string.IsNullOrWhiteSpace(cluster) && !string.IsNullOrWhiteSpace(database) &&
                     currentAuthority != authority))
                {
                    // Validate Kusto settings - this will throw exceptions on validation failure
                    // and return the normalized cluster name
                    cluster = await _kustoValidationService.ValidateKustoSettingsAsync(cluster, database, authority);

                    // Check if database is empty and get confirmation if not
                    try
                    {
                        await _kustoValidationService.CheckDatabaseEmptyAsync(
                            cluster, database, authority);
                    }
                    catch (KustoDatabaseValidationException)
                    {
                        // Database is not empty - ask user for confirmation
                        var wipeDialogResult = MessageBox.Show(
                            this,
                            $"WARNING! There are existing functions and tables in the {database} database" +
                            $" on the {cluster} cluster. If you proceed, everything will be dropped from that database every time a comparison " +
                            $"is run. Do you wish to DROP EVERYTHING in the '{database}' database?",
                            "Non-Empty Database",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning);

                        if (wipeDialogResult != DialogResult.Yes)
                        {
                            return;
                        }

                        // Note that we don't actually need to clean the database here. We've gotten
                        // permission to do so and it will happen automatically as needed during
                        // schema comparison operations.
                    }
                }

                _settingsProvider.SetSetting("TempCluster", cluster);
                _settingsProvider.SetSetting("TempDatabase", database);
                _settingsProvider.SetSetting("TableFieldsOnNewLine", cbTableFieldsOnNewLine.Checked.ToString());
                _settingsProvider.SetSetting("CreateMergeEnabled", cbCreateMerge.Checked.ToString());
                _settingsProvider.SetSetting("KustoObjectDropWarning", chkTableDropWarning.Checked.ToString());
                if (currentAuthority != authority)
                {
                    _settingsProvider.SetSetting("AADAuthority", authority);
                }
                _settingsProvider.SetSetting("UseLegacyCslExtension", cbUseLegacyCslExtension.Checked.ToString());

                var checkedButton = lineEndingRadioButtons.FirstOrDefault(b => b.Checked);
                if (checkedButton?.Tag != null)
                {
                    _settingsProvider.SetSetting("LineEndingMode", ((int)(LineEndingMode)checkedButton.Tag).ToString());
                }

                _settingsProvider.SetSetting("CertificateLocation", cbCertLocation.SelectedItem!.ToString()!);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Core.Exceptions.KustoSettingsException ex)
            {
                MessageBox.Show(ex.Message, "Missing Info", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                if (KustoErrorDetails.Format(ex) is { } details)
                {
                    using var dialog = new ErrorDetailsForm("Couldn't validate Kusto settings.", details);
                    dialog.ShowDialog(this);
                }
                else
                {
                    MessageBox.Show(this, $"Unknown error: {ex.Message}", "Error Validating Settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                _isSaving = false;
                if (!IsDisposed)
                {
                    btnOk.Enabled = true;
                    btnCancel.Enabled = true;
                    tabControl.Enabled = true;
                }
                Cursor.Current = lastCursor;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_isSaving && e.CloseReason == CloseReason.UserClosing && DialogResult != DialogResult.OK)
            {
                e.Cancel = true;
            }

            base.OnFormClosing(e);
        }

        /// <summary>
        /// Close the form without saving anything
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
