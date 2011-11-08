using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Threading;


namespace AutoGitClient
{
    public partial class frmBase : Form
    {
        private Form1 f1;

        delegate void SetListCallback();
        delegate void SetOutputCallback(string text);
        delegate void SetStartCommitLinkCallback(bool enabled);

        internal string basepath = "";
        internal bool dirty = false;
        internal string prevpath = "";
        internal DateTime prevdt = DateTime.Now;
        internal bool committing = false;
        internal bool finsuccess = true;

        internal string logstring = "";
        internal frmLogs fl;

        internal JobManager jobman;

        public frmBase()
        {
            InitializeComponent();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            this.f1 = new Form1();
            this.f1.parent = this;

            RegistryKey regkey;
            regkey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\AutoGitClient", false);
            if (regkey != null)
            {
                basepath = (string)regkey.GetValue("curdir", Environment.CurrentDirectory);
                regkey.Close();
            }
            else
            {
                basepath = Environment.CurrentDirectory;
            }

            regkey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\AutoGitClient", false);
            if (regkey != null)
            {
                Common.WorkerSleepingTime = (int)regkey.GetValue("WorkerSleepingTime", Common.WorkerSleepingTime);
                regkey.Close();
            }


            // Check whether StrictHostKeyChecking (on SSH) is set "no".
            string path = Common.GetParentPath(Environment.GetFolderPath(Environment.SpecialFolder.Personal)) + ".ssh\\config";
            string sl;
            bool foundentry = false;

            if (File.Exists(path) == true)
            {
                StreamReader sr = new StreamReader(path);
                while ((sl = sr.ReadLine()) != null)
                {
                    if (sl.Trim().ToLower() == "host git.fluxflex.com")
                    {
                        foundentry = true;
                        break;
                    }
                }
                sr.Close();
            }

            // If not set, set it "no".
            if (foundentry == false)
            {
                StreamWriter sw = new StreamWriter(path, true);

                sw.WriteLine("Host git.fluxflex.com");
                sw.WriteLine(" StrictHostKeyChecking no");

                sw.Close();
            }

            this.logstring = "";
            this.fl = new frmLogs();
            this.fsw.Path = basepath;
            f1.textBox1.Text = basepath;
            this.jobman = new JobManager(this, basepath);
        }

        private void Form2_Shown(object sender, EventArgs e)
        {
            this.Hide();
        }

        private void showToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (f1 == null || f1.disposed == true)
            {
                f1 = new Form1();
                f1.parent = this;
            }

            f1.Show();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }




        private void Form1_Load(object sender, EventArgs e)
        {

        }


        
        private void dirchanged(System.IO.FileSystemEventArgs e)
        {


            if (e.FullPath == prevpath && (DateTime.Now - prevdt).Seconds < 2)
            {
                return;
            }

            prevpath = e.FullPath;
            prevdt = DateTime.Now;
            this.tmrCommit.Enabled = true;
            this.RefreshList();
        }



        public void WriteLog(string str)
        {
            if (str.IndexOf("DEBUG:") >= 0)
            {
                // If the string to write to logs contains "DEBUG:", discard the line.
                return;
            }
            str = DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString() + " - " + str + "\r\n";
            this.SetOutput(f1.txtOutput.Text + str);

            this.logstring += str;
            this.fl.SetOutput(this.logstring);
            Debug.WriteLine(str);
        }

        private void SetOutput(string text)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (f1.txtOutput.InvokeRequired)
            {
                SetOutputCallback d = new SetOutputCallback(SetOutput);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                f1.txtOutput.Text = text;
            }
        }
        private void SetStartCommitLink(bool enabled)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (f1.linkLabel3.InvokeRequired)
            {
                SetStartCommitLinkCallback d = new SetStartCommitLinkCallback(SetStartCommitLink);
                this.Invoke(d, new object[] { enabled });
            }
            else
            {
                f1.linkLabel3.Enabled = enabled;
            }
        }
        
        private void tmrCommit_Tick(object sender, EventArgs e)
        {
            this.jobman.ExecuteAllJobs();
        }

        private void fsw_Changed(object sender, System.IO.FileSystemEventArgs e)
        {
            if (!e.FullPath.Contains(".git"))
            {
                this.jobman.AddJob(new Job(e.FullPath, ActionType.Changed, this.basepath));
                this.dirty = true;
                this.dirchanged(e);
            }
        }

        private void fsw_Created(object sender, System.IO.FileSystemEventArgs e)
        {
            if (!e.FullPath.Contains(".git"))
            {
                this.jobman.AddJob(new Job(e.FullPath, ActionType.Added, this.basepath));
                this.dirty = true;
                this.dirchanged(e);
            }
        }

        private void fsw_Deleted(object sender, System.IO.FileSystemEventArgs e)
        {
            if (!e.FullPath.Contains(".git"))
            {
                this.jobman.AddJob(new Job(e.FullPath, ActionType.Removed, this.basepath));
                this.dirty = true;
                this.dirchanged(e);
            }
        }

        private void fsw_Renamed(object sender, System.IO.RenamedEventArgs e)
        {
            if (!e.FullPath.Contains(".git"))
            {
                this.jobman.AddJob(new Job(e.FullPath, ActionType.Added, this.basepath));
                this.jobman.AddJob(new Job(e.OldFullPath, ActionType.Removed, this.basepath));
                this.dirty = true;
                this.dirchanged(e);
            }
        }

        private void Form2_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.notifyIcon1.Visible = false;

            RegistryKey regkey;
            regkey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\AutoGitClient", true);
            if (regkey == null)
            {
                regkey = Registry.CurrentUser.CreateSubKey("SOFTWARE\\AutoGitClient");
            }

            regkey.SetValue("curdir", basepath);
            regkey.Close();
        }

        internal void RefreshList()
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (f1.lvChanges.InvokeRequired)
            {
                SetListCallback d = new SetListCallback(RefreshList);
                this.Invoke(d);
            }
            else
            {

                int i;
                ListViewItem li;
                string[] ns = new string[3];

                f1.lvChanges.Items.Clear();

                JobList jl = this.jobman.GetAllJobs();

                for (i = 0; i < jl.Count; i++)
                {
                    switch (jl[i].Action)
                    {
                        case ActionType.Changed:
                            ns[0] = "Changed";
                            break;
                        case ActionType.Added:
                            ns[0] = "Added";
                            break;
                        case ActionType.Removed:
                            ns[0] = "Removed";
                            break;
                    }
                    ns[1] = jl[i].FullPath;
                    ns[2] = jl[i].Repo;
                    li = new ListViewItem(ns);
                    f1.lvChanges.Items.Add(li);
                }
            }
        }

    }
}
