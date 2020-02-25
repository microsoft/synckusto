// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Kusto.Data;
using Kusto.Data.Common;
using SyncKusto.Functional;
using SyncKusto.Kusto;
using SyncKusto.Kusto.DatabaseSchemaBuilder;
using SyncKusto.SyncSources;
using SyncKusto.Validation.ErrorMessages;
using SyncKusto.Validation.Infrastructure;

namespace SyncKusto
{
    public partial class SchemaPickerControl : UserControl
    {
        /// <summary>
        ///     Default constructor to make the Windows Forms designer happy
        /// </summary>
        public SchemaPickerControl()
        {
            InitializeComponent();
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

        private AuthenticationMode Authentication =>
            rbFederated.Checked ? AuthenticationMode.AadFederated : AuthenticationMode.AadApplication;

        public string SourceFilePath => txtFilePath.Text;

        public KustoConnectionStringBuilder KustoConnection
        {
            get
            {
                return QueryEngine.GetKustoConnectionStringBuilder(txtCluster.Text, txtDatabase.Text, txtAppId.Text, txtAppKey.Text);
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
                .And(Spec<SchemaPickerControl>.NonEmptyString(s => s.txtCluster.Text))
                .And(Spec<SchemaPickerControl>.NonEmptyString(s => s.txtDatabase.Text))
                .And(
                    Spec<SchemaPickerControl>.IsTrue(s => s.Authentication == AuthenticationMode.AadFederated)
                        .Or(Spec<SchemaPickerControl>
                            .IsTrue(s => s.Authentication == AuthenticationMode.AadApplication)
                            .And(Spec<SchemaPickerControl>.NonEmptyString(s => s.txtAppId.Text)
                                .And(Spec<SchemaPickerControl>.NonEmptyString(s => s.txtAppKey.Text)))))
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

        private void rbApplication_CheckedChanged(object sender, EventArgs e) =>
            pnlApplicationAuthentication.Visible = ((RadioButton) sender).Checked;

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

            ToggleSourceSelections();}

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
                    schemaBuilder = new FileDatabaseSchemaBuilder(SourceFilePath);
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
    }
}