// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using DiffPlex;
using DiffPlex.DiffBuilder;
using Kusto.Data.Common;
using SyncKusto.ChangeModel;
using SyncKusto.Extensions;
using SyncKusto.Functional;
using SyncKusto.Kusto;
using SyncKusto.Validation.ErrorMessages;
using SyncKusto.Validation.ErrorMessages.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SyncKusto.SyncSources;

namespace SyncKusto
{
    public partial class MainForm : Form
    {
        private DatabaseSchema _sourceSchema;
        private DatabaseSchema _targetSchema;

        private readonly string _functionTreeNodeText = "Functions";
        private readonly string _tablesTreeNodeText = "Tables";

        /// <summary>
        /// Default constructor. Get the UI set up properly.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
            spcSource.ResetMainFormValueHolders = ResetValueHoldersOnChange;
            spcTarget.ResetMainFormValueHolders = ResetValueHoldersOnChange;
        }

        /// <summary>
        /// Clear out the results when there is a change
        /// </summary>
        private void ResetValueHoldersOnChange()
        {
            tvComparison.Nodes.Clear();
            rtbSourceText.Clear();
        }

        /// <summary>
        /// Compare the source schema to the target schema
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCompare_Click(object sender, EventArgs e)
        {
            rtbSourceText.Clear();
            tvComparison.Nodes.Clear();

            spcSource.ReportProgress($@"Validating...");

            if (!spcSource.IsValid())
            {
                MessageBox.Show(@"Source schema is not correctly specified.", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                spcSource.ReportProgress(string.Empty);
                return;
            }
            if (!spcTarget.IsValid())
            {
                MessageBox.Show(@"Target schema is not correctly specified.", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                spcSource.ReportProgress(string.Empty);
                return;
            }

            if (!ValidateSettings())
            {
                return;
            }

            // Load both of the schemas
            Cursor lastCursor = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;

            IDatabaseSchema TryGetSchema(Func<Either<IOperationError, DatabaseSchema>> schema)
            {
                try
                {
                    return schema()
                        .Map(valid => (IDatabaseSchema)new ValidDatabaseSchema(() => valid))
                        .Reduce(
                            schemaError => new InvalidDatabaseSchema(schemaError));
                }
                catch (Exception exception)
                {
                    return new InvalidDatabaseSchema(new NonSpecificOperationError(exception));
                }
            }

            Func<DefaultOperationErrorMessageResolver> errorMessageResolver = DefaultOperationErrorMessageResolver
                .Using(() => new List<IOperationErrorMessageSpecification>()
                {
                    KustoOperationErrorSpecifications.ClusterNotFound(),
                    KustoOperationErrorSpecifications.DatabaseNotFound(),
                    KustoOperationErrorSpecifications.NoPermissions(),
                    KustoOperationErrorSpecifications.CannotAuthenticate(),
                    FilePathOperationErrorSpecifications.FolderNotFound()
                });

            spcSource.ReportProgress($@"Loading source schema...");
            IDatabaseSchema sourceSchema = TryGetSchema(() => spcSource.TryLoadSchema());

            spcSource.ReportProgress($@"Schema loaded.");
            spcTarget.ReportProgress($@"Loading target schema...");
            IDatabaseSchema targetSchema = sourceSchema is InvalidDatabaseSchema _
                ? new InvalidDatabaseSchema(new NonSpecificOperationError(
                    new InvalidOperationException("Target schema not loaded due to Source schema error.")))
                : TryGetSchema(() => spcTarget.TryLoadSchema());

            switch (sourceSchema)
            {
                case InvalidDatabaseSchema invalidSource:
                    MessageBox.Show($@"The Source schema is invalid: {errorMessageResolver().ResolveFor(invalidSource.Error).Get()}", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;

                case ValidDatabaseSchema _ when targetSchema is InvalidDatabaseSchema invalidTarget:
                    MessageBox.Show($@"The Target schema is invalid: {errorMessageResolver().ResolveFor(invalidTarget.Error).Get()}", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;

                case ValidDatabaseSchema source when targetSchema is ValidDatabaseSchema target:
                    spcTarget.ReportProgress($@"Schema loaded.");
                    _sourceSchema = source.Schema;
                    _targetSchema = target.Schema;
                    break;
            }

            Cursor.Current = lastCursor;

            spcSource.ReportProgress($@"Comparing differences...");
            IEnumerable<SchemaDifference> tableDifferences = new KustoSchemaDifferenceMapper(() =>
                    _sourceSchema.Tables.AsKustoSchema().DifferenceFrom(_targetSchema.Tables.AsKustoSchema()))
                .GetDifferences();

            IEnumerable<SchemaDifference> functionDifferences = new KustoSchemaDifferenceMapper(() =>
                    _sourceSchema.Functions.AsKustoSchema().DifferenceFrom(_targetSchema.Functions.AsKustoSchema()))
                .GetDifferences();

            // Add to the tree view control
            PopulateTree(tableDifferences.Concat(functionDifferences), tvComparison);

            spcSource.ReportProgress(string.Empty);
            spcTarget.ReportProgress(string.Empty);

            // Save the cluster and databases used in recent history to populate the combo boxes for
            // next time and then reload them both. (Note that combining save an reload into a
            // single operation would mean that the source recent history list wouldn't contain
            // whatever was just used in the target schema so we keep them as separate steps.)
            spcSource.SaveRecentValues();
            spcTarget.SaveRecentValues();
            spcSource.ReloadRecentValues();
            spcTarget.ReloadRecentValues();

            // Enable the update button now that a comparison has been generated.
            btnUpdate.Enabled = true;
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
            string objectName = e.Node.Text;
            string sourceText = "";
            string targetText = "";

            if (_sourceSchema.Functions.ContainsKey(objectName) && e.Node.FullPath.StartsWith(_functionTreeNodeText))
            {
                sourceText = CslCommandGenerator.GenerateCreateOrAlterFunctionCommand(_sourceSchema.Functions[objectName], true);
            }

            if (_sourceSchema.Tables.ContainsKey(objectName) && e.Node.FullPath.StartsWith(_tablesTreeNodeText))
            {
                sourceText = FormattedCslCommandGenerator.GenerateTableCreateCommand(_sourceSchema.Tables[objectName], true);
            }

            if (_targetSchema.Functions.ContainsKey(objectName) && e.Node.FullPath.StartsWith(_functionTreeNodeText))
            {
                targetText = CslCommandGenerator.GenerateCreateOrAlterFunctionCommand(_targetSchema.Functions[objectName], true);
            }

            if (_targetSchema.Tables.ContainsKey(objectName) && e.Node.FullPath.StartsWith(_tablesTreeNodeText))
            {
                targetText = FormattedCslCommandGenerator.GenerateTableCreateCommand(_targetSchema.Tables[objectName], true);
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
        private void btnUpdate_Click(object sender, EventArgs e)
        {
            Cursor lastCursor = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;

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
                return;
            }

            // Save the changes either to disk or to Kusto
            if (spcTarget.SourceSelection == SourceSelection.FilePath())
            {
                PersistChanges(selectedNodes);
            }
            else
            {
                if (SettingsWrapper.KustoObjectDropWarning && selectedNodes.Any(n => n.Parent.Text == "Only In Target"))
                {
                    var dialogResult = new DropWarningForm().ShowDialog();
                    if (dialogResult != DialogResult.Yes)
                    {
                        MessageBox.Show("Operation has been canceled", "Canceled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                }

                using (var kustoQueryEngine = new QueryEngine(spcTarget.KustoConnection))
                {
                    PersistChanges(selectedNodes, kustoQueryEngine);
                }
            }

            tvComparison.Nodes.Clear();
            btnUpdate.Enabled = false;
            Cursor.Current = lastCursor;

            MessageBox.Show(@"Target update is complete.", @"Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Save the changes denoted by the selected nodes.
        /// </summary>
        /// <param name="selectedNodes">The changes to persist</param>
        /// <param name="kustoQueryEngine">Pass a connection to Kusto if the target is Kusto</param>
        private void PersistChanges(IEnumerable<TreeNode> selectedNodes, QueryEngine kustoQueryEngine = null)
        {
            void WriteToTarget(IKustoSchema schema)
            {
                if (spcTarget.SourceSelection == SourceSelection.FilePath())
                {
                    schema.WriteToFile(spcTarget.SourceFilePath, SettingsWrapper.FileExtension);
                }
                else
                {
                    schema.WriteToKusto(kustoQueryEngine);
                }
            }

            void DeleteFromTarget(IKustoSchema schema)
            {
                if (spcTarget.SourceSelection == SourceSelection.Kusto())
                {
                    schema.DeleteFromKusto(kustoQueryEngine);
                }
                else
                {
                    schema.DeleteFromFolder(spcTarget.SourceFilePath, SettingsWrapper.FileExtension);
                }
            }

            foreach (SchemaDifference difference in selectedNodes.Select(node => (SchemaDifference)node.Tag))
            {
                switch (difference.Schema)
                {
                    case IKustoSchema update when
                        difference.Difference is Modified ||
                        difference.Difference is OnlyInSource:
                        WriteToTarget(update);
                        break;

                    case IKustoSchema delete when
                        difference.Difference is OnlyInTarget:
                        DeleteFromTarget(delete);
                        break;

                    default:
                        throw new InvalidOperationException("Unhandled type supplied.");
                }
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
                CheckAllChildNodes(e.Node, e.Node.Checked);
            }
        }

        /// <summary>
        /// Make all the children of this node have the specified checked state
        /// </summary>
        /// <param name="node">All children of this node will have their checked state modified</param>
        /// <param name="checkedState">The new state for the check box</param>
        private void CheckAllChildNodes(TreeNode node, bool checkedState)
        {
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
            var frm = new SettingsForm();
            frm.ShowDialog();
        }

        /// <summary>
        /// Validate that the user has specified the required settings
        /// </summary>
        /// <returns>True if everything is set up properly, false otherwise</returns>
        private bool ValidateSettings()
        {
            // Using the local file system for either the source or the target requires access to a cluster where we can make a temporary database
            if ((spcSource.SourceSelection == SourceSelection.FilePath() || spcTarget.SourceSelection == SourceSelection.FilePath()) &&
                !IsTempClusterDefined())
            {
                MessageBox.Show("Cannot compare without cluster setting.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return true;
        }

        /// <summary>
        /// If the temporary cluster isn't defined yet, show the dialog.
        /// </summary>
        /// <returns>True if the cluster is set, false otherwise.</returns>
        private bool IsTempClusterDefined()
        {
            if (string.IsNullOrWhiteSpace(SettingsWrapper.KustoClusterForTempDatabases))
            {
                var frm = new SettingsForm();
                frm.ShowDialog();
            }

            return !string.IsNullOrWhiteSpace(SettingsWrapper.KustoClusterForTempDatabases);
        }
    }
}