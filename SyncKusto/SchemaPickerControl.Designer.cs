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
            this.pnlApplicationSniAuthentication = new System.Windows.Forms.Panel();
            this.btnCertificate = new System.Windows.Forms.Button();
            this.txtCertificate = new System.Windows.Forms.TextBox();
            this.lblCertificate = new System.Windows.Forms.Label();
            this.txtAppIdSni = new System.Windows.Forms.TextBox();
            this.lblAppIdSni = new System.Windows.Forms.Label();
            this.cmbAuthentication = new System.Windows.Forms.ComboBox();
            this.pnlApplicationAuthentication = new System.Windows.Forms.Panel();
            this.txtAppKey = new System.Windows.Forms.TextBox();
            this.lblAppKey = new System.Windows.Forms.Label();
            this.txtAppId = new System.Windows.Forms.TextBox();
            this.lblAppId = new System.Windows.Forms.Label();
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
            this.pnlApplicationSniAuthentication.SuspendLayout();
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
            this.grpSourceSchema.Location = new System.Drawing.Point(4, 5);
            this.grpSourceSchema.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.grpSourceSchema.Name = "grpSourceSchema";
            this.grpSourceSchema.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.grpSourceSchema.Size = new System.Drawing.Size(446, 348);
            this.grpSourceSchema.TabIndex = 1;
            this.grpSourceSchema.TabStop = false;
            this.grpSourceSchema.Text = "Source Schema";
            // 
            // txtOperationProgress
            // 
            this.txtOperationProgress.AutoSize = true;
            this.txtOperationProgress.Location = new System.Drawing.Point(16, 368);
            this.txtOperationProgress.Name = "txtOperationProgress";
            this.txtOperationProgress.Size = new System.Drawing.Size(0, 20);
            this.txtOperationProgress.TabIndex = 21;
            // 
            // pnlKusto
            // 
            this.pnlKusto.Controls.Add(this.grpAuthentication);
            this.pnlKusto.Controls.Add(this.txtDatabase);
            this.pnlKusto.Controls.Add(this.lblDatabase);
            this.pnlKusto.Controls.Add(this.txtCluster);
            this.pnlKusto.Controls.Add(this.lblCluster);
            this.pnlKusto.Location = new System.Drawing.Point(9, 66);
            this.pnlKusto.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.pnlKusto.Name = "pnlKusto";
            this.pnlKusto.Size = new System.Drawing.Size(426, 265);
            this.pnlKusto.TabIndex = 5;
            // 
            // grpAuthentication
            // 
            this.grpAuthentication.Controls.Add(this.pnlApplicationSniAuthentication);
            this.grpAuthentication.Controls.Add(this.cmbAuthentication);
            this.grpAuthentication.Controls.Add(this.pnlApplicationAuthentication);
            this.grpAuthentication.Location = new System.Drawing.Point(10, 86);
            this.grpAuthentication.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.grpAuthentication.Name = "grpAuthentication";
            this.grpAuthentication.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.grpAuthentication.Size = new System.Drawing.Size(405, 172);
            this.grpAuthentication.TabIndex = 4;
            this.grpAuthentication.TabStop = false;
            this.grpAuthentication.Text = "Authentication Mode";
            this.grpAuthentication.UseCompatibleTextRendering = true;
            // 
            // pnlApplicationSniAuthentication
            // 
            this.pnlApplicationSniAuthentication.Controls.Add(this.btnCertificate);
            this.pnlApplicationSniAuthentication.Controls.Add(this.txtCertificate);
            this.pnlApplicationSniAuthentication.Controls.Add(this.lblCertificate);
            this.pnlApplicationSniAuthentication.Controls.Add(this.txtAppIdSni);
            this.pnlApplicationSniAuthentication.Controls.Add(this.lblAppIdSni);
            this.pnlApplicationSniAuthentication.Location = new System.Drawing.Point(9, 71);
            this.pnlApplicationSniAuthentication.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.pnlApplicationSniAuthentication.Name = "pnlApplicationSniAuthentication";
            this.pnlApplicationSniAuthentication.Size = new System.Drawing.Size(388, 89);
            this.pnlApplicationSniAuthentication.TabIndex = 4;
            this.pnlApplicationSniAuthentication.Visible = false;
            // 
            // btnCertificate
            // 
            this.btnCertificate.Location = new System.Drawing.Point(349, 48);
            this.btnCertificate.Name = "btnCertificate";
            this.btnCertificate.Size = new System.Drawing.Size(39, 31);
            this.btnCertificate.TabIndex = 8;
            this.btnCertificate.Text = "...";
            this.btnCertificate.UseVisualStyleBackColor = true;
            this.btnCertificate.Click += new System.EventHandler(this.btnCertificate_Click);
            // 
            // txtCertificate
            // 
            this.txtCertificate.Location = new System.Drawing.Point(180, 48);
            this.txtCertificate.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txtCertificate.Name = "txtCertificate";
            this.txtCertificate.Size = new System.Drawing.Size(160, 26);
            this.txtCertificate.TabIndex = 7;
            // 
            // lblCertificate
            // 
            this.lblCertificate.AutoSize = true;
            this.lblCertificate.Location = new System.Drawing.Point(3, 51);
            this.lblCertificate.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblCertificate.Name = "lblCertificate";
            this.lblCertificate.Size = new System.Drawing.Size(169, 20);
            this.lblCertificate.TabIndex = 6;
            this.lblCertificate.Text = "Certificate Thumbprint:";
            // 
            // txtAppIdSni
            // 
            this.txtAppIdSni.Location = new System.Drawing.Point(180, 8);
            this.txtAppIdSni.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txtAppIdSni.Name = "txtAppIdSni";
            this.txtAppIdSni.Size = new System.Drawing.Size(204, 26);
            this.txtAppIdSni.TabIndex = 5;
            // 
            // lblAppIdSni
            // 
            this.lblAppIdSni.AutoSize = true;
            this.lblAppIdSni.Location = new System.Drawing.Point(3, 11);
            this.lblAppIdSni.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblAppIdSni.Name = "lblAppIdSni";
            this.lblAppIdSni.Size = new System.Drawing.Size(60, 20);
            this.lblAppIdSni.TabIndex = 4;
            this.lblAppIdSni.Text = "App Id:";
            // 
            // cmbAuthentication
            // 
            this.cmbAuthentication.FormattingEnabled = true;
            this.cmbAuthentication.Location = new System.Drawing.Point(7, 27);
            this.cmbAuthentication.Name = "cmbAuthentication";
            this.cmbAuthentication.Size = new System.Drawing.Size(389, 28);
            this.cmbAuthentication.TabIndex = 3;
            this.cmbAuthentication.SelectedValueChanged += new System.EventHandler(this.cmbAuthentication_SelectedValueChanged);
            // 
            // pnlApplicationAuthentication
            // 
            this.pnlApplicationAuthentication.Controls.Add(this.txtAppKey);
            this.pnlApplicationAuthentication.Controls.Add(this.lblAppKey);
            this.pnlApplicationAuthentication.Controls.Add(this.txtAppId);
            this.pnlApplicationAuthentication.Controls.Add(this.lblAppId);
            this.pnlApplicationAuthentication.Location = new System.Drawing.Point(9, 71);
            this.pnlApplicationAuthentication.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.pnlApplicationAuthentication.Name = "pnlApplicationAuthentication";
            this.pnlApplicationAuthentication.Size = new System.Drawing.Size(388, 89);
            this.pnlApplicationAuthentication.TabIndex = 2;
            this.pnlApplicationAuthentication.Visible = false;
            // 
            // txtAppKey
            // 
            this.txtAppKey.Location = new System.Drawing.Point(96, 48);
            this.txtAppKey.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txtAppKey.Name = "txtAppKey";
            this.txtAppKey.PasswordChar = '*';
            this.txtAppKey.Size = new System.Drawing.Size(288, 26);
            this.txtAppKey.TabIndex = 7;
            // 
            // lblAppKey
            // 
            this.lblAppKey.AutoSize = true;
            this.lblAppKey.Location = new System.Drawing.Point(3, 51);
            this.lblAppKey.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblAppKey.Name = "lblAppKey";
            this.lblAppKey.Size = new System.Drawing.Size(72, 20);
            this.lblAppKey.TabIndex = 6;
            this.lblAppKey.Text = "App Key:";
            // 
            // txtAppId
            // 
            this.txtAppId.Location = new System.Drawing.Point(96, 8);
            this.txtAppId.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txtAppId.Name = "txtAppId";
            this.txtAppId.Size = new System.Drawing.Size(288, 26);
            this.txtAppId.TabIndex = 5;
            // 
            // lblAppId
            // 
            this.lblAppId.AutoSize = true;
            this.lblAppId.Location = new System.Drawing.Point(3, 11);
            this.lblAppId.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblAppId.Name = "lblAppId";
            this.lblAppId.Size = new System.Drawing.Size(60, 20);
            this.lblAppId.TabIndex = 4;
            this.lblAppId.Text = "App Id:";
            // 
            // txtDatabase
            // 
            this.txtDatabase.Location = new System.Drawing.Point(99, 44);
            this.txtDatabase.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txtDatabase.Name = "txtDatabase";
            this.txtDatabase.Size = new System.Drawing.Size(314, 26);
            this.txtDatabase.TabIndex = 3;
            // 
            // lblDatabase
            // 
            this.lblDatabase.AutoSize = true;
            this.lblDatabase.Location = new System.Drawing.Point(6, 46);
            this.lblDatabase.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblDatabase.Name = "lblDatabase";
            this.lblDatabase.Size = new System.Drawing.Size(83, 20);
            this.lblDatabase.TabIndex = 2;
            this.lblDatabase.Text = "Database:";
            // 
            // txtCluster
            // 
            this.txtCluster.Location = new System.Drawing.Point(99, 4);
            this.txtCluster.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txtCluster.Name = "txtCluster";
            this.txtCluster.Size = new System.Drawing.Size(314, 26);
            this.txtCluster.TabIndex = 1;
            // 
            // lblCluster
            // 
            this.lblCluster.AutoSize = true;
            this.lblCluster.Location = new System.Drawing.Point(6, 6);
            this.lblCluster.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblCluster.Name = "lblCluster";
            this.lblCluster.Size = new System.Drawing.Size(63, 20);
            this.lblCluster.TabIndex = 0;
            this.lblCluster.Text = "Cluster:";
            // 
            // pnlFilePath
            // 
            this.pnlFilePath.Controls.Add(this.lblExample);
            this.pnlFilePath.Controls.Add(this.txtFilePath);
            this.pnlFilePath.Controls.Add(this.btnChooseDirectory);
            this.pnlFilePath.Location = new System.Drawing.Point(10, 66);
            this.pnlFilePath.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.pnlFilePath.Name = "pnlFilePath";
            this.pnlFilePath.Size = new System.Drawing.Size(426, 129);
            this.pnlFilePath.TabIndex = 4;
            // 
            // lblExample
            // 
            this.lblExample.AutoSize = true;
            this.lblExample.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            this.lblExample.Location = new System.Drawing.Point(6, 46);
            this.lblExample.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblExample.Name = "lblExample";
            this.lblExample.Size = new System.Drawing.Size(414, 60);
            this.lblExample.TabIndex = 4;
            this.lblExample.Text = "Choose the directory that is the parent of the \"Functions\" \r\ndirectory. \r\nExample" +
    ": c:\\git\\myrepo\\mycluster\\mydatabase";
            // 
            // txtFilePath
            // 
            this.txtFilePath.Location = new System.Drawing.Point(4, 5);
            this.txtFilePath.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txtFilePath.Name = "txtFilePath";
            this.txtFilePath.Size = new System.Drawing.Size(361, 26);
            this.txtFilePath.TabIndex = 2;
            // 
            // btnChooseDirectory
            // 
            this.btnChooseDirectory.Location = new System.Drawing.Point(376, 5);
            this.btnChooseDirectory.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnChooseDirectory.Name = "btnChooseDirectory";
            this.btnChooseDirectory.Size = new System.Drawing.Size(39, 31);
            this.btnChooseDirectory.TabIndex = 3;
            this.btnChooseDirectory.Text = "...";
            this.btnChooseDirectory.UseVisualStyleBackColor = true;
            this.btnChooseDirectory.Click += new System.EventHandler(this.btnChooseDirectory_Click);
            // 
            // rbKusto
            // 
            this.rbKusto.AutoSize = true;
            this.rbKusto.Location = new System.Drawing.Point(117, 29);
            this.rbKusto.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.rbKusto.Name = "rbKusto";
            this.rbKusto.Size = new System.Drawing.Size(75, 24);
            this.rbKusto.TabIndex = 1;
            this.rbKusto.Text = "Kusto";
            this.rbKusto.UseVisualStyleBackColor = true;
            this.rbKusto.CheckedChanged += new System.EventHandler(this.rbKusto_CheckedChanged);
            // 
            // rbFilePath
            // 
            this.rbFilePath.AutoSize = true;
            this.rbFilePath.Checked = true;
            this.rbFilePath.Location = new System.Drawing.Point(9, 29);
            this.rbFilePath.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.rbFilePath.Name = "rbFilePath";
            this.rbFilePath.Size = new System.Drawing.Size(96, 24);
            this.rbFilePath.TabIndex = 0;
            this.rbFilePath.TabStop = true;
            this.rbFilePath.Text = "File Path";
            this.rbFilePath.UseVisualStyleBackColor = true;
            this.rbFilePath.CheckedChanged += new System.EventHandler(this.rbFilePath_CheckedChanged);
            // 
            // SchemaPickerControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.grpSourceSchema);
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "SchemaPickerControl";
            this.Size = new System.Drawing.Size(459, 362);
            this.grpSourceSchema.ResumeLayout(false);
            this.grpSourceSchema.PerformLayout();
            this.pnlKusto.ResumeLayout(false);
            this.pnlKusto.PerformLayout();
            this.grpAuthentication.ResumeLayout(false);
            this.pnlApplicationSniAuthentication.ResumeLayout(false);
            this.pnlApplicationSniAuthentication.PerformLayout();
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
        private System.Windows.Forms.Panel pnlApplicationAuthentication;
        private System.Windows.Forms.TextBox txtAppKey;
        private System.Windows.Forms.Label lblAppKey;
        private System.Windows.Forms.TextBox txtAppId;
        private System.Windows.Forms.Label lblAppId;
        private System.Windows.Forms.Label lblExample;
        private System.Windows.Forms.Label txtOperationProgress;
        private System.Windows.Forms.ComboBox cmbAuthentication;
        private System.Windows.Forms.Panel pnlApplicationSniAuthentication;
        private System.Windows.Forms.Button btnCertificate;
        private System.Windows.Forms.TextBox txtCertificate;
        private System.Windows.Forms.Label lblCertificate;
        private System.Windows.Forms.TextBox txtAppIdSni;
        private System.Windows.Forms.Label lblAppIdSni;
    }
}
