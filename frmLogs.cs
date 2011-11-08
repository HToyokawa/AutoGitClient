using System;
using System.Windows.Forms;

namespace AutoGitClient
{
    public partial class frmLogs : Form
    {
        public delegate void SetOutputCallback(string text);
        internal bool disposed = false;

        internal void SetOutput(string text)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (this.txtOutput.InvokeRequired)
            {
                SetOutputCallback d = new SetOutputCallback(SetOutput);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.txtOutput.Text = text;
            }
        }

        public frmLogs()
        {
            InitializeComponent();
        }

        private void frmLogs_Load(object sender, EventArgs e)
        {
            this.disposed = false;
        }

        private void frmLogs_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.disposed = true;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            this.Hide();
        }
    }
}
