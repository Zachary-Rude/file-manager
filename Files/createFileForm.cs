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

namespace Files
{
    public partial class createFileForm : Form
    {
        public createFileForm()
        {
            InitializeComponent();
            string newFileName = txtFileName.Text;
        }

        private void btnCreateFile_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
