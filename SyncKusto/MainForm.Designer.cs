// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace SyncKusto
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.btnCompare = new System.Windows.Forms.ToolStripButton();
            this.btnUpdate = new System.Windows.Forms.ToolStripButton();
            this.btnSettings = new System.Windows.Forms.ToolStripButton();
            this.grpComparison = new System.Windows.Forms.GroupBox();
            this.rtbSourceText = new System.Windows.Forms.RichTextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tvComparison = new System.Windows.Forms.TreeView();
            this.label2 = new System.Windows.Forms.Label();

            // hack around using a parameterized constructor
                spcTarget = new SyncKusto.SchemaPickerControl(new DestinationSelections());
                spcSource = new SyncKusto.SchemaPickerControl(new SourceSelections());
                this.spcTargetHolder = new SyncKusto.SchemaPickerControl();
                this.spcSourceHolder = new SyncKusto.SchemaPickerControl();
            // *
            
            this.toolStrip1.SuspendLayout();
            this.grpComparison.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnCompare,
            this.btnUpdate,
            this.btnSettings});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(639, 25);
            this.toolStrip1.TabIndex = 2;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // btnCompare
            // 
            this.btnCompare.Image = ((System.Drawing.Image)(resources.GetObject("btnCompare.Image")));
            this.btnCompare.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnCompare.Name = "btnCompare";
            this.btnCompare.Size = new System.Drawing.Size(76, 22);
            this.btnCompare.Text = "Compare";
            this.btnCompare.Click += new System.EventHandler(this.btnCompare_Click);
            // 
            // btnUpdate
            // 
            this.btnUpdate.Enabled = false;
            this.btnUpdate.Image = ((System.Drawing.Image)(resources.GetObject("btnUpdate.Image")));
            this.btnUpdate.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnUpdate.Name = "btnUpdate";
            this.btnUpdate.Size = new System.Drawing.Size(65, 22);
            this.btnUpdate.Text = "Update";
            this.btnUpdate.Click += new System.EventHandler(this.btnUpdate_Click);
            // 
            // btnSettings
            // 
            this.btnSettings.Enabled = true;
            this.btnSettings.Image = ((System.Drawing.Image)(resources.GetObject("btnSettings.Image")));
            this.btnSettings.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnSettings.Name = "btnSettings";
            this.btnSettings.Size = new System.Drawing.Size(69, 22);
            this.btnSettings.Text = "Settings";
            this.btnSettings.Click += new System.EventHandler(this.btnSettings_Click);
            // 
            // grpComparison
            // 
            this.grpComparison.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grpComparison.Controls.Add(this.rtbSourceText);
            this.grpComparison.Controls.Add(this.label1);
            this.grpComparison.Controls.Add(this.tvComparison);
            this.grpComparison.Location = new System.Drawing.Point(12, 298);
            this.grpComparison.Name = "grpComparison";
            this.grpComparison.Size = new System.Drawing.Size(615, 535);
            this.grpComparison.TabIndex = 3;
            this.grpComparison.TabStop = false;
            this.grpComparison.Text = "Comparison";
            // 
            // rtbSourceText
            // 
            this.rtbSourceText.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtbSourceText.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rtbSourceText.Location = new System.Drawing.Point(6, 278);
            this.rtbSourceText.Name = "rtbSourceText";
            this.rtbSourceText.Size = new System.Drawing.Size(603, 251);
            this.rtbSourceText.TabIndex = 11;
            this.rtbSourceText.Text = "";
            this.rtbSourceText.WordWrap = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(7, 261);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(23, 13);
            this.label1.TabIndex = 9;
            this.label1.Text = "Diff";
            // 
            // tvComparison
            // 
            this.tvComparison.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tvComparison.CheckBoxes = true;
            this.tvComparison.Location = new System.Drawing.Point(6, 19);
            this.tvComparison.Name = "tvComparison";
            this.tvComparison.Size = new System.Drawing.Size(603, 221);
            this.tvComparison.TabIndex = 6;
            this.tvComparison.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(this.tvComparison_AfterCheck);
            this.tvComparison.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.tvComparison_NodeMouseClick);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(545, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(81, 13);
            this.label2.TabIndex = 6;
            this.label2.Text = "Build 20200626";
            // 
            // spcTargetHolder
            // 
            this.spcTargetHolder.Location = new System.Drawing.Point(326, 29);
            this.spcTargetHolder.Name = "spcTargetHolder";
            this.spcTargetHolder.Size = new System.Drawing.Size(306, 264);
            this.spcTargetHolder.TabIndex = 5;
            this.spcTargetHolder.Title = "Target Schema";
            this.spcTargetHolder.Visible = true;

            // 
            // spcTarget
            // 
            this.spcTarget.Location = this.spcTargetHolder.Location;
            this.spcTarget.Name = this.spcTargetHolder.Name;
            this.spcTarget.Size = this.spcTargetHolder.Size;
            this.spcTarget.TabIndex = this.spcTargetHolder.TabIndex;
            this.spcTarget.Title = this.spcTargetHolder.Title;

            // 
            // spcSourceHolder
            // 
            this.spcSourceHolder.Location = new System.Drawing.Point(18, 28);
            this.spcSourceHolder.Name = "spcSourceHolder";
            this.spcSourceHolder.Size = new System.Drawing.Size(306, 264);
            this.spcSourceHolder.TabIndex = 4;
            this.spcSourceHolder.Title = "Source Schema";
            this.spcSourceHolder.Visible = true;

            // 
            // spcSource
            // 
            this.spcSource.Location = this.spcSourceHolder.Location;
            this.spcSource.Name = this.spcSourceHolder.Name;
            this.spcSource.Size = this.spcSourceHolder.Size;
            this.spcSource.TabIndex = this.spcSourceHolder.TabIndex;
            this.spcSource.Title = this.spcSourceHolder.Title;

            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(639, 845);
            this.Controls.Add(this.label2);
            //this.Controls.Add(this.spcTargetHolder);
            //this.Controls.Add(this.spcSourceHolder);
            spcTargetHolder.Visible = false;
            spcSourceHolder.Visible = false;
            this.Controls.Add(this.spcTarget);
            this.Controls.Add(this.spcSource);
            this.Controls.Add(this.grpComparison);
            this.Controls.Add(this.toolStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(655, 822);
            this.Name = "MainForm";
            this.Text = "SyncKusto";
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.grpComparison.ResumeLayout(false);
            this.grpComparison.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton btnCompare;
        private System.Windows.Forms.GroupBox grpComparison;
        private System.Windows.Forms.TreeView tvComparison;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RichTextBox rtbSourceText;
        private System.Windows.Forms.ToolStripButton btnUpdate;
        private System.Windows.Forms.ToolStripButton btnSettings;
        private SchemaPickerControl spcSourceHolder;
        private SchemaPickerControl spcTargetHolder;
        private SchemaPickerControl spcSource;
        private SchemaPickerControl spcTarget;
        private System.Windows.Forms.Label label2;
    }
}