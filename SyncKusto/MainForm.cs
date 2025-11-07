// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using DiffPlex;
using DiffPlex.DiffBuilder;
using Kusto.Data.Common;
using SyncKusto.ChangeModel;
using SyncKusto.Core.Abstractions;
using SyncKusto.Core.Configuration;
using SyncKusto.Core.Models;
using SyncKusto.ErrorHandling;
using SyncKusto.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SyncKusto.Abstractions;
using SyncKusto.Adapters;

namespace SyncKusto
{
    public partial class MainForm : Form
    {
        private DatabaseSchema? _sourceSchema;
        private DatabaseSchema? _targetSchema;
        private ComparisonResult? _lastComparison;

        private readonly string _functionTreeNodeText = "Functions";
        private readonly string _tablesTreeNodeText = "Tables";
        private readonly IErrorMessageResolver _errorMessageResolver;
        private readonly IMainFormPresenter _presenter;
        private readonly ISettingsProvider _settingsProvider;
        private readonly SyncKustoSettings _settings;
        private readonly ISchemaValidationService _validationService;
        private readonly IKustoValidationService _kustoValidationService;
        private readonly SchemaSourceSelectorAdapter _sourceAdapter;
        private readonly SchemaSourceSelectorAdapter _targetAdapter;

        /// <summary>
        /// Default constructor for designer support
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
            
            _errorMessageResolver = ErrorMessageResolverFactory.CreateDefault();
            
            // Create temporary instances - will be replaced by DI constructor
            _presenter = null!; // Set by DI
            _settingsProvider = null!; // Set by DI
            _settings = null!; // Set by DI
            _validationService = null!; // Set by DI
            _kustoValidationService = null!; // Set by DI
            _sourceAdapter = new SchemaSourceSelectorAdapter(spcSource);
            _targetAdapter = new SchemaSourceSelectorAdapter(spcTarget);
        }

        /// <summary>
        /// Constructor with dependency injection
        /// </summary>
        public MainForm(
            IMainFormPresenter presenter,
            IErrorMessageResolver errorMessageResolver,
            ISettingsProvider settingsProvider,
            SyncKustoSettings settings,
            ISchemaValidationService validationService,
            IKustoValidationService kustoValidationService) : this()
        {
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _errorMessageResolver = errorMessageResolver ?? throw new ArgumentNullException(nameof(errorMessageResolver));
            _settingsProvider = settingsProvider ?? throw new ArgumentNullException(nameof(settingsProvider));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _kustoValidationService = kustoValidationService ?? throw new ArgumentNullException(nameof(kustoValidationService));
            
            // Initialize the picker controls with settings provider
            InitializePickerControls();
            
            // Wire up reset event handlers
            spcSource.ResetMainFormValueHolders = ResetValueHoldersOnChange;
            spcTarget.ResetMainFormValueHolders = ResetValueHoldersOnChange;
        }

        /// <summary>
        /// Initialize picker controls with settings provider
        /// </summary>
        private void InitializePickerControls()
        {
            // Initialize picker controls created by the designer with the settings provider
            spcSource.Initialize(_settingsProvider);
            spcTarget.Initialize(_settingsProvider);
        }

        /// <summary>
        /// Clear out the results when there is a change
        /// </summary>
        private void ResetValueHoldersOnChange()
        {
            tvComparison.Nodes.Clear();
            rtbSourceText.Clear();
            _lastComparison = null;
        }

        /// <summary>
        /// Compare the source schema to the target schema
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnCompare_Click(object sender, EventArgs e)
        {
            rtbSourceText.Clear();
            tvComparison.Nodes.Clear();

            spcSource.ReportProgress($@"Validating...");

            // Validate UI inputs
            var sourceValidation = _sourceAdapter.Validate();
            if (!sourceValidation.IsValid)
            {
                MessageBox.Show(@"Source schema is not correctly specified.", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                spcSource.ReportProgress(string.Empty);
                return;
            }
            
            var targetValidation = _targetAdapter.Validate();
            if (!targetValidation.IsValid)
            {
                MessageBox.Show(@"Target schema is not correctly specified.", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                spcSource.ReportProgress(string.Empty);
                return;
            }

            // Get source info from adapters
            var sourceInfo = _sourceAdapter.GetSourceInfo();
            var targetInfo = _targetAdapter.GetSourceInfo();

            // Validate settings
            var settingsValidation = _presenter.ValidateSettings(sourceInfo, targetInfo);
            if (!settingsValidation.IsValid)
            {
                MessageBox.Show(settingsValidation.ErrorMessage, @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                spcSource.ReportProgress(string.Empty);
                return;
            }

            // Compare with progress reporting
            var previousCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;

            try
            {
                var progress = new Progress<SyncProgress>(p =>
                {
                    // Update both source and target progress displays
                    if (p.Stage == SyncProgressStage.LoadingSourceSchema || 
                        p.Stage == SyncProgressStage.ComparingSchemas)
                    {
                        spcSource.ReportProgress(p.Message);
                    }
                    
                    if (p.Stage == SyncProgressStage.LoadingTargetSchema || 
                        p.Stage == SyncProgressStage.ComparingSchemas)
                    {
                        spcTarget.ReportProgress(p.Message);
                    }
                });

                _lastComparison = await _presenter.CompareAsync(sourceInfo, targetInfo, progress);
                
                // Cache schemas for diff view
                _sourceSchema = _lastComparison.SourceSchema;
                _targetSchema = _lastComparison.TargetSchema;

                // Populate tree with differences
                PopulateTree(_lastComparison.Differences.AllDifferences, tvComparison);

                spcSource.ReportProgress(string.Empty);
                spcTarget.ReportProgress(string.Empty);

                // Save and reload recent values
                _sourceAdapter.SaveRecentValues();
                _targetAdapter.SaveRecentValues();
                _sourceAdapter.ReloadRecentValues();
                _targetAdapter.ReloadRecentValues();

                // Enable the update button now that a comparison has been generated.
                btnUpdate.Enabled = true;
            }
            catch (Exception ex)
            {
                var errorMessage = _errorMessageResolver.ResolveErrorMessage(ex);
                MessageBox.Show($@"Failed to compare schemas: {errorMessage}", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                spcSource.ReportProgress(string.Empty);
                spcTarget.ReportProgress(string.Empty);
            }
            finally
            {
                this.Cursor = previousCursor;
            }
        }

        /// <summary>
        /// Given a set of Kusto difference objects, put them into the function or table tree nodes depending on the type of the object
        /// </summary>
        /// <param name="differences">A list of differences to display</param>
        /// <param name="tv">The tree view control to populate with the differences</param>
        private void PopulateTree(IEnumerable<SchemaDifference> differences, TreeView tv)
        {
            TreeNode functionRootNode = tv.Nodes.Add(_functionTreeNodeText);
            TreeNode functionAddNode = functionRootNode.Nodes.Add("Not In Target");
            TreeNode functionDropNode = functionRootNode.Nodes.Add("Only In Target");
            TreeNode functionEditNode = functionRootNode.Nodes.Add("Different");

            TreeNode ToTreeNode(SchemaDifference difference)
            {
                return new TreeNode()
                {
                    Text = difference.Name,
                    Tag = difference
                };
            }

            var schemaDifferences = differences.ToList();

            foreach (FunctionSchemaDifference schemaDifference in schemaDifferences.OfType<FunctionSchemaDifference>().OrderBy(f => f.Name))
            {
                switch (schemaDifference)
                {
                    case FunctionSchemaDifference drop
                        when drop.Difference is OnlyInTarget:
                        functionDropNode.Nodes.Add(ToTreeNode(drop));
                        break;

                    case FunctionSchemaDifference add
                        when add.Difference is OnlyInSource:
                        functionAddNode.Nodes.Add(ToTreeNode(add));
                        break;

                    case FunctionSchemaDifference change
                        when change.Difference is Modified:
                        functionEditNode.Nodes.Add(ToTreeNode(change));
                        break;
                }
            }

            TreeNode tableRootNode = tv.Nodes.Add(_tablesTreeNodeText);
            TreeNode tableAddNode = tableRootNode.Nodes.Add("Not In Target");
            TreeNode tableDropNode = tableRootNode.Nodes.Add("Only In Target");
            TreeNode tableEditNode = tableRootNode.Nodes.Add("Different");

            foreach (TableSchemaDifference schemaDifference in schemaDifferences.OfType<TableSchemaDifference>().OrderBy(f => f.Name))
            {
                switch (schemaDifference)
                {
                    case TableSchemaDifference drop
                        when drop.Difference is OnlyInTarget:
                        tableDropNode.Nodes.Add(ToTreeNode(drop));
                        break;

                    case TableSchemaDifference add
                        when add.Difference is OnlyInSource:
                        tableAddNode.Nodes.Add(ToTreeNode(add));
                        break;

                    case TableSchemaDifference change
                        when change.Difference is Modified:
                        tableEditNode.Nodes.Add(ToTreeNode(change));
                        break;
                }
            }

            ClearEmptyNodes(functionDropNode, functionAddNode, functionEditNode, tableDropNode, tableAddNode, tableEditNode);
        }

        /// <summary>
        /// Go through all the specified nodes and remove any that don't have children
        /// </summary>
        /// <param name="nodes">An array of nodes to inspect</param>
        private void ClearEmptyNodes(params TreeNode[] nodes)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Nodes.Count == 0)
                {
                    node.Remove();
                }
            }
        }

        /// <summary>
        /// Create a comparison for the node that was clicked on
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tvComparison_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (_sourceSchema == null || _targetSchema == null)
            {
                return;
            }

            string objectName = e.Node.Text;
            string sourceText = "";
            string targetText = "";

            if (_sourceSchema.Functions.ContainsKey(objectName) && e.Node.FullPath.StartsWith(_functionTreeNodeText))
            {
                sourceText = CslCommandGenerator.GenerateCreateOrAlterFunctionCommand(_sourceSchema.Functions[objectName], true);
            }

            if (_sourceSchema.Tables.ContainsKey(objectName) && e.Node.FullPath.StartsWith(_tablesTreeNodeText))
            {
                sourceText = SyncKusto.Kusto.Services.FormattedCslCommandGenerator.GenerateTableCreateCommand(
                    _sourceSchema.Tables[objectName], 
                    true,
                    _settings.CreateMergeEnabled,
                    _settings.TableFieldsOnNewLine,
                    _settings.LineEndingMode);
            }

            if (_targetSchema.Functions.ContainsKey(objectName) && e.Node.FullPath.StartsWith(_functionTreeNodeText))
            {
                targetText = CslCommandGenerator.GenerateCreateOrAlterFunctionCommand(_targetSchema.Functions[objectName], true);
            }

            if (_targetSchema.Tables.ContainsKey(objectName) && e.Node.FullPath.StartsWith(_tablesTreeNodeText))
            {
                targetText = SyncKusto.Kusto.Services.FormattedCslCommandGenerator.GenerateTableCreateCommand(
                    _targetSchema.Tables[objectName], 
                    true,
                    _settings.CreateMergeEnabled,
                    _settings.TableFieldsOnNewLine,
                    _settings.LineEndingMode);
            }

            var diffBuilder = new InlineDiffBuilder(new Differ());
            DiffPlex.DiffBuilder.Model.DiffPaneModel diff = diffBuilder.BuildDiffModel(targetText, sourceText);
            rtbSourceText.Clear();

            int longestLine = 98;
            if (diff.Lines.Any())
            {
                longestLine = Math.Max(diff.Lines.Max(l => l.Text.Length), longestLine);
            }

            foreach (DiffPlex.DiffBuilder.Model.DiffPiece line in diff.Lines)
            {
                switch (line.Type)
                {
                    case DiffPlex.DiffBuilder.Model.ChangeType.Inserted:
                        rtbSourceText.SelectionBackColor = System.Drawing.Color.Yellow;
                        rtbSourceText.SelectedText = line.Text.PadRight(longestLine);
                        break;

                    case DiffPlex.DiffBuilder.Model.ChangeType.Deleted:
                        rtbSourceText.SelectionBackColor = System.Drawing.Color.Red;
                        rtbSourceText.SelectedText = line.Text.PadRight(longestLine);
                        break;

                    case DiffPlex.DiffBuilder.Model.ChangeType.Imaginary:
                        break;

                    default:
                        rtbSourceText.SelectionBackColor = System.Drawing.Color.White;
                        rtbSourceText.SelectedText = line.Text.PadRight(longestLine);
                        break;
                }

                rtbSourceText.SelectedText += "\n";
            }
        }

        /// <summary>
        /// Update the target to match the source
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnUpdate_Click(object sender, EventArgs e)
        {
            if (_lastComparison == null)
            {
                MessageBox.Show(@"No comparison available. Please compare schemas first.", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var previousCursor = this.Cursor;
            this.Cursor = Cursors.WaitCursor;

            rtbSourceText.Text = "";

            // Figure out which differences were selected in the tree control
            var selectedNodes = new List<TreeNode>();
            foreach (TreeNode n in tvComparison.Nodes)
            {
                selectedNodes.AddRange(GetCheckedNodes(n));
            }

            if (!selectedNodes.Any())
            {
                MessageBox.Show(@"No differences were selected. Nothing to update in the target.", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Cursor = previousCursor;
                return;
            }

            // Check for drop operations and show warning if needed
            var targetInfo = _targetAdapter.GetSourceInfo();
            if (_settings.KustoObjectDropWarning && 
                targetInfo.SourceType == SourceSelection.Kusto() &&
                selectedNodes.Any(n => n.Parent.Text == "Only In Target"))
            {
                var dialogResult = new DropWarningForm(_settingsProvider).ShowDialog();
                if (dialogResult != DialogResult.Yes)
                {
                    MessageBox.Show("Operation has been canceled", "Canceled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Cursor = previousCursor;
                    return;
                }
            }

            try
            {
                // Get selected differences
                var selectedDifferences = selectedNodes
                    .Select(node => (SchemaDifference)node.Tag);

                // Synchronize with progress reporting
                var progress = new Progress<SyncProgress>(p =>
                {
                    spcSource.ReportProgress(p.Message);
                    spcTarget.ReportProgress(p.Message);
                });

                var result = await _presenter.SynchronizeAsync(selectedDifferences, progress);

                if (!result.Success)
                {
                    var errorMessage = string.Join(Environment.NewLine, result.Errors);
                    MessageBox.Show($@"Synchronization completed with errors:{Environment.NewLine}{errorMessage}", 
                        @"Partial Success", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    MessageBox.Show(@"Target update is complete.", @"Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                // Clear the comparison
                tvComparison.Nodes.Clear();
                btnUpdate.Enabled = false;
                _lastComparison = null;
                
                spcSource.ReportProgress(string.Empty);
                spcTarget.ReportProgress(string.Empty);
            }
            catch (Exception ex)
            {
                var errorMessage = _errorMessageResolver.ResolveErrorMessage(ex);
                MessageBox.Show($@"Failed to synchronize schemas: {errorMessage}", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                spcSource.ReportProgress(string.Empty);
                spcTarget.ReportProgress(string.Empty);
            }
            finally
            {
                this.Cursor = previousCursor;
            }
        }

        /// <summary>
        /// When a node is checked or unchecked, make the whole subtree match
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tvComparison_AfterCheck(object sender, TreeViewEventArgs e)
        {
            // If the user caused the check action then update the subtree
            if (e.Action != TreeViewAction.Unknown)
            {
                CheckAllChildNodes(e.Node, e.Node?.Checked ?? false);
            }
        }

        /// <summary>
        /// Make all the children of this node have the specified checked state
        /// </summary>
        /// <param name="node">All children of this node will have their checked state modified</param>
        /// <param name="checkedState">The new state for the check box</param>
        private void CheckAllChildNodes(TreeNode? node, bool checkedState)
        {
            if (node == null)
            {
                return;
            }

            foreach (TreeNode n in node.Nodes)
            {
                n.Checked = checkedState;
                CheckAllChildNodes(n, checkedState);
            }
        }

        /// <summary>
        /// Get all of the checked nodes that are children of this node
        /// </summary>
        /// <param name="node">The node to inspect</param>
        /// <returns>All children of "node" that are checked and leaf nodes</returns>
        private IEnumerable<TreeNode> GetCheckedNodes(TreeNode node)
        {
            var result = new List<TreeNode>();
            foreach (TreeNode n in node.Nodes)
            {
                if (n.Checked && n.Nodes.Count == 0)
                {
                    result.Add(n);
                }
                result.AddRange(GetCheckedNodes(n));
            }

            return result;
        }

        /// <summary>
        /// Display the settings form as a dialog
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSettings_Click(object sender, EventArgs e)
        {
            var frm = new SettingsForm(_settingsProvider, _kustoValidationService);
            frm.ShowDialog();
        }
    }
}
