using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DeskCryptoPad
{
    public partial class PasswordDialog : Form
    {
        public PasswordDialog()
        {
            InitializeComponent();
            AcceptButton = okButton;
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            Password = passwordBox.Text;
            DialogResult = DialogResult.OK;
        }

        public string Password { get; set; }
    }
}
