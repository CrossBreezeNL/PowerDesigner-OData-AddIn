
namespace CrossBreeze.Tools.PowerDesigner.AddIn.OData
{
    partial class PdODataReversePropertiesForm
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
            this.reverseEngineerButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.labelModelName = new System.Windows.Forms.Label();
            this.textBoxModelName = new System.Windows.Forms.TextBox();
            this.labelODataMetadataURI = new System.Windows.Forms.Label();
            this.textBoxODataMetadataURI = new System.Windows.Forms.TextBox();
            this.labelAuthenticationType = new System.Windows.Forms.Label();
            this.comboBoxAuthenticationType = new System.Windows.Forms.ComboBox();
            this.labelFormExplanation = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // reverseEngineerButton
            // 
            this.reverseEngineerButton.Location = new System.Drawing.Point(160, 176);
            this.reverseEngineerButton.Name = "reverseEngineerButton";
            this.reverseEngineerButton.Size = new System.Drawing.Size(118, 23);
            this.reverseEngineerButton.TabIndex = 6;
            this.reverseEngineerButton.Text = "Reverse engineer";
            this.reverseEngineerButton.UseVisualStyleBackColor = true;
            this.reverseEngineerButton.Click += new System.EventHandler(this.reverseEngineerButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.Location = new System.Drawing.Point(285, 176);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 7;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // labelModelName
            // 
            this.labelModelName.AutoSize = true;
            this.labelModelName.Location = new System.Drawing.Point(21, 48);
            this.labelModelName.Name = "labelModelName";
            this.labelModelName.Size = new System.Drawing.Size(92, 13);
            this.labelModelName.TabIndex = 0;
            this.labelModelName.Text = "New model name:";
            // 
            // textBoxModelName
            // 
            this.textBoxModelName.Location = new System.Drawing.Point(160, 45);
            this.textBoxModelName.Name = "textBoxModelName";
            this.textBoxModelName.Size = new System.Drawing.Size(200, 20);
            this.textBoxModelName.TabIndex = 1;
            // 
            // labelODataMetadataURI
            // 
            this.labelODataMetadataURI.AutoSize = true;
            this.labelODataMetadataURI.Location = new System.Drawing.Point(21, 90);
            this.labelODataMetadataURI.Name = "labelODataMetadataURI";
            this.labelODataMetadataURI.Size = new System.Drawing.Size(116, 13);
            this.labelODataMetadataURI.TabIndex = 2;
            this.labelODataMetadataURI.Text = "OData $metadata URI:";
            // 
            // textBoxODataMetadataURI
            // 
            this.textBoxODataMetadataURI.Location = new System.Drawing.Point(160, 87);
            this.textBoxODataMetadataURI.Name = "textBoxODataMetadataURI";
            this.textBoxODataMetadataURI.Size = new System.Drawing.Size(400, 20);
            this.textBoxODataMetadataURI.TabIndex = 3;
            // 
            // labelAuthenticationType
            // 
            this.labelAuthenticationType.AutoSize = true;
            this.labelAuthenticationType.Location = new System.Drawing.Point(21, 131);
            this.labelAuthenticationType.Name = "labelAuthenticationType";
            this.labelAuthenticationType.Size = new System.Drawing.Size(101, 13);
            this.labelAuthenticationType.TabIndex = 4;
            this.labelAuthenticationType.Text = "Authentication type:";
            // 
            // comboBoxAuthenticationType
            // 
            this.comboBoxAuthenticationType.FormattingEnabled = true;
            this.comboBoxAuthenticationType.Location = new System.Drawing.Point(160, 128);
            this.comboBoxAuthenticationType.Name = "comboBoxAuthenticationType";
            this.comboBoxAuthenticationType.Size = new System.Drawing.Size(200, 21);
            this.comboBoxAuthenticationType.TabIndex = 5;
            // 
            // labelFormExplanation
            // 
            this.labelFormExplanation.AutoSize = true;
            this.labelFormExplanation.Location = new System.Drawing.Point(21, 18);
            this.labelFormExplanation.Name = "labelFormExplanation";
            this.labelFormExplanation.Size = new System.Drawing.Size(344, 13);
            this.labelFormExplanation.TabIndex = 8;
            this.labelFormExplanation.Text = "Please enter the information asked below and press \'Reverse engineer\'.";
            // 
            // PdODataReversePropertiesForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 211);
            this.Controls.Add(this.labelFormExplanation);
            this.Controls.Add(this.comboBoxAuthenticationType);
            this.Controls.Add(this.labelAuthenticationType);
            this.Controls.Add(this.textBoxODataMetadataURI);
            this.Controls.Add(this.labelODataMetadataURI);
            this.Controls.Add(this.textBoxModelName);
            this.Controls.Add(this.labelModelName);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.reverseEngineerButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "PdODataReversePropertiesForm";
            this.Text = "Reverse engineer OData to PDM";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button reverseEngineerButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Label labelModelName;
        private System.Windows.Forms.TextBox textBoxModelName;
        private System.Windows.Forms.Label labelODataMetadataURI;
        private System.Windows.Forms.TextBox textBoxODataMetadataURI;
        private System.Windows.Forms.Label labelAuthenticationType;
        private System.Windows.Forms.ComboBox comboBoxAuthenticationType;
        private System.Windows.Forms.Label labelFormExplanation;
    }
}