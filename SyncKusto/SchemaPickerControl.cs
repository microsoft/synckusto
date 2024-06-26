﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Kusto.Data;
using Kusto.Data.Common;
using SyncKusto.Functional;
using SyncKusto.Kusto;
using SyncKusto.Kusto.DatabaseSchemaBuilder;
using SyncKusto.SyncSources;
using SyncKusto.Utilities;
using SyncKusto.Validation.ErrorMessages;
using SyncKusto.Validation.Infrastructure;
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

            ProgressMessageState = new Stack<(string, SourceSelection)>();
            SetDefaultControlView();
        }

        private IReadOnlyDictionary<SourceSelection, (bool enabled, Action<bool> whenAllowed)> SourceAllowedMap { get; }

        private IReadOnlyDictionary<SourceSelection, Action<bool>> SourceSelectionMap { get; }

        private IReadOnlyDictionary<SourceSelection, Func<bool>> SourceValidationMap { get; }

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

        public string SourceFilePath => txtFilePath.Text.HandleLongFileNames();

        public KustoConnectionStringBuilder KustoConnection
        {
            get
            {
                switch (Authentication)
                {
                    case AuthenticationMode.AadFederated:
                        return QueryEngine.GetKustoConnectionStringBuilder(cbCluster.Text, cbDatabase.Text);

                    case AuthenticationMode.AadApplication:
                        return QueryEngine.GetKustoConnectionStringBuilder(
                            cbCluster.Text,
                            cbDatabase.Text,
                            aadClientId: cbAppId.Text,
                            aadClientKey: txtAppKey.Text);

                    case AuthenticationMode.AadApplicationSni:
                        return QueryEngine.GetKustoConnectionStringBuilder(
                            cbCluster.Text,
                            cbDatabase.Text,
                            aadClientId: cbAppIdSni.Text,
                            certificateThumbprint: txtCertificate.Text);

                    default:
                        throw new Exception("Unknown authentication type");
                }
            }
        }

        private Stack<(string message, SourceSelection source)> ProgressMessageState { get; }

        private Func<string, bool> IsConfigured => (input) => Spec<string>.NonEmptyString(s => s).IsSatisfiedBy(input);

        public Action ResetMainFormValueHolders { private get; set; }

        private static Func<ComboBox, bool> WhenSelectedItemAndEmptyValues
            =>
                (box) => Spec<ComboBox>.IsTrue(c => c.Items.Count == 0)
                    .And(Spec<ComboBox>.NonEmptyString(c => c.Text))
                    .IsSatisfiedBy(box);

        private void SetDefaultControlView()
        {
            EnableSourceSelections();
            SourceSelection = SourceSelection.FilePath();
            ToggleSourceSelections();
        }

        private void AllowedKustoSource(bool predicate) => rbKusto.Enabled = predicate;

        private void AllowedFilePathSource(bool predicate) => rbFilePath.Enabled = predicate;

        private bool FilePathSourceSpecification() =>
            Spec<SchemaPickerControl>
                .IsTrue(s => s.SourceSelection == SourceSelection.FilePath())
                .And(Spec<SchemaPickerControl>.NonEmptyString(s => s.SourceFilePath))
                .IsSatisfiedBy(this);

        private bool KustoSourceSpecification() =>
            Spec<SchemaPickerControl>
                .IsTrue(s => s.SourceSelection == SourceSelection.Kusto())
                .And(Spec<SchemaPickerControl>.NonEmptyString(s => s.cbCluster.Text))
                .And(Spec<SchemaPickerControl>.NonEmptyString(s => s.cbDatabase.Text))
                .And(
                    Spec<SchemaPickerControl>.IsTrue(s => s.Authentication == AuthenticationMode.AadFederated)
                        .Or(Spec<SchemaPickerControl>
                            .IsTrue(s => s.Authentication == AuthenticationMode.AadApplication)
                            .And(Spec<SchemaPickerControl>.NonEmptyString(s => s.cbAppId.Text)
                                .And(Spec<SchemaPickerControl>.NonEmptyString(s => s.txtAppKey.Text))))
                        .Or(Spec<SchemaPickerControl>
                            .IsTrue(s => s.Authentication == AuthenticationMode.AadApplicationSni)
                            .And(Spec<SchemaPickerControl>.NonEmptyString(s => s.cbAppIdSni.Text)
                                .And(Spec<SchemaPickerControl>.NonEmptyString(s => s.txtCertificate.Text)))))
                .IsSatisfiedBy(this);

        private void ToggleFilePathSourcePanel(bool predicate) => pnlFilePath.Visible = predicate;

        private void ToggleKustoSourcePanel(bool predicate) => pnlKusto.Visible = predicate;

        private void EnableSourceSelections()
        {
            foreach ((bool enabled, Action<bool> whenAllowed) value in SourceAllowedMap.Values)
            {
                value.whenAllowed.Invoke(value.enabled);
            }
        }

        private void ToggleSourceSelections()
        {
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
                if (Spec<string>.NonEmptyString(s => s).IsSatisfiedBy(txtOperationProgress.Text))
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
        public bool IsValid() => SourceValidationMap[SourceSelection].Invoke();

        /// <summary>
        /// Attempt to load the schema specified in the control
        /// </summary>
        /// <returns>Either an error or the schema that was loaded</returns>
        public Either<IOperationError, DatabaseSchema> TryLoadSchema()
        {
            IDatabaseSchemaBuilder schemaBuilder = EmptyDatabaseSchemaBuilder.Value;

            try
            {
                if (SourceSelection == SourceSelection.Kusto())
                {
                    schemaBuilder = new KustoDatabaseSchemaBuilder(new QueryEngine(KustoConnection));
                }

                if (SourceSelection == SourceSelection.FilePath())
                {
                    SettingsWrapper.PreviousFilePath = SourceFilePath;
                    schemaBuilder = new FileDatabaseSchemaBuilder(SourceFilePath, SettingsWrapper.FileExtension);
                }

                switch (schemaBuilder)
                {
                    case KustoDatabaseSchemaBuilder _:
                    case FileDatabaseSchemaBuilder _:
                        ReportProgress($@"Constructing schema...");
                        return Task.Run(async () =>
                            await schemaBuilder.Build().ConfigureAwait(false)).Result;

                    default:
                        return new DatabaseSchemaOperationError(new InvalidOperationException("An unknown type was supplied."));
                }
            }
            catch (Exception e)
            {
                return new DatabaseSchemaOperationError(e);
            }
        }

        /// <summary>
        /// Update the UI with the status message
        /// </summary>
        /// <param name="message">The message to display</param>
        public void ReportProgress(string message)
        {
            if (Spec<string>.NotNull(s => s).IsSatisfiedBy(message))
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