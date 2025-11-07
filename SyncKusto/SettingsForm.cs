// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using SyncKusto.Core.Abstractions;
using SyncKusto.Core.Models;
using SyncKusto.Kusto.Exceptions;
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

            try
            {
                // Save non-Kusto settings immediately
                _settingsProvider.SetSetting("TableFieldsOnNewLine", cbTableFieldsOnNewLine.Checked.ToString());
                _settingsProvider.SetSetting("CreateMergeEnabled", cbCreateMerge.Checked.ToString());
                _settingsProvider.SetSetting("KustoObjectDropWarning", chkTableDropWarning.Checked.ToString());
                _settingsProvider.SetSetting("AADAuthority", txtAuthority.Text);
                _settingsProvider.SetSetting("UseLegacyCslExtension", cbUseLegacyCslExtension.Checked.ToString());
                
                var checkedButton = lineEndingRadioButtons.Where(b => b.Checked).FirstOrDefault();
                if (checkedButton?.Tag != null)
                {
                    _settingsProvider.SetSetting("LineEndingMode", ((int)(LineEndingMode)checkedButton.Tag).ToString());
                }
                
                _settingsProvider.SetSetting("CertificateLocation", cbCertLocation.SelectedItem!.ToString()!);

                var currentCluster = _settingsProvider.GetSetting("TempCluster") ?? string.Empty;
                var currentDatabase = _settingsProvider.GetSetting("TempDatabase") ?? string.Empty;

                // Only check the Kusto settings if they changed
                if (currentCluster != txtKustoCluster.Text || currentDatabase != txtKustoDatabase.Text)
                {
                    // Validate Kusto settings - this will throw exceptions on validation failure
                    // and return the normalized cluster name
                    txtKustoCluster.Text = await _kustoValidationService.ValidateKustoSettingsAsync(
                        txtKustoCluster.Text, 
                        txtKustoDatabase.Text, 
                        txtAuthority.Text);

                    // Check if database is empty and get confirmation if not
                    try
                    {
                        await _kustoValidationService.CheckDatabaseEmptyAsync(
                            txtKustoCluster.Text,
                            txtKustoDatabase.Text,
                            txtAuthority.Text);
                    }
                    catch (KustoDatabaseValidationException)
                    {
                        // Database is not empty - ask user for confirmation
                        var wipeDialogResult = MessageBox.Show(
                            $"WARNING! There are existing functions and tables in the {txtKustoDatabase.Text} database" +
                            $" on the {txtKustoCluster.Text} cluster. If you proceed, everything will be dropped from that database every time a comparison " +
                            $"is run. Do you wish to DROP EVERYTHING in the '{txtKustoDatabase.Text}' database?",
                            "Non-Empty Database",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning);
                        
                        if (wipeDialogResult != DialogResult.Yes)
                        {
                            Cursor.Current = lastCursor;
                            return;
                        }

                        // Note that we don't actually need to clean the database here. We've gotten
                        // permission to do so and it will happen automatically as needed during
                        // schema comparison operations.
                    }

                    // Store the settings now that we know they work
                    _settingsProvider.SetSetting("TempCluster", txtKustoCluster.Text);
                    _settingsProvider.SetSetting("TempDatabase", txtKustoDatabase.Text);
                }

                this.Close();
            }
            catch (Core.Exceptions.KustoSettingsException ex)
            {
                MessageBox.Show(ex.Message, "Missing Info", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (KustoPermissionException ex)
            {
                MessageBox.Show(ex.Message, "Error Validating Permissions", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (KustoClusterException ex)
            {
                MessageBox.Show(ex.Message, "Error Validating Cluster", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (KustoAuthenticationException ex)
            {
                MessageBox.Show(ex.Message, "Error Authenticating", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unknown error: {ex.Message}", "Error Validating Settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor.Current = lastCursor;
            }
        }

        /// <summary>
        /// Close the form without saving anything
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
