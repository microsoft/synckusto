// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Drawing;

namespace SyncKusto
{
    partial class SchemaPickerControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.grpSourceSchema = new System.Windows.Forms.GroupBox();
            this.txtOperationProgress = new System.Windows.Forms.Label();
            this.pnlKusto = new System.Windows.Forms.Panel();
            this.grpAuthentication = new System.Windows.Forms.GroupBox();
            this.pnlApplicationAuthentication = new System.Windows.Forms.Panel();
            this.txtAppKey = new System.Windows.Forms.TextBox();
            this.lblAppKey = new System.Windows.Forms.Label();
            this.txtAppId = new System.Windows.Forms.TextBox();
            this.lblAppId = new System.Windows.Forms.Label();
            this.rbApplication = new System.Windows.Forms.RadioButton();
            this.rbFederated = new System.Windows.Forms.RadioButton();
            this.txtDatabase = new System.Windows.Forms.TextBox();
            this.lblDatabase = new System.Windows.Forms.Label();
            this.txtCluster = new System.Windows.Forms.TextBox();
            this.lblCluster = new System.Windows.Forms.Label();
            this.pnlFilePath = new System.Windows.Forms.Panel();
            this.lblExample = new System.Windows.Forms.Label();
            this.txtFilePath = new System.Windows.Forms.TextBox();
            this.btnChooseDirectory = new System.Windows.Forms.Button();
            this.rbKusto = new System.Windows.Forms.RadioButton();
            this.rbFilePath = new System.Windows.Forms.RadioButton();
            this.grpSourceSchema.SuspendLayout();
            this.pnlKusto.SuspendLayout();
            this.grpAuthentication.SuspendLayout();
            this.pnlApplicationAuthentication.SuspendLayout();
            this.pnlFilePath.SuspendLayout();
            this.SuspendLayout();
            // 
            // grpSourceSchema
            // 
            this.grpSourceSchema.Controls.Add(this.txtOperationProgress);
            this.grpSourceSchema.Controls.Add(this.pnlKusto);
            this.grpSourceSchema.Controls.Add(this.pnlFilePath);
            this.grpSourceSchema.Controls.Add(this.rbKusto);
            this.grpSourceSchema.Controls.Add(this.rbFilePath);
            this.grpSourceSchema.Location = new System.Drawing.Point(3, 3);
            this.grpSourceSchema.Name = "grpSourceSchema";
            this.grpSourceSchema.Size = new System.Drawing.Size(297, 259);
            this.grpSourceSchema.TabIndex = 1;
            this.grpSourceSchema.TabStop = false;
            this.grpSourceSchema.Text = "Source Schema";
            // 
            // txtOperationProgress
            // 
            this.txtOperationProgress.AutoSize = true;
            this.txtOperationProgress.Location = new System.Drawing.Point(11, 239);
            this.txtOperationProgress.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.txtOperationProgress.Name = "txtOperationProgress";
            this.txtOperationProgress.Size = new System.Drawing.Size(0, 13);
            this.txtOperationProgress.TabIndex = 21;
            // 
            // pnlKusto
            // 
            this.pnlKusto.Controls.Add(this.grpAuthentication);
            this.pnlKusto.Controls.Add(this.txtDatabase);
            this.pnlKusto.Controls.Add(this.lblDatabase);
            this.pnlKusto.Controls.Add(this.txtCluster);
            this.pnlKusto.Controls.Add(this.lblCluster);
            this.pnlKusto.Location = new System.Drawing.Point(6, 43);
            this.pnlKusto.Name = "pnlKusto";
            this.pnlKusto.Size = new System.Drawing.Size(284, 201);
            this.pnlKusto.TabIndex = 5;
            // 
            // grpAuthentication
            // 
            this.grpAuthentication.Controls.Add(this.pnlApplicationAuthentication);
            this.grpAuthentication.Controls.Add(this.rbApplication);
            this.grpAuthentication.Controls.Add(this.rbFederated);
            this.grpAuthentication.Location = new System.Drawing.Point(7, 56);
            this.grpAuthentication.Name = "grpAuthentication";
            this.grpAuthentication.Size = new System.Drawing.Size(270, 135);
            this.grpAuthentication.TabIndex = 4;
            this.grpAuthentication.TabStop = false;
            this.grpAuthentication.Text = "Authentication";
            // 
            // pnlApplicationAuthentication
            // 
            this.pnlApplicationAuthentication.Controls.Add(this.txtAppKey);
            this.pnlApplicationAuthentication.Controls.Add(this.lblAppKey);
            this.pnlApplicationAuthentication.Controls.Add(this.txtAppId);
            this.pnlApplicationAuthentication.Controls.Add(this.lblAppId);
            this.pnlApplicationAuthentication.Location = new System.Drawing.Point(25, 66);
            this.pnlApplicationAuthentication.Name = "pnlApplicationAuthentication";
            this.pnlApplicationAuthentication.Size = new System.Drawing.Size(239, 58);
            this.pnlApplicationAuthentication.TabIndex = 2;
            this.pnlApplicationAuthentication.Visible = false;
            // 
            // txtAppKey
            // 
            this.txtAppKey.Location = new System.Drawing.Point(64, 29);
            this.txtAppKey.Name = "txtAppKey";
            this.txtAppKey.PasswordChar = '*';
            this.txtAppKey.Size = new System.Drawing.Size(149, 20);
            this.txtAppKey.TabIndex = 7;
            // 
            // lblAppKey
            // 
            this.lblAppKey.AutoSize = true;
            this.lblAppKey.Location = new System.Drawing.Point(2, 33);
            this.lblAppKey.Name = "lblAppKey";
            this.lblAppKey.Size = new System.Drawing.Size(50, 13);
            this.lblAppKey.TabIndex = 6;
            this.lblAppKey.Text = "App Key:";
            // 
            // txtAppId
            // 
            this.txtAppId.Location = new System.Drawing.Point(64, 3);
            this.txtAppId.Name = "txtAppId";
            this.txtAppId.Size = new System.Drawing.Size(149, 20);
            this.txtAppId.TabIndex = 5;
            // 
            // lblAppId
            // 
            this.lblAppId.AutoSize = true;
            this.lblAppId.Location = new System.Drawing.Point(2, 7);
            this.lblAppId.Name = "lblAppId";
            this.lblAppId.Size = new System.Drawing.Size(41, 13);
            this.lblAppId.TabIndex = 4;
            this.lblAppId.Text = "App Id:";
            // 
            // rbApplication
            // 
            this.rbApplication.AutoSize = true;
            this.rbApplication.Location = new System.Drawing.Point(7, 44);
            this.rbApplication.Name = "rbApplication";
            this.rbApplication.Size = new System.Drawing.Size(173, 17);
            this.rbApplication.TabIndex = 1;
            this.rbApplication.Text = "AAD Application Authentication";
            this.rbApplication.UseVisualStyleBackColor = true;
            this.rbApplication.CheckedChanged += new System.EventHandler(this.rbApplication_CheckedChanged);
            // 
            // rbFederated
            // 
            this.rbFederated.AutoSize = true;
            this.rbFederated.Checked = true;
            this.rbFederated.Location = new System.Drawing.Point(7, 20);
            this.rbFederated.Name = "rbFederated";
            this.rbFederated.Size = new System.Drawing.Size(169, 17);
            this.rbFederated.TabIndex = 0;
            this.rbFederated.TabStop = true;
            this.rbFederated.Text = "AAD Federated Authentication";
            this.rbFederated.UseVisualStyleBackColor = true;
            // 
            // txtDatabase
            // 
            this.txtDatabase.Location = new System.Drawing.Point(66, 26);
            this.txtDatabase.Name = "txtDatabase";
            this.txtDatabase.Size = new System.Drawing.Size(211, 20);
            this.txtDatabase.TabIndex = 3;
            // 
            // lblDatabase
            // 
            this.lblDatabase.AutoSize = true;
            this.lblDatabase.Location = new System.Drawing.Point(4, 30);
            this.lblDatabase.Name = "lblDatabase";
            this.lblDatabase.Size = new System.Drawing.Size(56, 13);
            this.lblDatabase.TabIndex = 2;
            this.lblDatabase.Text = "Database:";
            // 
            // txtCluster
            // 
            this.txtCluster.Location = new System.Drawing.Point(66, 0);
            this.txtCluster.Name = "txtCluster";
            this.txtCluster.Size = new System.Drawing.Size(211, 20);
            this.txtCluster.TabIndex = 1;
            // 
            // lblCluster
            // 
            this.lblCluster.AutoSize = true;
            this.lblCluster.Location = new System.Drawing.Point(4, 4);
            this.lblCluster.Name = "lblCluster";
            this.lblCluster.Size = new System.Drawing.Size(42, 13);
            this.lblCluster.TabIndex = 0;
            this.lblCluster.Text = "Cluster:";
            // 
            // pnlFilePath
            // 
            this.pnlFilePath.Controls.Add(this.lblExample);
            this.pnlFilePath.Controls.Add(this.txtFilePath);
            this.pnlFilePath.Controls.Add(this.btnChooseDirectory);
            this.pnlFilePath.Location = new System.Drawing.Point(7, 43);
            this.pnlFilePath.Name = "pnlFilePath";
            this.pnlFilePath.Size = new System.Drawing.Size(284, 84);
            this.pnlFilePath.TabIndex = 4;
            // 
            // lblExample
            // 
            this.lblExample.AutoSize = true;
            this.lblExample.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            this.lblExample.Location = new System.Drawing.Point(4, 30);
            this.lblExample.Name = "lblExample";
            this.lblExample.Size = new System.Drawing.Size(278, 39);
            this.lblExample.TabIndex = 4;
            this.lblExample.Text = "Choose the directory that is the parent of the \"Functions\" \r\ndirectory. \r\nExample" +
    ": c:\\git\\myrepo\\mycluster\\mydatabase";
            // 
            // txtFilePath
            // 
            this.txtFilePath.Location = new System.Drawing.Point(3, 3);
            this.txtFilePath.Name = "txtFilePath";
            this.txtFilePath.Size = new System.Drawing.Size(242, 20);
            this.txtFilePath.TabIndex = 2;
            // 
            // btnChooseDirectory
            // 
            this.btnChooseDirectory.Location = new System.Drawing.Point(251, 3);
            this.btnChooseDirectory.Name = "btnChooseDirectory";
            this.btnChooseDirectory.Size = new System.Drawing.Size(26, 20);
            this.btnChooseDirectory.TabIndex = 3;
            this.btnChooseDirectory.Text = "...";
            this.btnChooseDirectory.UseVisualStyleBackColor = true;
            this.btnChooseDirectory.Click += new System.EventHandler(this.btnChooseDirectory_Click);
            // 
            // rbKusto
            // 
            this.rbKusto.AutoSize = true;
            this.rbKusto.Location = new System.Drawing.Point(78, 19);
            this.rbKusto.Name = "rbKusto";
            this.rbKusto.Size = new System.Drawing.Size(52, 17);
            this.rbKusto.TabIndex = 1;
            this.rbKusto.Text = "Kusto";
            this.rbKusto.UseVisualStyleBackColor = true;
            this.rbKusto.CheckedChanged += new System.EventHandler(this.rbKusto_CheckedChanged);
            // 
            // rbFilePath
            // 
            this.rbFilePath.AutoSize = true;
            this.rbFilePath.Checked = true;
            this.rbFilePath.Location = new System.Drawing.Point(6, 19);
            this.rbFilePath.Name = "rbFilePath";
            this.rbFilePath.Size = new System.Drawing.Size(66, 17);
            this.rbFilePath.TabIndex = 0;
            this.rbFilePath.TabStop = true;
            this.rbFilePath.Text = "File Path";
            this.rbFilePath.UseVisualStyleBackColor = true;
            this.rbFilePath.CheckedChanged += new System.EventHandler(this.rbFilePath_CheckedChanged);
            // 
            // SchemaPickerControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.grpSourceSchema);
            this.Name = "SchemaPickerControl";
            this.Size = new System.Drawing.Size(306, 261);
            this.grpSourceSchema.ResumeLayout(false);
            this.grpSourceSchema.PerformLayout();
            this.pnlKusto.ResumeLayout(false);
            this.pnlKusto.PerformLayout();
            this.grpAuthentication.ResumeLayout(false);
            this.grpAuthentication.PerformLayout();
            this.pnlApplicationAuthentication.ResumeLayout(false);
            this.pnlApplicationAuthentication.PerformLayout();
            this.pnlFilePath.ResumeLayout(false);
            this.pnlFilePath.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox grpSourceSchema;
        private System.Windows.Forms.Panel pnlFilePath;
        private System.Windows.Forms.TextBox txtFilePath;
        private System.Windows.Forms.Button btnChooseDirectory;
        private System.Windows.Forms.RadioButton rbKusto;
        private System.Windows.Forms.RadioButton rbFilePath;
        private System.Windows.Forms.Panel pnlKusto;
        private System.Windows.Forms.TextBox txtDatabase;
        private System.Windows.Forms.Label lblDatabase;
        private System.Windows.Forms.TextBox txtCluster;
        private System.Windows.Forms.Label lblCluster;
        private System.Windows.Forms.GroupBox grpAuthentication;
        private System.Windows.Forms.RadioButton rbApplication;
        private System.Windows.Forms.RadioButton rbFederated;
        private System.Windows.Forms.Panel pnlApplicationAuthentication;
        private System.Windows.Forms.TextBox txtAppKey;
        private System.Windows.Forms.Label lblAppKey;
        private System.Windows.Forms.TextBox txtAppId;
        private System.Windows.Forms.Label lblAppId;
        private System.Windows.Forms.Label lblExample;
        private System.Windows.Forms.Label txtOperationProgress;
    }
}
