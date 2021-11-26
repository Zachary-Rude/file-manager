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
using System.Diagnostics;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Files
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            btnForward.Enabled = fileBrowser.CanGoForward;
            btnBack.Enabled = fileBrowser.CanGoBack;
            txtLocation.Text = @"C:\";
            fileBrowser.Url = new Uri(txtLocation.Text);
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            if (fileBrowser.CanGoBack)
            {
                fileBrowser.GoBack();
            }
        }

        private void btnForward_Click(object sender, EventArgs e)
        {
            if (fileBrowser.CanGoForward)
            {
                fileBrowser.GoForward();
            }
        }

        private void goToFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtLocation.Text = fbd.SelectedPath;
                    fileBrowser.Url = new Uri(fbd.SelectedPath);
                }
            }
        }

        private void txtLocation_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyValue == 13)
            {
                fileBrowser.Url = new Uri(txtLocation.Text);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void fileBrowser_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            txtLocation.Text = fileBrowser.Url.ToString().Replace("file:///", "").Replace(@"/", @"\");
            btnForward.Enabled = fileBrowser.CanGoForward;
            btnBack.Enabled = fileBrowser.CanGoBack;
        }

        private void openFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    Process.Start(ofd.FileName);
                }
            }
        }

        private void newFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DirectoryInfo di = Directory.CreateDirectory(txtLocation.Text + @"\New folder");
            fileBrowser.Refresh();
            expTree1.Refresh();
        }

        private void aboutFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutFilesForm aboutFilesForm = new AboutFilesForm();
            aboutFilesForm.ShowDialog();
        }

        private void backToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnBack.PerformClick();
        }

        private void forwardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnForward.PerformClick();
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog fileDialog = new OpenFileDialog())
            {
                if (fileDialog.ShowDialog() == DialogResult.OK)
                {
                    File.Copy(Path.GetFullPath(fileDialog.FileName), txtLocation.Text + @"\" + fileDialog.FileName);
                }
            }
        }

        private void newFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FileStream fs = File.Create(txtLocation.Text + @"\Untitled.txt");
            fileBrowser.Refresh();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void maximizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Maximized;
        }

        private void minimizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            fileBrowser.Refresh();
            expTree1.Refresh();
        }

        private void folderOptionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("rundll32.exe", "shell32.dll,Options_RunDLL 0");
            fileBrowser.Refresh();
        }

        private void copyPathToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txtLocation.Copy();
        }

        private void navigateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fileBrowser.Navigate(txtLocation.Text);
        }
    }
}
