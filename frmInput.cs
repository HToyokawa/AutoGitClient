using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace AutoGitClient
{
    public partial class frmInput : Form
    {
        public string input;

        public frmInput()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.input = this.txtInput.Text;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

        private void frmInput_Load(object sender, EventArgs e)
        {
            this.txtInput.Text = input;
        }
    }
}
