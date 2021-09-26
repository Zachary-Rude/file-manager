using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace File_Manager
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog() { Description = "Select your path." })
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    fileBrowser.Url = new Uri(fbd.SelectedPath);
                    txtLocation.Text = fbd.SelectedPath;
                }
            }
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            if (fileBrowser.CanGoBack)
            {
                fileBrowser.GoBack();
                txtLocation.Text = fileBrowser.Url.ToString().Replace("file:///", "").Replace(@"/", @"\");
            }
        }

        private void btnForward_Click(object sender, EventArgs e)
        {
            if (fileBrowser.CanGoForward)
            {
                fileBrowser.GoForward();
                txtLocation.Text = fileBrowser.Url.ToString().Replace("file:///", "").Replace(@"/", @"\");
            }
        }

        private void goToFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnBrowse.PerformClick();
        }

        private void txtLocation_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyValue == 13)
            {
                fileBrowser.Url = new Uri(txtLocation.Text);
            }
        }
    }
}
