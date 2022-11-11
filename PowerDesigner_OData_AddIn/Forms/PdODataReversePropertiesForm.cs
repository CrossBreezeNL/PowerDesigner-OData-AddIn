using System;
using System.Windows.Forms;

namespace CrossBreeze.Tools.PowerDesigner.AddIn.OData
{
    public partial class PdODataReversePropertiesForm : Form
    {
        PdODataModelUpdater _pdDataModelUpdater;

        public PdODataReversePropertiesForm(PdODataModelUpdater pdDataModelUpdater)
        {
            InitializeComponent();

            // Store reference to the PD Data Model Updater.
            this._pdDataModelUpdater = pdDataModelUpdater;

            // Add the authentication types from the Enum to the combobox.
            this.comboBoxAuthenticationType.DataSource = Enum.GetValues(typeof(PdODataModelUpdater.ODataAutenticationType));
            // The default value for the combobox is automatically set to the first item, so None.
        }

        // If the reverse engineer button is pressed, start the process.
        private void reverseEngineerButton_Click(object sender, EventArgs e)
        {
            // Validate the contents of the text and combo boxes.
            if (this.textBoxModelName.Text == null || this.textBoxModelName.Text.Length == 0)
            {
                MessageBox.Show(this, "Please enter the model name is empty.");
                return;
            }
            else if(this.textBoxODataMetadataURI.Text == null || this.textBoxODataMetadataURI.Text.Length == 0)
            {
                MessageBox.Show(this, "Please enter the OData Metadata URI.");
                return;
            }
            else if (this.comboBoxAuthenticationType.SelectedValue == null)
            {
                MessageBox.Show(this, "Please select an authentication type for the OData service.");
                return;
            }

            // Collect the information for creating the PDM model.
            string newModelName = this.textBoxModelName.Text;
            string oDataMetadataUri = this.textBoxODataMetadataURI.Text;
            PdODataModelUpdater.ODataAutenticationType oDataAuthenticationType = (PdODataModelUpdater.ODataAutenticationType)this.comboBoxAuthenticationType.SelectedValue;

            // Close the form.
            this.Close();

            // Create the PDM model.
            _pdDataModelUpdater.CreatePdmModelFromODataMetadata(newModelName, oDataMetadataUri, oDataAuthenticationType, false);
        }

        /// <summary>
        /// If the cancel button is pressed, close the form.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
