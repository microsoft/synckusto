// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Kusto.Data;
using Kusto.Data.Common;
using SyncKusto.Core.Exceptions;
using SyncKusto.Core.Models;
using SyncKusto.FileSystem.Extensions;
using SyncKusto.Kusto.DatabaseSchemaBuilder;
using SyncKusto.SyncSources;
using SyncKusto.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SyncKusto
{
    public partial class SchemaPickerControl : UserControl
    {
        private const string ENTRA_ID_USER = "Microsoft Entra ID User";
        private const string ENTRA_ID_APP_KEY = "Microsoft Entra ID App (Key)";
        private const string ENTRA_ID_APP_SNI = "Microsoft Entra ID App (SubjectName/Issuer)";

        /// <summary>
        ///     Default constructor to make the Windows Forms designer happy
        /// </summary>
        public SchemaPickerControl()
        {
            InitializeComponent();

            this.cmbAuthentication.Items.AddRange(
                new object[] {
                    ENTRA_ID_USER,
                    ENTRA_ID_APP_KEY,
                    ENTRA_ID_APP_SNI });

            this.cmbAuthentication.SelectedIndex = 0;

            this.cbCluster.Items.AddRange(SettingsWrapper.RecentClusters.ToArray());
            this.cbDatabase.Items.AddRange(SettingsWrapper.RecentDatabases.ToArray());
            
            // Initialize required properties to prevent nullable warnings
            SourceSelection = SourceSelection.FilePath();
            ProgressMessageState = new Stack<(string, SourceSelection)>();
        }

        /// <summary>
        /// Constructor that defaults to type of source
        /// </summary>
        /// <param name="sourceSelectionFactory">Factory for picking a source</param>
        public SchemaPickerControl(ISourceSelectionFactory sourceSelectionFactory) : this()
        {
            txtFilePath.Text = SettingsWrapper.PreviousFilePath;

            SourceSelectionMap =
                sourceSelectionFactory.Choose(ToggleFilePathSourcePanel, ToggleKustoSourcePanel);

            SourceValidationMap =
                sourceSelectionFactory.Validate(FilePathSourceSpecification, KustoSourceSpecification);

            SourceAllowedMap =
                sourceSelectionFactory.Allowed(AllowedFilePathSource, AllowedKustoSource);

            SetDefaultControlView();
        }

        private IReadOnlyDictionary<SourceSelection, (bool enabled, Action<bool> whenAllowed)>? SourceAllowedMap { get; }

        private IReadOnlyDictionary<SourceSelection, Action<bool>>? SourceSelectionMap { get; }

        private IReadOnlyDictionary<SourceSelection, Func<bool>>? SourceValidationMap { get; }

        public SourceSelection SourceSelection { get; private set; }

        public string Title
        {
            get => grpSourceSchema.Text;
            set => grpSourceSchema.Text = value;
        }

        private AuthenticationMode Authentication
        {
            get
            {
                switch (cmbAuthentication.SelectedItem)
                {
                    case ENTRA_ID_USER:
                        return AuthenticationMode.AadFederated;

                    case ENTRA_ID_APP_KEY:
                        return AuthenticationMode.AadApplication;

                    case ENTRA_ID_APP_SNI:
                        return AuthenticationMode.AadApplicationSni;

                    default:
                        throw new Exception("Unknown authentication type");
                }
            }
        }

        public string SourceFilePath => FileSystemSchemaExtensions.HandleLongFileNames(txtFilePath.Text);

        public KustoConnectionStringBuilder KustoConnection
        {
            get
            {
                var factory = new SyncKusto.Kusto.Services.KustoConnectionFactory();
                
                var options = Authentication switch
                {
                    AuthenticationMode.AadFederated => new SyncKusto.Core.Abstractions.KustoConnectionOptions(
                        Cluster: cbCluster.Text,
                        Database: cbDatabase.Text,
                        AuthMode: Authentication,
                        Authority: SettingsWrapper.AADAuthority,
                        AppId: null,
                        AppKey: null,
                        CertificateThumbprint: null,
                        CertificateLocation: SettingsWrapper.CertificateLocation),
                        
                    AuthenticationMode.AadApplication => new SyncKusto.Core.Abstractions.KustoConnectionOptions(
                        Cluster: cbCluster.Text,
                        Database: cbDatabase.Text,
                        AuthMode: Authentication,
                        Authority: SettingsWrapper.AADAuthority,
                        AppId: cbAppId.Text,
                        AppKey: txtAppKey.Text,
                        CertificateThumbprint: null,
                        CertificateLocation: SettingsWrapper.CertificateLocation),
                        
                    AuthenticationMode.AadApplicationSni => new SyncKusto.Core.Abstractions.KustoConnectionOptions(
                        Cluster: cbCluster.Text,
                        Database: cbDatabase.Text,
                        AuthMode: Authentication,
                        Authority: SettingsWrapper.AADAuthority,
                        AppId: cbAppIdSni.Text,
                        AppKey: null,
                        CertificateThumbprint: txtCertificate.Text,
                        CertificateLocation: SettingsWrapper.CertificateLocation),
                        
                    _ => throw new Exception("Unknown authentication type")
                };

                return (KustoConnectionStringBuilder)factory.CreateConnectionString(options);
            }
        }

        private Stack<(string message, SourceSelection source) > ProgressMessageState { get; }

        private Func<string, bool> IsConfigured => (input) => !string.IsNullOrWhiteSpace(input);

        public Action? ResetMainFormValueHolders { private get; set; }

        private static Func<ComboBox, bool> WhenSelectedItemAndEmptyValues
            =>
                (box) => box.Items.Count == 0 && !string.IsNullOrWhiteSpace(box.Text);

        private void SetDefaultControlView()
        {
            EnableSourceSelections();
            SourceSelection = SourceSelection.FilePath();
            ToggleSourceSelections();
        }

        private void AllowedKustoSource(bool predicate) => rbKusto.Enabled = predicate;

        private void AllowedFilePathSource(bool predicate) => rbFilePath.Enabled = predicate;

        private bool FilePathSourceSpecification() =>
            SourceSelection == SourceSelection.FilePath() && 
            !string.IsNullOrWhiteSpace(SourceFilePath);

        private bool KustoSourceSpecification()
        {
            if (SourceSelection != SourceSelection.Kusto())
                return false;
                
            if (string.IsNullOrWhiteSpace(cbCluster.Text) || string.IsNullOrWhiteSpace(cbDatabase.Text))
                return false;
                
            return Authentication switch
            {
                AuthenticationMode.AadFederated => true,
                AuthenticationMode.AadApplication => 
                    !string.IsNullOrWhiteSpace(cbAppId.Text) && !string.IsNullOrWhiteSpace(txtAppKey.Text),
                AuthenticationMode.AadApplicationSni => 
                    !string.IsNullOrWhiteSpace(cbAppIdSni.Text) && !string.IsNullOrWhiteSpace(txtCertificate.Text),
                _ => false
            };
        }

        private void ToggleFilePathSourcePanel(bool predicate) => pnlFilePath.Visible = predicate;

        private void ToggleKustoSourcePanel(bool predicate) => pnlKusto.Visible = predicate;

        private void EnableSourceSelections()
        {
            if (SourceAllowedMap == null) return;
            
            foreach ((bool enabled, Action<bool> whenAllowed) value in SourceAllowedMap.Values)
            {
                value.whenAllowed.Invoke(value.enabled);
            }
        }

        private void ToggleSourceSelections()
        {
            if (SourceSelectionMap == null) return;
            
            foreach (SourceSelection source in SourceSelectionMap.Keys)
            {
                SourceSelectionMap[source].Invoke(source == SourceSelection);
            }
        }

        private void cmbAuthentication_SelectedValueChanged(object sender, EventArgs e)
        {
            pnlApplicationAuthentication.Visible = Authentication == AuthenticationMode.AadApplication;
            pnlApplicationSniAuthentication.Visible = Authentication == AuthenticationMode.AadApplicationSni;
        }

        private void rbKusto_CheckedChanged(object sender, EventArgs e) =>
            SourceButtonCheckChange(sender, SourceSelection.Kusto());

        private void rbFilePath_CheckedChanged(object sender, EventArgs e) =>
            SourceButtonCheckChange(sender, SourceSelection.FilePath());

        private void SourceButtonCheckChange(object sender, SourceSelection source)
        {
            if (((RadioButton)sender).Checked)
            {
                if (!string.IsNullOrWhiteSpace(txtOperationProgress.Text))
                {
                    ProgressMessageState.Push((txtOperationProgress.Text, SourceSelection));
                }

                SourceSelection = source;
                (string message, SourceSelection _) = ProgressMessageState.FirstOrDefault(x => x.source == SourceSelection);
                ReportProgress(message ?? string.Empty);
            }

            ToggleSourceSelections();
        }

        private void btnChooseDirectory_Click(object sender, EventArgs e)
        {
            var fbd = new FolderBrowserDialog
            {
                ShowNewFolderButton = false,
                Description = "Select the directory that contains the full schema for the Kusto database.",
                SelectedPath = txtFilePath.Text
            };

            if (fbd.ShowDialog() == DialogResult.OK)
            {
                txtFilePath.Text = fbd.SelectedPath;
            }
        }

        /// <summary>
        ///     Check if the user has correctly specified a schema source
        /// </summary>
        /// <returns>True if it is correctly set, false otherwise.</returns>
        public bool IsValid()
        {
            if (SourceValidationMap == null) return false;
            return SourceValidationMap[SourceSelection].Invoke();
        }

        /// <summary>
        /// Load the schema specified in the control
        /// </summary>
        /// <returns>The loaded database schema</returns>
        /// <exception cref="SchemaLoadException">Thrown when schema cannot be loaded</exception>
        public DatabaseSchema LoadSchema()
        {
            try
            {
                IDatabaseSchemaBuilder schemaBuilder;

                if (SourceSelection == SourceSelection.Kusto())
                {
                    schemaBuilder = new KustoDatabaseSchemaBuilder(new SyncKusto.Kusto.Services.QueryEngine(KustoConnection, SettingsWrapper.LineEndingMode));
                }
                else if (SourceSelection == SourceSelection.FilePath())
                {
                    SettingsWrapper.PreviousFilePath = SourceFilePath;
                    // Use the deprecated FileDatabaseSchemaBuilder temporarily until the refactoring is complete
                    #pragma warning disable CS0618 // Type or member is obsolete
                    schemaBuilder = new FileDatabaseSchemaBuilder(SourceFilePath, SettingsWrapper.FileExtension);
                    #pragma warning restore CS0618 // Type or member is obsolete
                }
                else
                {
                    throw new InvalidOperationException("An unknown source type was supplied.");
                }

                ReportProgress($@"Constructing schema...");
                return Task.Run(async () => await schemaBuilder.Build().ConfigureAwait(false)).Result;
            }
            catch (Exception ex) when (ex is not SchemaLoadException)
            {
                throw new SchemaLoadException("Failed to load database schema", ex);
            }
        }

        /// <summary>
        /// Update the UI with the status message
        /// </summary>
        /// <param name="message">The message to display</param>
        public void ReportProgress(string? message)
        {
            if (message != null)
                txtOperationProgress.Text = message;
        }

        private void btnCertificate_Click(object sender, EventArgs e)
        {
            // Show the certificate selection dialog
            var selectedCertificateCollection = X509Certificate2UI.SelectFromCollection(
                CertificateStore.GetAllCertificates(SettingsWrapper.CertificateLocation),
                "Select a certificate",
                "Choose a certificate for authentication",
                X509SelectionFlag.SingleSelection);

            if (selectedCertificateCollection != null &&
                selectedCertificateCollection.Count == 1)
            {
                txtCertificate.Text = selectedCertificateCollection[0].Thumbprint;
            }
        }

        /// <summary>
        /// For some of the inputs, we save the most recent values that were used. This method
        /// updates the storage behind all those settings to include the most recently used values.
        /// </summary>
        public void SaveRecentValues()
        {
            SettingsWrapper.AddRecentCluster(this.cbCluster.Text);
            SettingsWrapper.AddRecentDatabase(this.cbDatabase.Text);
            SettingsWrapper.AddRecentAppId(this.cbAppId.Text);
            SettingsWrapper.AddRecentAppId(this.cbAppIdSni.Text);
        }

        /// <summary>
        /// For the inputs where we store recents, reload them all with the latest values.
        /// </summary>
        public void ReloadRecentValues()
        {
            this.cbCluster.Items.Clear();
            this.cbCluster.Items.AddRange(SettingsWrapper.RecentClusters.ToArray());
            this.cbDatabase.Items.Clear();
            this.cbDatabase.Items.AddRange(SettingsWrapper.RecentDatabases.ToArray());
            this.cbAppId.Items.Clear();
            this.cbAppId.Items.AddRange(SettingsWrapper.RecentAppIds.ToArray());
            this.cbAppIdSni.Items.Clear();
            this.cbAppIdSni.Items.AddRange(SettingsWrapper.RecentAppIds.ToArray());
        }
    }
}