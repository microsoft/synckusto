// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using SyncKusto.Core.Abstractions;
using SyncKusto.Core.Models;
using SyncKusto.FileSystem.Extensions;
using SyncKusto.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;
using CoreStoreLocation = SyncKusto.Core.Models.StoreLocation;

namespace SyncKusto
{
    public partial class SchemaPickerControl : UserControl
    {
        private const string ENTRA_ID_USER = "Microsoft Entra ID User";
        private const string ENTRA_ID_APP_KEY = "Microsoft Entra ID App (Key)";
        private const string ENTRA_ID_APP_SNI = "Microsoft Entra ID App (SubjectName/Issuer)";

        private ISettingsProvider? _settingsProvider;

        /// <summary>
        /// Default constructor to make the Windows Forms designer happy
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

            SourceSelection = SourceSelection.FilePath();
            ProgressMessageState = new Stack<(string, SourceSelection)>();
        }

        /// <summary>
        /// Constructor that defaults to type of source
        /// </summary>
        public SchemaPickerControl(ISourceSelectionFactory sourceSelectionFactory) : this()
        {
            SourceSelectionMap = sourceSelectionFactory.Choose(ToggleFilePathSourcePanel, ToggleKustoSourcePanel);
            SourceValidationMap = sourceSelectionFactory.Validate(FilePathSourceSpecification, KustoSourceSpecification);
            SourceAllowedMap = sourceSelectionFactory.Allowed(AllowedFilePathSource, AllowedKustoSource);
            SetDefaultControlView();
        }

        /// <summary>
        /// Initialize the control with a settings provider
        /// </summary>
        public void Initialize(ISettingsProvider settingsProvider)
        {
            _settingsProvider = settingsProvider ?? throw new ArgumentNullException(nameof(settingsProvider));
            
            txtFilePath.Text = _settingsProvider.GetSetting("PreviousFilePath") ?? string.Empty;
            
            this.cbCluster.Items.Clear();
            this.cbCluster.Items.AddRange(_settingsProvider.GetRecentValues("RecentClusters").ToArray());
            
            this.cbDatabase.Items.Clear();
            this.cbDatabase.Items.AddRange(_settingsProvider.GetRecentValues("RecentDatabases").ToArray());
        }

        private IReadOnlyDictionary<SourceSelection, (bool enabled, Action<bool> whenAllowed)>? SourceAllowedMap { get; }
        private IReadOnlyDictionary<SourceSelection, Action<bool>>? SourceSelectionMap { get; }
        private IReadOnlyDictionary<SourceSelection, Func<bool>>? SourceValidationMap { get; }

        public SourceSelection SourceSelection { get; private set; }
        private Stack<(string message, SourceSelection source)> ProgressMessageState { get; }
        public Action? ResetMainFormValueHolders { private get; set; }

        public string Title
        {
            get => grpSourceSchema.Text;
            set => grpSourceSchema.Text = value;
        }

        /// <summary>
        /// Get schema source information from the control
        /// </summary>
        public SchemaSourceInfo GetSourceInfo()
        {
            if (SourceSelection == SourceSelection.FilePath())
            {
                return new SchemaSourceInfo(
                    SourceSelection.FilePath(),
                    FilePath: FileSystemSchemaExtensions.HandleLongFileNames(txtFilePath.Text));
            }
            else if (SourceSelection == SourceSelection.Kusto())
            {
                var certificateLocation = GetCertificateLocation();
                var authority = _settingsProvider?.GetSetting("AADAuthority") ?? string.Empty;
                
                var kustoInfo = new KustoConnectionInfo(
                    Cluster: cbCluster.Text,
                    Database: cbDatabase.Text,
                    AuthMode: GetAuthenticationMode(),
                    Authority: authority,
                    AppId: GetAppId(),
                    AppKey: GetAppKey(),
                    CertificateThumbprint: GetCertificateThumbprint(),
                    CertificateLocation: certificateLocation);
                
                return new SchemaSourceInfo(SourceSelection.Kusto(), KustoInfo: kustoInfo);
            }
            
            throw new InvalidOperationException($"Unknown source selection type: {SourceSelection}");
        }

        /// <summary>
        /// Check if the user has correctly specified a schema source
        /// </summary>
        public bool IsValid()
        {
            if (SourceValidationMap == null) return false;
            return SourceValidationMap[SourceSelection].Invoke();
        }

        /// <summary>
        /// Update the UI with the status message
        /// </summary>
        public void ReportProgress(string? message)
        {
            if (message != null)
                txtOperationProgress.Text = message;
        }

        /// <summary>
        /// Save recently used values
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
        /// Reload recently used values
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

        // UI Event Handlers
        private void cmbAuthentication_SelectedValueChanged(object sender, EventArgs e)
        {
            pnlApplicationAuthentication.Visible = GetAuthenticationMode() == AuthenticationMode.AadApplication;
            pnlApplicationSniAuthentication.Visible = GetAuthenticationMode() == AuthenticationMode.AadApplicationSni;
        }

        private void rbKusto_CheckedChanged(object sender, EventArgs e) =>
            SourceButtonCheckChange(sender, SourceSelection.Kusto());

        private void rbFilePath_CheckedChanged(object sender, EventArgs e) =>
            SourceButtonCheckChange(sender, SourceSelection.FilePath());

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

        private void btnCertificate_Click(object sender, EventArgs e)
        {
            var certificateLocation = GetCertificateLocation();
            var selectedCertificateCollection = X509Certificate2UI.SelectFromCollection(
                CertificateStore.GetAllCertificates(certificateLocation),
                "Select a certificate",
                "Choose a certificate for authentication",
                X509SelectionFlag.SingleSelection);

            if (selectedCertificateCollection != null && selectedCertificateCollection.Count == 1)
            {
                txtCertificate.Text = selectedCertificateCollection[0].Thumbprint;
            }
        }

        // Helper Methods
        private void SetDefaultControlView()
        {
            EnableSourceSelections();
            SourceSelection = SourceSelection.FilePath();
            ToggleSourceSelections();
        }

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

        private void AllowedKustoSource(bool predicate) => rbKusto.Enabled = predicate;
        private void AllowedFilePathSource(bool predicate) => rbFilePath.Enabled = predicate;
        private void ToggleFilePathSourcePanel(bool predicate) => pnlFilePath.Visible = predicate;
        private void ToggleKustoSourcePanel(bool predicate) => pnlKusto.Visible = predicate;

        private bool FilePathSourceSpecification() =>
            SourceSelection == SourceSelection.FilePath() && 
            !string.IsNullOrWhiteSpace(txtFilePath.Text);

        private bool KustoSourceSpecification()
        {
            if (SourceSelection != SourceSelection.Kusto())
                return false;
                
            if (string.IsNullOrWhiteSpace(cbCluster.Text) || string.IsNullOrWhiteSpace(cbDatabase.Text))
                return false;
                
            return GetAuthenticationMode() switch
            {
                AuthenticationMode.AadFederated => true,
                AuthenticationMode.AadApplication => 
                    !string.IsNullOrWhiteSpace(cbAppId.Text) && !string.IsNullOrWhiteSpace(txtAppKey.Text),
                AuthenticationMode.AadApplicationSni => 
                    !string.IsNullOrWhiteSpace(cbAppIdSni.Text) && !string.IsNullOrWhiteSpace(txtCertificate.Text),
                _ => false
            };
        }

        private AuthenticationMode GetAuthenticationMode() => cmbAuthentication.SelectedItem switch
        {
            ENTRA_ID_USER => AuthenticationMode.AadFederated,
            ENTRA_ID_APP_KEY => AuthenticationMode.AadApplication,
            ENTRA_ID_APP_SNI => AuthenticationMode.AadApplicationSni,
            _ => throw new Exception("Unknown authentication type")
        };

        private string? GetAppId() => GetAuthenticationMode() switch
        {
            AuthenticationMode.AadApplication => cbAppId.Text,
            AuthenticationMode.AadApplicationSni => cbAppIdSni.Text,
            _ => null
        };

        private string? GetAppKey() => GetAuthenticationMode() == AuthenticationMode.AadApplication 
            ? txtAppKey.Text 
            : null;

        private string? GetCertificateThumbprint() => GetAuthenticationMode() == AuthenticationMode.AadApplicationSni 
            ? txtCertificate.Text 
            : null;

        private CoreStoreLocation GetCertificateLocation()
        {
            var certLocationStr = _settingsProvider?.GetSetting("CertificateLocation");
            if (Enum.TryParse<CoreStoreLocation>(certLocationStr, out var parsedLocation))
            {
                return parsedLocation;
            }
            return CoreStoreLocation.CurrentUser;
        }
    }
}