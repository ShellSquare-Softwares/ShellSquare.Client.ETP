using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ShellSquare.Client.ETP
{
    public partial class TimeDepthBasedData : Form
    {
        public TimeDepthBasedData()
        {
            InitializeComponent();
        }
        private void DoneButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void DoneButton_MouseHover(object sender, EventArgs e)
        {
            DoneButton.BackColor = Color.FromArgb(50, 61, 78);
            DoneButton.ForeColor = Color.White;
        }

        private void DoneButton_MouseLeave(object sender, EventArgs e)
        {
            DoneButton.BackColor = Color.White;
            DoneButton.ForeColor = Color.Black;
        }

        private void TimeDepthBasedData_Load(object sender, EventArgs e)
        {
            PanelColumn.BackColor = Color.FromArgb(50, 61, 78);
            PanelColumn.ForeColor = Color.White;
        }
    }
}
