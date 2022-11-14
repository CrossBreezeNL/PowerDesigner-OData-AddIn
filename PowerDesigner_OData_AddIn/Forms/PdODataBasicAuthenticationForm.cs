using System;
using System.Windows.Forms;

namespace CrossBreeze.Tools.PowerDesigner.AddIn.OData.Forms
{
    public partial class PdODataBasicAuthenticationForm : Form
    {
        /// <summary>
        /// Property with getter for the Username.
        /// </summary>
        public string Username
        {
            get
            {
                return this.textBoxUsername.Text;
            }
        }

        /// <summary>
        /// Property with getter for the Password.
        /// </summary>
        public string Password
        {
            get
            {
                return this.textBoxPassword.Text;
            }
        }

        public PdODataBasicAuthenticationForm()
        {
            InitializeComponent();
        }

        private void authenticateButton_Click(object sender, EventArgs e)
        {
            // Validate the contents of the text and combo boxes.
            if (this.textBoxUsername.Text == null || this.textBoxUsername.Text.Length == 0)
            {
                MessageBox.Show(this, "Please enter a username.");
                return;
            }
            else if (this.textBoxPassword.Text == null || this.textBoxPassword.Text.Length == 0)
            {
                MessageBox.Show(this, "Please enter a password.");
                return;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        /// <summary>
        /// If the cancel button is pressed, close the window.
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
