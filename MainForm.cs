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
    public partial class MainForm : Form
    {
        private CryptoPad cryptoPad;

        public MainForm()
        {
            InitializeComponent();
        }

        private void CryptoPadMain_Load(object sender, EventArgs e)
        {
            var dlg = new PasswordDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
                Login(dlg.Password);
            else
                Application.Exit();
        }

        private void Login(string password)
        {
            cryptoPad = new CryptoPad(password);
            cryptoPad.PadsLoaded += CryptoPadOnPadsLoaded;
            cryptoPad.LoadPads();
        }

        private void CryptoPadOnPadsLoaded(object sender, BindingList<CryptoPadPad> cryptoPadPads)
        {
            cryptoPadPadBindingSource.DataSource = cryptoPadPads;
            listBox1.DataSource = cryptoPadPadBindingSource;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            CryptoPadPad newPad = new CryptoPadPad("New Pad", cryptoPad);
            newPad.Data = "";
            cryptoPadPadBindingSource.Add(newPad);
            
        }
    }
}
