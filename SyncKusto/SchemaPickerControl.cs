// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Kusto.Data;
using Kusto.Data.Common;
using SyncKusto.Core.Abstractions;
using SyncKusto.Core.Exceptions;
using SyncKusto.Core.Models;
using SyncKusto.FileSystem.Extensions;
using SyncKusto.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Windows.Forms;
using CoreStoreLocation = SyncKusto.Core.Models.StoreLocation;
using X509StoreLocation = System.Security.Cryptography.X509Certificates.StoreLocation;

namespace SyncKusto
{
    public partial class SchemaPickerControl : UserControl
    {
        private const string ENTRA_ID_USER = "Microsoft Entra ID User";
        private const string ENTRA_ID_APP_KEY = "Microsoft Entra ID App (Key)";
        private const string ENTRA_ID_APP_SNI = "Microsoft Entra ID App (SubjectName/Issuer)";

        private ISettingsProvider? _settingsProvider;

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
            SourceSelectionMap =
                sourceSelectionFactory.Choose(ToggleFilePathSourcePanel, ToggleKustoSourcePanel);

            SourceValidationMap =
                sourceSelectionFactory.Validate(FilePathSourceSpecification, KustoSourceSpecification);

            SourceAllowedMap =
                sourceSelectionFactory.Allowed(AllowedFilePathSource, AllowedKustoSource);

            SetDefaultControlView();
        }

        /// <summary>
        /// Initialize the control with a settings provider
        /// </summary>
        /// <param name="settingsProvider">Settings provider for configuration</param>
        public void Initialize(ISettingsProvider settingsProvider)
        {
            _settingsProvider = settingsProvider ?? throw new ArgumentNullException(nameof(settingsProvider));
            
            txtFilePath.Text = _settingsProvider.GetSetting("PreviousFilePath") ?? string.Empty;
            
            // Load recent values
            this.cbCluster.Items.Clear();
            this.cbCluster.Items.AddRange(_settingsProvider.GetRecentValues("RecentClusters").ToArray());
            
            this.cbDatabase.Items.Clear();
            this.cbDatabase.Items.AddRange(_settingsProvider.GetRecentValues("RecentDatabases").ToArray());
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
                if (_settingsProvider == null)
                    throw new InvalidOperationException("SettingsProvider not initialized. Call Initialize() first.");

                var factory = new SyncKusto.Kusto.Services.KustoConnectionFactory();
                
                var certificateLocation = CoreStoreLocation.CurrentUser;
                var certLocationStr = _settingsProvider.GetSetting("CertificateLocation");
                if (Enum.TryParse<CoreStoreLocation>(certLocationStr, out var parsedLocation))
                {
                    certificateLocation = parsedLocation;
                }

                var aadAuthority = _settingsProvider.GetSetting("AADAuthority") ?? string.Empty;
                
                var options = Authentication switch
                {
                    AuthenticationMode.AadFederated => new SyncKusto.Core.Abstractions.KustoConnectionOptions(
                        Cluster: cbCluster.Text,
                        Database: cbDatabase.Text,
                        AuthMode: Authentication,
                        Authority: aadAuthority,
                        AppId: null,
                        AppKey: null,
                        CertificateThumbprint: null,
                        CertificateLocation: certificateLocation),
                        
                    AuthenticationMode.AadApplication => new SyncKusto.Core.Abstractions.KustoConnectionOptions(
                        Cluster: cbCluster.Text,
                        Database: cbDatabase.Text,
                        AuthMode: Authentication,
                        Authority: aadAuthority,
                        AppId: cbAppId.Text,
                        AppKey: txtAppKey.Text,
                        CertificateThumbprint: null,
                        CertificateLocation: certificateLocation),
                        
                    AuthenticationMode.AadApplicationSni => new SyncKusto.Core.Abstractions.KustoConnectionOptions(
                        Cluster: cbCluster.Text,
                        Database: cbDatabase.Text,
                        AuthMode: Authentication,
                        Authority: aadAuthority,
                        AppId: cbAppIdSni.Text,
                        AppKey: null,
                        CertificateThumbprint: txtCertificate.Text,
                        CertificateLocation: certificateLocation),
                        
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
            if (_settingsProvider == null)
                throw new InvalidOperationException("SettingsProvider not initialized. Call Initialize() first.");

            var certificateLocation = CoreStoreLocation.CurrentUser;
            var certLocationStr = _settingsProvider.GetSetting("CertificateLocation");
            if (Enum.TryParse<CoreStoreLocation>(certLocationStr, out var parsedLocation))
            {
                certificateLocation = parsedLocation;
            }

            // Show the certificate selection dialog
            var selectedCertificateCollection = X509Certificate2UI.SelectFromCollection(
                CertificateStore.GetAllCertificates(certificateLocation),
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
        /// For some of the inputs, we save the most recently used values that were used. This method
        /// updates the storage behind all those settings to include the most recently used values.
        /// </summary>
        public void SaveRecentValues()
        {
            if (_settingsProvider == null) return;

            _settingsProvider.AddRecentValue("RecentClusters", this.cbCluster.Text);
            _settingsProvider.AddRecentValue("RecentDatabases", this.cbDatabase.Text);
            _settingsProvider.AddRecentValue("RecentAppIds", this.cbAppId.Text);
            _settingsProvider.AddRecentValue("RecentAppIds", this.cbAppIdSni.Text);
        }

        /// <summary>
        /// For the inputs where we store recents, reload them all with the latest values.
        /// </summary>
        public void ReloadRecentValues()
        {
            if (_settingsProvider == null) return;

            this.cbCluster.Items.Clear();
            this.cbCluster.Items.AddRange(_settingsProvider.GetRecentValues("RecentClusters").ToArray());
            this.cbDatabase.Items.Clear();
            this.cbDatabase.Items.AddRange(_settingsProvider.GetRecentValues("RecentDatabases").ToArray());
            this.cbAppId.Items.Clear();
            this.cbAppId.Items.AddRange(_settingsProvider.GetRecentValues("RecentAppIds").ToArray());
            this.cbAppIdSni.Items.Clear();
            this.cbAppIdSni.Items.AddRange(_settingsProvider.GetRecentValues("RecentAppIds").ToArray());
        }
    }
}