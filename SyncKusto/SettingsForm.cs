// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;
using SyncKusto.Kusto;
using System;
using System.Collections.Generic;
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
        }

        /// <summary>
        /// Populate the existing settings into the text boxes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClusterSelectionForm_Load(object sender, EventArgs e)
        {
            txtKustoCluster.Text = SettingsWrapper.KustoClusterForTempDatabases;
            txtKustoDatabase.Text = SettingsWrapper.TemporaryKustoDatabase;
            txtAuthority.Text = SettingsWrapper.AADAuthority;
            chkTableDropWarning.Checked = SettingsWrapper.KustoObjectDropWarning;
            cbTableFieldsOnNewLine.Checked = SettingsWrapper.TableFieldsOnNewLine ?? false;
            cbCreateMerge.Checked = SettingsWrapper.CreateMergeEnabled ?? false;
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

        /// <summary>
        /// Test out the settings before saving them.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnOk_Click(object sender, EventArgs e)
        {
            Cursor lastCursor = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;

            SettingsWrapper.TableFieldsOnNewLine = cbTableFieldsOnNewLine.Checked;
            SettingsWrapper.CreateMergeEnabled = cbCreateMerge.Checked;
            SettingsWrapper.KustoObjectDropWarning = chkTableDropWarning.Checked;
            SettingsWrapper.AADAuthority = txtAuthority.Text;

            // Only check the Kusto settings if they changed
            if (SettingsWrapper.KustoClusterForTempDatabases != txtKustoCluster.Text || SettingsWrapper.TemporaryKustoDatabase != txtKustoDatabase.Text)
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
                        MessageBox.Show($"Could not authenticate with AAD. Please verify that the AAD Authority is specified correctly.", "Error Authenticating", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                        MessageBox.Show($"Drop all functions and tables in the {txtKustoDatabase.Text} database before specifying this as the temporary database. " +
                            $"This check is performed to reinforce the point that this databse will be wiped every time a comparison is run.", 
                            "Error Validating Empty Database", 
                            MessageBoxButtons.OK, 
                            MessageBoxIcon.Error);
                        return;
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
    }
}
