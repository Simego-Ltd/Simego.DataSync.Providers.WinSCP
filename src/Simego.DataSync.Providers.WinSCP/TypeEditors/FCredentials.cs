using System;
using System.Windows.Forms;

namespace Simego.DataSync.Providers.WinSCP.TypeEditors
{
    public partial class FCredentials : Form
    {
        public string Username { get { return textUsername.Text; } set { textUsername.Text = value; } }
        public string Password { get { return textPassword.Text; } }
       
        public FCredentials()
        {
            InitializeComponent();
            Setup();
        }

        private void Setup()
        {                     
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
