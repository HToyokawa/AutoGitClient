using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace AutoGitClient
{
    static class Program
    {
        /// <summary>
        /// Main entry point
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmBase());
        }
    }
}
