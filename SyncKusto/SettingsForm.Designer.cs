// Copyright (c) Microsoft Corporation. All rights reserved.
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
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.txtAuthority = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.txtKustoDatabase = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtKustoCluster = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.chkTableDropWarning = new System.Windows.Forms.CheckBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.label6 = new System.Windows.Forms.Label();
            this.cbCreateMerge = new System.Windows.Forms.CheckBox();
            this.cbTableFieldsOnNewLine = new System.Windows.Forms.CheckBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.Location = new System.Drawing.Point(339, 727);
            this.btnOk.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(112, 35);
            this.btnOk.TabIndex = 6;
            this.btnOk.Text = "O&K";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.Location = new System.Drawing.Point(460, 727);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(112, 35);
            this.btnCancel.TabIndex = 7;
            this.btnCancel.Text = "&Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.txtAuthority);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Location = new System.Drawing.Point(18, 271);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.groupBox1.Size = new System.Drawing.Size(554, 154);
            this.groupBox1.TabIndex = 106;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "AAD Authority";
            // 
            // txtAuthority
            // 
            this.txtAuthority.Location = new System.Drawing.Point(9, 114);
            this.txtAuthority.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txtAuthority.Name = "txtAuthority";
            this.txtAuthority.Size = new System.Drawing.Size(534, 26);
            this.txtAuthority.TabIndex = 2;
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(9, 25);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(536, 85);
            this.label3.TabIndex = 107;
            this.label3.Text = resources.GetString("label3.Text");
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label5);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.txtKustoDatabase);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.txtKustoCluster);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Location = new System.Drawing.Point(18, 18);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.groupBox2.Size = new System.Drawing.Size(554, 243);
            this.groupBox2.TabIndex = 6;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Temporary Databases";
            // 
            // label5
            // 
            this.label5.ForeColor = System.Drawing.Color.Red;
            this.label5.Location = new System.Drawing.Point(9, 122);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(536, 32);
            this.label5.TabIndex = 101;
            this.label5.Text = "Everything in this database will be deleted every time a comparison is done.";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(9, 204);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(83, 20);
            this.label4.TabIndex = 104;
            this.label4.Text = "&Database:";
            // 
            // txtKustoDatabase
            // 
            this.txtKustoDatabase.Location = new System.Drawing.Point(104, 199);
            this.txtKustoDatabase.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txtKustoDatabase.Name = "txtKustoDatabase";
            this.txtKustoDatabase.Size = new System.Drawing.Size(439, 26);
            this.txtKustoDatabase.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(9, 164);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(63, 20);
            this.label2.TabIndex = 103;
            this.label2.Text = "C&luster:";
            // 
            // txtKustoCluster
            // 
            this.txtKustoCluster.Location = new System.Drawing.Point(104, 159);
            this.txtKustoCluster.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txtKustoCluster.Name = "txtKustoCluster";
            this.txtKustoCluster.Size = new System.Drawing.Size(439, 26);
            this.txtKustoCluster.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(9, 32);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(536, 90);
            this.label1.TabIndex = 100;
            this.label1.Text = resources.GetString("label1.Text");
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.chkTableDropWarning);
            this.groupBox3.Location = new System.Drawing.Point(18, 434);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(554, 83);
            this.groupBox3.TabIndex = 108;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Warnings";
            // 
            // chkTableDropWarning
            // 
            this.chkTableDropWarning.AutoSize = true;
            this.chkTableDropWarning.Location = new System.Drawing.Point(13, 35);
            this.chkTableDropWarning.Name = "chkTableDropWarning";
            this.chkTableDropWarning.Size = new System.Drawing.Size(438, 24);
            this.chkTableDropWarning.TabIndex = 3;
            this.chkTableDropWarning.Text = "&Ask before dropping objects in the target Kusto database";
            this.chkTableDropWarning.UseVisualStyleBackColor = true;
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.label6);
            this.groupBox4.Controls.Add(this.cbCreateMerge);
            this.groupBox4.Controls.Add(this.cbTableFieldsOnNewLine);
            this.groupBox4.Location = new System.Drawing.Point(18, 523);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(554, 185);
            this.groupBox4.TabIndex = 109;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Formatting";
            // 
            // label6
            // 
            this.label6.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label6.Location = new System.Drawing.Point(9, 115);
            this.label6.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(534, 67);
            this.label6.TabIndex = 110;
            this.label6.Text = resources.GetString("label6.Text");
            // 
            // cbCreateMerge
            // 
            this.cbCreateMerge.AutoSize = true;
            this.cbCreateMerge.Location = new System.Drawing.Point(13, 65);
            this.cbCreateMerge.Name = "cbCreateMerge";
            this.cbCreateMerge.Size = new System.Drawing.Size(517, 24);
            this.cbCreateMerge.TabIndex = 5;
            this.cbCreateMerge.Text = "&Generate \".create-merge table\" commands instead of \".create table\"";
            this.cbCreateMerge.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.cbCreateMerge.UseVisualStyleBackColor = true;
            // 
            // cbTableFieldsOnNewLine
            // 
            this.cbTableFieldsOnNewLine.AutoSize = true;
            this.cbTableFieldsOnNewLine.Location = new System.Drawing.Point(13, 35);
            this.cbTableFieldsOnNewLine.Name = "cbTableFieldsOnNewLine";
            this.cbTableFieldsOnNewLine.Size = new System.Drawing.Size(245, 24);
            this.cbTableFieldsOnNewLine.TabIndex = 4;
            this.cbTableFieldsOnNewLine.Text = "&Place table fields on new lines";
            this.cbTableFieldsOnNewLine.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.cbTableFieldsOnNewLine.UseVisualStyleBackColor = true;
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(590, 774);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "SettingsForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "Settings";
            this.Load += new System.EventHandler(this.ClusterSelectionForm_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox txtAuthority;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtKustoCluster;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtKustoDatabase;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.CheckBox chkTableDropWarning;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.CheckBox cbCreateMerge;
        private System.Windows.Forms.CheckBox cbTableFieldsOnNewLine;
    }
}