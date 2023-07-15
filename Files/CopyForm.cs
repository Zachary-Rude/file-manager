using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Files
{
    public partial class CopyForm : Form
    {
        public CopyForm()
        {
            InitializeComponent();
        }

        public void Copy(FileInfo _source, FileInfo _destination)
        {
            if (!this.IsHandleCreated)
            {
                this.CreateControl();
            }
            Task.Run(() =>
            {
                _source.CopyTo(_destination, x => progressBar1.BeginInvoke(new Action(() => { progressBar1.Value = x; lbProgress.Text = x.ToString() + "%"; })));
            }).GetAwaiter().OnCompleted(() => this.BeginInvoke(new Action(() => { this.Close(); })));
        }
    }
}
