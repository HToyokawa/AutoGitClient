using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace AutoGitClient
{
    public partial class Form1 : Form
    {
        internal bool disposed = false;
        internal frmBase parent;
        
        public Form1()
        {
            InitializeComponent();
        }

        private void showToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Show();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.disposed = true;
        }

        private void linkLabel3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            /*GitJobParameters p = new GitJobParameters();
            p.toadd = parent.toadd;
            p.tochange = parent.tochange;
            p.toremove = parent.toremove;
            p.dir = parent.curdir;*/
            
            parent.jobman.ExecuteAllJobs();
            //parent.git.StartCommitChanges(p);
        }

        private void linkLabel2_LinkClicked_1(object sender, LinkLabelLinkClickedEventArgs e)
        {
            this.Hide();
        }

        private void linkLabel4_LinkClicked_1(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (parent.fl.disposed == true)
                parent.fl = new frmLogs();

            parent.fl.txtOutput.Text = parent.logstring;

            parent.fl.Show();
        }

        private void linkLabel1_LinkClicked_1(object sender, LinkLabelLinkClickedEventArgs e)
        {
            FolderBrowserDialog fd = new FolderBrowserDialog();
            fd.Description = "Select a `parent' folder of projects.\r\nRepository folders are located under the folder you select via this window.";
            fd.SelectedPath = parent.basepath;
            if (fd.ShowDialog() == DialogResult.OK)
            {
                /*GitJobParameters p = new GitJobParameters();
                p.dir = parent.curdir;
                p.toadd = parent.toadd;
                p.tochange = parent.tochange;
                p.toremove = parent.toremove;*/

                parent.jobman.ExecuteAllJobs();
                
                //parent.git.StartCommitChanges(p);
                parent.basepath = fd.SelectedPath;
                if (parent.basepath.Substring(parent.basepath.Length - 1) != "\\")
                    parent.basepath += "\\";
                parent.fsw.Path = parent.basepath;
                this.textBox1.Text = parent.basepath;

                parent.jobman = new JobManager(parent, parent.basepath);
                parent.RefreshList();
            }
        }

        private void linkLabel6_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            int i;
            string[] d = Directory.GetDirectories(this.parent.basepath);

            for (i = 0; i < d.Length; i++)
            {
                if(d[i].Substring(d[i].Length - 1) != "\\")
                    d[i] += "\\";

                if (Directory.Exists(d[i] + ".git"))
                {
                    parent.jobman.AddJob(new Job(d[i], ActionType.Status, this.parent.basepath));
                }
            }
            parent.jobman.ExecuteAllJobs();
        }

        private void linkLabel7_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            int i;
            string[] d = Directory.GetDirectories(this.parent.basepath);

            for (i = 0; i < d.Length; i++)
            {
                if (d[i].Substring(d[i].Length - 1) != "\\")
                    d[i] += "\\";

                if (Directory.Exists(d[i] + ".git"))
                {
                    parent.jobman.AddJob(new Job(d[i], ActionType.Pull, this.parent.basepath));
                }
            }
            parent.jobman.ExecuteAllJobs();
        }

        private void linkLabel5_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            frmInput fi = new frmInput();
            if (fi.ShowDialog() == DialogResult.OK)
            {
                parent.jobman.AddJob(new Job(fi.input, ActionType.Clone, this.parent.basepath));
                parent.jobman.ExecuteAllJobs();
            }
        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void linkLabel8_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            frmInput fi = new frmInput();
            string apikey, apikeyenc;

            RegistryKey regkey;
            regkey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\AutoGitClient", false);
            if (regkey != null)
            {
                apikeyenc = (string)regkey.GetValue("apikey", "");
                regkey.Close();

                if (apikeyenc != "")
                {
                    apikey = StringEnc.DecryptString(apikeyenc, StringEnc.TransStr());
                }
                else
                {
                    apikey = "";
                }
            }
            else
            {
                apikey = "";
            }

            fi.input = apikey;

            if (fi.ShowDialog() == DialogResult.Cancel)
            {
                return;
            }

            apikey = fi.input;

            regkey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\AutoGitClient", true);
            if (regkey == null)
            {
                regkey = Registry.CurrentUser.CreateSubKey("SOFTWARE\\AutoGitClient");
            }

            apikeyenc = StringEnc.EncryptString(apikey, StringEnc.TransStr());

            regkey.SetValue("apikey", apikeyenc);
            regkey.Close();

            // Set character code as UTF-8
            System.Text.Encoding enc =
                System.Text.Encoding.GetEncoding("utf-8");

            // Make string to send as POST
            string postData =
                "inlang=ja&word=" +
                    System.Web.HttpUtility.UrlEncode("api_key:" + apikey, enc);
            byte[] postDataBytes = System.Text.Encoding.UTF8.GetBytes(postData);

            // Make an WebRequest
            System.Net.WebRequest req =
                System.Net.WebRequest.Create("http://mydic.fluxflex.com/");
            
            // Set the WebRequest
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
            req.ContentLength = postDataBytes.Length;

            // Get a stream to send POST data
            System.IO.Stream reqStream = req.GetRequestStream();

            // Send data
            reqStream.Write(postDataBytes, 0, postDataBytes.Length);
            reqStream.Close();

            // Make an WebResponse and receive data from the server.
            System.Net.WebResponse res = req.GetResponse();
            System.IO.Stream resStream = res.GetResponseStream();
            System.IO.StreamReader sr = new System.IO.StreamReader(resStream, enc);
            
            int i;
            string s = sr.ReadToEnd();
            Console.WriteLine(sr.ReadToEnd());
            sr.Close();

            char[] splitter = new char[]{','};

            string[] strs = s.Split(splitter);
            for (i = 0; i < strs.Length; i++)
            {
                parent.jobman.AddJob(new Job(strs[i], ActionType.Clone, this.parent.basepath));
            }

            parent.jobman.ExecuteAllJobs();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

    }
}
