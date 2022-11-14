using System;
using System.Windows.Forms;

namespace CrossBreeze.Tools.PowerDesigner.AddIn.OData.Forms
{
    public partial class PdODataReversePropertiesForm : Form
    {
        /// <summary>
        /// Public property to retrieve the model name.
        /// </summary>
        public string ModelName
        {
            get
            {
                return this.textBoxModelName.Text;
            }
        }

        /// <summary>
        /// Public property to retrieve the OData metadata uri.
        /// </summary>
        public string ODataMetadataUri
        {
            get
            {
                return this.textBoxODataMetadataURI.Text;
            }
        }

        /// <summary>
        /// Public property to retrieve the OData metadata uri.
        /// </summary>
        public PdODataModelUpdater.ODataAutenticationType AuthenticationType
        {
            get
            {
                return (PdODataModelUpdater.ODataAutenticationType)this.comboBoxAuthenticationType.SelectedValue;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public PdODataReversePropertiesForm()
        {
            InitializeComponent();

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
                MessageBox.Show(this, "Please enter the model name.");
                return;
            }
            else if(this.textBoxODataMetadataURI.Text == null || this.textBoxODataMetadataURI.Text.Length == 0)
            {
                MessageBox.Show(this, "Please enter the OData service metadata URI.");
                return;
            }
            else if (this.comboBoxAuthenticationType.SelectedValue == null)
            {
                MessageBox.Show(this, "Please select an authentication type for the OData service.");
                return;
            }

            // Close the form.
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        /// <summary>
        /// If the cancel button is pressed, close the form.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cancelButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
