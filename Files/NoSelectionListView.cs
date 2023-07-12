using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Files
{
    public partial class NoSelectionListView : ListView
    {
        public NoSelectionListView()
        {
            InitializeComponent();
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);
        }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0007) //WM_SETFOCUS
            {
                this.LabelEdit = false;
                return;
            }
            base.WndProc(ref m);
        }
    }
}
