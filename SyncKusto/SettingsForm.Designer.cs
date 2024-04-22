﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace SyncKusto
{
    partial class SettingsForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsForm));
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tpTempDatabase = new System.Windows.Forms.TabPage();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.txtKustoDatabase = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtKustoCluster = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tcEntraId = new System.Windows.Forms.TabPage();
            this.txtAuthority = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.tcWarnings = new System.Windows.Forms.TabPage();
            this.chkTableDropWarning = new System.Windows.Forms.CheckBox();
            this.tcFormatting = new System.Windows.Forms.TabPage();
            this.cbUseLegacyCslExtension = new System.Windows.Forms.CheckBox();
            this.label7 = new System.Windows.Forms.Label();
            this.cbCreateMerge = new System.Windows.Forms.CheckBox();
            this.cbTableFieldsOnNewLine = new System.Windows.Forms.CheckBox();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.tabControl.SuspendLayout();
            this.tpTempDatabase.SuspendLayout();
            this.tcEntraId.SuspendLayout();
            this.tcWarnings.SuspendLayout();
            this.tcFormatting.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl
            // 
            this.tabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl.Controls.Add(this.tpTempDatabase);
            this.tabControl.Controls.Add(this.tcEntraId);
            this.tabControl.Controls.Add(this.tcWarnings);
            this.tabControl.Controls.Add(this.tcFormatting);
            this.tabControl.Location = new System.Drawing.Point(12, 12);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(534, 281);
            this.tabControl.TabIndex = 0;
            // 
            // tpTempDatabase
            // 
            this.tpTempDatabase.Controls.Add(this.label5);
            this.tpTempDatabase.Controls.Add(this.label4);
            this.tpTempDatabase.Controls.Add(this.txtKustoDatabase);
            this.tpTempDatabase.Controls.Add(this.label2);
            this.tpTempDatabase.Controls.Add(this.txtKustoCluster);
            this.tpTempDatabase.Controls.Add(this.label1);
            this.tpTempDatabase.Location = new System.Drawing.Point(4, 29);
            this.tpTempDatabase.Name = "tpTempDatabase";
            this.tpTempDatabase.Padding = new System.Windows.Forms.Padding(3, 10, 3, 10);
            this.tpTempDatabase.Size = new System.Drawing.Size(526, 248);
            this.tpTempDatabase.TabIndex = 0;
            this.tpTempDatabase.Text = "Temporary Database";
            this.tpTempDatabase.UseVisualStyleBackColor = true;
            // 
            // label5
            // 
            this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label5.ForeColor = System.Drawing.Color.Red;
            this.label5.Location = new System.Drawing.Point(7, 100);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(512, 32);
            this.label5.TabIndex = 108;
            this.label5.Text = "Everything in this database will be dropped before every comparison!";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(7, 182);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(83, 20);
            this.label4.TabIndex = 110;
            this.label4.Text = "&Database:";
            // 
            // txtKustoDatabase
            // 
            this.txtKustoDatabase.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtKustoDatabase.Location = new System.Drawing.Point(102, 177);
            this.txtKustoDatabase.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txtKustoDatabase.Name = "txtKustoDatabase";
            this.txtKustoDatabase.Size = new System.Drawing.Size(417, 26);
            this.txtKustoDatabase.TabIndex = 106;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(7, 142);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(63, 20);
            this.label2.TabIndex = 109;
            this.label2.Text = "C&luster:";
            // 
            // txtKustoCluster
            // 
            this.txtKustoCluster.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtKustoCluster.Location = new System.Drawing.Point(102, 137);
            this.txtKustoCluster.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txtKustoCluster.Name = "txtKustoCluster";
            this.txtKustoCluster.Size = new System.Drawing.Size(417, 26);
            this.txtKustoCluster.TabIndex = 105;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.Location = new System.Drawing.Point(7, 10);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(512, 90);
            this.label1.TabIndex = 107;
            this.label1.Text = resources.GetString("label1.Text");
            // 
            // tcEntraId
            // 
            this.tcEntraId.Controls.Add(this.txtAuthority);
            this.tcEntraId.Controls.Add(this.label3);
            this.tcEntraId.Location = new System.Drawing.Point(4, 29);
            this.tcEntraId.Name = "tcEntraId";
            this.tcEntraId.Padding = new System.Windows.Forms.Padding(3, 10, 3, 10);
            this.tcEntraId.Size = new System.Drawing.Size(526, 248);
            this.tcEntraId.TabIndex = 1;
            this.tcEntraId.Text = "Entra Id";
            this.tcEntraId.UseVisualStyleBackColor = true;
            // 
            // txtAuthority
            // 
            this.txtAuthority.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtAuthority.Location = new System.Drawing.Point(8, 100);
            this.txtAuthority.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txtAuthority.Name = "txtAuthority";
            this.txtAuthority.Size = new System.Drawing.Size(501, 26);
            this.txtAuthority.TabIndex = 108;
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.Location = new System.Drawing.Point(4, 10);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(498, 85);
            this.label3.TabIndex = 109;
            this.label3.Text = resources.GetString("label3.Text");
            // 
            // tcWarnings
            // 
            this.tcWarnings.Controls.Add(this.chkTableDropWarning);
            this.tcWarnings.Location = new System.Drawing.Point(4, 29);
            this.tcWarnings.Name = "tcWarnings";
            this.tcWarnings.Padding = new System.Windows.Forms.Padding(3, 10, 3, 10);
            this.tcWarnings.Size = new System.Drawing.Size(526, 248);
            this.tcWarnings.TabIndex = 2;
            this.tcWarnings.Text = "Warnings";
            this.tcWarnings.UseVisualStyleBackColor = true;
            // 
            // chkTableDropWarning
            // 
            this.chkTableDropWarning.AutoSize = true;
            this.chkTableDropWarning.Location = new System.Drawing.Point(6, 12);
            this.chkTableDropWarning.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.chkTableDropWarning.Name = "chkTableDropWarning";
            this.chkTableDropWarning.Size = new System.Drawing.Size(438, 24);
            this.chkTableDropWarning.TabIndex = 4;
            this.chkTableDropWarning.Text = "&Ask before dropping objects in the target Kusto database";
            this.chkTableDropWarning.UseVisualStyleBackColor = true;
            // 
            // tcFormatting
            // 
            this.tcFormatting.Controls.Add(this.cbUseLegacyCslExtension);
            this.tcFormatting.Controls.Add(this.label7);
            this.tcFormatting.Controls.Add(this.cbCreateMerge);
            this.tcFormatting.Controls.Add(this.cbTableFieldsOnNewLine);
            this.tcFormatting.Location = new System.Drawing.Point(4, 29);
            this.tcFormatting.Name = "tcFormatting";
            this.tcFormatting.Padding = new System.Windows.Forms.Padding(3, 10, 3, 10);
            this.tcFormatting.Size = new System.Drawing.Size(526, 248);
            this.tcFormatting.TabIndex = 3;
            this.tcFormatting.Text = "Formatting";
            this.tcFormatting.UseVisualStyleBackColor = true;
            // 
            // cbUseLegacyCslExtension
            // 
            this.cbUseLegacyCslExtension.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cbUseLegacyCslExtension.AutoSize = true;
            this.cbUseLegacyCslExtension.Location = new System.Drawing.Point(10, 144);
            this.cbUseLegacyCslExtension.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cbUseLegacyCslExtension.Name = "cbUseLegacyCslExtension";
            this.cbUseLegacyCslExtension.Size = new System.Drawing.Size(244, 24);
            this.cbUseLegacyCslExtension.TabIndex = 114;
            this.cbUseLegacyCslExtension.Text = "&Use legacy .csl file extensions";
            this.cbUseLegacyCslExtension.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.cbUseLegacyCslExtension.UseVisualStyleBackColor = true;
            // 
            // label7
            // 
            this.label7.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label7.Location = new System.Drawing.Point(6, 10);
            this.label7.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(496, 68);
            this.label7.TabIndex = 113;
            this.label7.Text = resources.GetString("label7.Text");
            // 
            // cbCreateMerge
            // 
            this.cbCreateMerge.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cbCreateMerge.AutoSize = true;
            this.cbCreateMerge.Location = new System.Drawing.Point(10, 116);
            this.cbCreateMerge.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cbCreateMerge.Name = "cbCreateMerge";
            this.cbCreateMerge.Size = new System.Drawing.Size(517, 24);
            this.cbCreateMerge.TabIndex = 112;
            this.cbCreateMerge.Text = "&Generate \".create-merge table\" commands instead of \".create table\"";
            this.cbCreateMerge.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.cbCreateMerge.UseVisualStyleBackColor = true;
            // 
            // cbTableFieldsOnNewLine
            // 
            this.cbTableFieldsOnNewLine.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cbTableFieldsOnNewLine.AutoSize = true;
            this.cbTableFieldsOnNewLine.Location = new System.Drawing.Point(10, 86);
            this.cbTableFieldsOnNewLine.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cbTableFieldsOnNewLine.Name = "cbTableFieldsOnNewLine";
            this.cbTableFieldsOnNewLine.Size = new System.Drawing.Size(245, 24);
            this.cbTableFieldsOnNewLine.TabIndex = 111;
            this.cbTableFieldsOnNewLine.Text = "&Place table fields on new lines";
            this.cbTableFieldsOnNewLine.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.cbTableFieldsOnNewLine.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.Location = new System.Drawing.Point(428, 301);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(112, 35);
            this.btnCancel.TabIndex = 9;
            this.btnCancel.Text = "&Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.Location = new System.Drawing.Point(307, 301);
            this.btnOk.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(112, 35);
            this.btnOk.TabIndex = 8;
            this.btnOk.Text = "O&K";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(553, 349);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.tabControl);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "SettingsForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "Settings";
            this.Load += new System.EventHandler(this.SettingsForm_Load);
            this.tabControl.ResumeLayout(false);
            this.tpTempDatabase.ResumeLayout(false);
            this.tpTempDatabase.PerformLayout();
            this.tcEntraId.ResumeLayout(false);
            this.tcEntraId.PerformLayout();
            this.tcWarnings.ResumeLayout(false);
            this.tcWarnings.PerformLayout();
            this.tcFormatting.ResumeLayout(false);
            this.tcFormatting.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tpTempDatabase;
        private System.Windows.Forms.TabPage tcEntraId;
        private System.Windows.Forms.TabPage tcWarnings;
        private System.Windows.Forms.TabPage tcFormatting;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtKustoDatabase;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtKustoCluster;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtAuthority;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox chkTableDropWarning;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.CheckBox cbCreateMerge;
        private System.Windows.Forms.CheckBox cbTableFieldsOnNewLine;
        private System.Windows.Forms.CheckBox cbUseLegacyCslExtension;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOk;
    }
}