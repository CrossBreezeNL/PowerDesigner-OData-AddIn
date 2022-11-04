using System;
using System.Drawing;
using System.Windows.Forms;

namespace CrossBreeze.Tools.PowerDesigner.AddIn.OData
{
    public class InputBoxHelper
    {
        const int DIALOG_WIDTH = 800;
        const int DIALOG_MARGIN = 15;
        const int LABEL_HEIGHT = 60;
        const int TEXTBOX_HEIGHT = 60;
        const int BUTTON_WIDTH = 160;
        const int BUTTON_HEIGHT = 60;

        public static DialogResult InputBox(IWin32Window ownerWindow, string title, string promptText, ref string value)
        {
            Form form = new Form();
            form.Text = title;
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimizeBox = false;
            form.MaximizeBox = false;

            int fieldWidth = DIALOG_WIDTH - (DIALOG_MARGIN * 2);

            Label label = new Label();
            label.Text = promptText;
            label.SetBounds(DIALOG_MARGIN, DIALOG_MARGIN, fieldWidth, LABEL_HEIGHT);
            label.AutoSize = true;

            TextBox textBox = new TextBox();
            textBox.TabIndex = 0;
            textBox.SetBounds(DIALOG_MARGIN, DIALOG_MARGIN + LABEL_HEIGHT, fieldWidth, TEXTBOX_HEIGHT);

            int buttonRowY = DIALOG_MARGIN + LABEL_HEIGHT + TEXTBOX_HEIGHT;
            Button buttonOk = new Button();
            buttonOk.Text = "OK";
            buttonOk.TabIndex = 1;
            buttonOk.DialogResult = DialogResult.OK;
            buttonOk.SetBounds(((fieldWidth / 2) - BUTTON_WIDTH), buttonRowY, BUTTON_WIDTH, BUTTON_HEIGHT);

            Button buttonCancel = new Button();
            buttonCancel.Text = "Cancel";
            buttonOk.TabIndex = 2;
            buttonCancel.DialogResult = DialogResult.Cancel;
            buttonCancel.SetBounds(((fieldWidth / 2) + DIALOG_MARGIN), buttonRowY, BUTTON_WIDTH, BUTTON_HEIGHT);

            form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel });
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;

            form.ClientSize = new Size(DIALOG_WIDTH, buttonRowY + BUTTON_HEIGHT + DIALOG_MARGIN);

            DialogResult result = form.ShowDialog(ownerWindow);
            value = textBox.Text;
            form.Close();
            return result;
        }
    }
}
