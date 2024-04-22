// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;
using SyncKusto.Kusto;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;

namespace SyncKusto
{
    /// <summary>
    /// Collect some settings from the user
    /// </summary>
    public partial class SettingsForm : Form
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public SettingsForm()
        {
            InitializeComponent();

            cbCertLocation.Items.AddRange(Enum.GetNames(typeof(StoreLocation)));
        }

        /// <summary>
        /// Populate the existing settings into the text boxes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SettingsForm_Load(object sender, EventArgs e)
        {
            txtKustoCluster.Text = SettingsWrapper.KustoClusterForTempDatabases;
            txtKustoDatabase.Text = SettingsWrapper.TemporaryKustoDatabase;
            txtAuthority.Text = SettingsWrapper.AADAuthority;
            chkTableDropWarning.Checked = SettingsWrapper.KustoObjectDropWarning;
            cbTableFieldsOnNewLine.Checked = SettingsWrapper.TableFieldsOnNewLine ?? false;
            cbCreateMerge.Checked = SettingsWrapper.CreateMergeEnabled ?? false;
            cbUseLegacyCslExtension.Checked = SettingsWrapper.UseLegacyCslExtension ?? false;
            cbCertLocation.SelectedItem = SettingsWrapper.CertificateLocation;
        }

        /// <summary>
        /// Test out the settings before saving them.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnOk_Click(object sender, System.EventArgs e)
        {
            Cursor lastCursor = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;

            SettingsWrapper.TableFieldsOnNewLine = cbTableFieldsOnNewLine.Checked;
            SettingsWrapper.CreateMergeEnabled = cbCreateMerge.Checked;
            SettingsWrapper.KustoObjectDropWarning = chkTableDropWarning.Checked;
            SettingsWrapper.AADAuthority = txtAuthority.Text;
            SettingsWrapper.UseLegacyCslExtension = cbUseLegacyCslExtension.Checked;
            SettingsWrapper.CertificateLocation = cbCertLocation.SelectedItem.ToString();

            // Only check the Kusto settings if they changed
            if (SettingsWrapper.KustoClusterForTempDatabases != txtKustoCluster.Text ||
                SettingsWrapper.TemporaryKustoDatabase != txtKustoDatabase.Text)
            {
                // Allow for multiple ways of specifying a cluster name
                if (string.IsNullOrEmpty(txtKustoCluster.Text))
                {
                    MessageBox.Show($"No Kusto cluster was specified.", "Missing Info", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                string clusterName = QueryEngine.NormalizeClusterName(txtKustoCluster.Text);

                string databaseName = txtKustoDatabase.Text;
                if (string.IsNullOrEmpty(databaseName))
                {
                    MessageBox.Show($"No Kusto database was specified.", "Missing Info", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // If the required info is present, update the cluster textbox with the modified cluster url
                txtKustoCluster.Text = clusterName;

                // Verify connection and permissions by creating and removing a function
                var connString = new KustoConnectionStringBuilder(clusterName)
                {
                    FederatedSecurity = true,
                    InitialCatalog = databaseName,
                    Authority = txtAuthority.Text
                };
                var adminClient = KustoClientFactory.CreateCslAdminProvider(connString);

                try
                {
                    string functionName = "SyncKustoPermissionsTest" + Guid.NewGuid();
                    adminClient.ExecuteControlCommand(
                        CslCommandGenerator.GenerateCreateOrAlterFunctionCommand(
                            functionName,
                            "",
                            "",
                            new Dictionary<string, string>(),
                            "{print now()}"));
                    adminClient.ExecuteControlCommand(CslCommandGenerator.GenerateFunctionDropCommand(functionName));
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("403-Forbidden"))
                    {
                        MessageBox.Show($"The current user does not have permission to create a function on cluster('{clusterName}').database('{databaseName}')", "Error Validating Permissions", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else if (ex.Message.Contains("failed to resolve the service name"))
                    {
                        MessageBox.Show($"Cluster {clusterName} could not be found.", "Error Validating Permissions", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else if (ex.Message.Contains("Kusto client failed to perform authentication"))
                    {
                        MessageBox.Show($"Could not authenticate with Microsoft Entra ID. Please verify that the Microsoft Entra ID Authority is specified correctly.", "Error Authenticating", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        MessageBox.Show($"Unknown error: {ex.Message}", "Error Validating Permissions", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    return;
                }

                // Verify that the scratch database is empty
                try
                {
                    long functionCount = 0;
                    long tableCount = 0;

                    using (var functionReader = adminClient.ExecuteControlCommand(".show functions | count"))
                    {
                        functionReader.Read();
                        functionCount = functionReader.GetInt64(0);
                    }

                    using (var tableReader = adminClient.ExecuteControlCommand(".show tables | count"))
                    {
                        tableReader.Read();
                        tableCount = tableReader.GetInt64(0);
                    }

                    if (functionCount != 0 || tableCount != 0)
                    {
                        var wipeDialogResult = MessageBox.Show($"WARNING! There are existing functions and tables in the {txtKustoDatabase.Text} database" +
                            $" on the {txtKustoCluster.Text} cluster. If you proceed, everything will be dropped from that database every time a comparison " +
                            $"is run. Do you wish to DROP EVERYTHING in the '{txtKustoDatabase.Text}' database?",
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
                catch (Exception ex)
                {
                    MessageBox.Show($"Unknown error: {ex.Message}", "Error Validating Empty Database", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                // Store the settings now that we know they work
                SettingsWrapper.KustoClusterForTempDatabases = clusterName;
                SettingsWrapper.TemporaryKustoDatabase = databaseName;
            }

            this.Close();

            Cursor.Current = lastCursor;
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