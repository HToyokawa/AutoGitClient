using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;

namespace AutoGitClient
{
    public class GitManager
    {
        public frmBase parent;
        public string gitcmd = "cmd"; //Application.StartupPath + @"\autogit.cmd";
        private bool finsuccess = false;
        private bool waiting = false;
        public int WorkerSleepingTime = 10000;

        internal void StatusAndRefresh(object o)
        {
            GitJobParameters prm = (GitJobParameters)o;
            string s;
            string targetdir = prm.gitpath;

            // Execute "git status"
            s = ExecGit("status", targetdir);

            // Analyze the returned string from "git status"
            // and add untracked files to the repository.
            byte[] byteArray = Encoding.ASCII.GetBytes(s);
            MemoryStream stream = new MemoryStream(byteArray);
            StreamReader sr = new StreamReader(stream);
            string sl, sl2;
            GitMessageSection sec = GitMessageSection.Other;
            int counter = 0;

            while ((sl = sr.ReadLine()) != null)
            {
                if (sl.Substring(0, 1) != "#")
                    continue;

                sl = sl.Substring(sl.IndexOf("#") + 1);
                sl = sl.Trim();

                sl = sl.Replace("/", "\\");

                switch (sec)
                {
                    case GitMessageSection.Other:

                        if (sl == "Changes not staged for commit:")
                        {
                            sec = GitMessageSection.ChangesNotStaged;
                            counter = 0;
                            break;
                        }

                        if (sl == "Untracked files:")
                        {
                            sec = GitMessageSection.Untracked;
                            counter = 0;
                            break;
                        }

                        break;

                    case GitMessageSection.ChangesNotStaged:
                        // skip the first 3 lines.

                        if (counter < 2)
                        {
                            counter++;
                        }
                        else
                        {
                            if (sl == "Untracked files:")
                            {
                                sec = GitMessageSection.Untracked;
                                counter = 0;
                                break;
                            }

                            // Substitute by commit -a
                        }
                        break;

                    case GitMessageSection.Untracked:
                        // skip the first 2 lines.

                        if (counter < 1)
                        {
                            counter++;
                        }
                        else
                        {
                            if (sl == "Changes not staged for commit:")
                            {
                                sec = GitMessageSection.ChangesNotStaged;
                                counter = 0;
                                break;
                            }

                            sl2 = sl.Substring(sl.IndexOf(":") + 1).Trim();
                                this.parent.jobman.AddJob(
                                    new Job((prm.gitpath + sl2).Replace("/", "\\"),
                                        ActionType.Added,
                                        Common.GetParentPath(prm.gitpath)));

                                Debug.WriteLine((prm.gitpath + sl2).Replace("/", "\\") + "\t" + Common.GetParentPath(prm.gitpath));
                        }
                        break;
                }
            }

            this.parent.RefreshList();
        }

        internal void PullFromRemote(object o)
        {
            GitJobParameters prm = (GitJobParameters)o;
            string s;
            string targetdir = prm.gitpath;

            s = ExecGit("pull", targetdir);
        }

        internal void CloneFromRemote(object o)
        {
            GitJobParameters prm = (GitJobParameters)o;
            string s;
            string targetdir = prm.gitpath;

            s = ExecGit("clone " + prm.tarpath, targetdir);
        }

        internal void CommitChanges(object o)
        {
            GitJobParameters prm = (GitJobParameters)o;
            string targetdir = prm.gitpath;

            string fn;
            string s;
            int i;
            for (i = 0; i < prm.toadd.Count; i++)
            {
                fn = prm.toadd[0];
                fn = fn.Replace("\\", "/");
                
                // Add new files/dirs to the repository.
                s = ExecGit("add \"" + fn + "\"", targetdir);
                prm.toadd.RemoveAt(0);
                parent.WriteLog(s);
            }

            for (i = 0; i < prm.toremove.Count; i++)
            {
                fn = prm.toremove[0];
                fn = fn.Replace("\\", "/");

                // Remove deleted files/dirs from the repository.
                s = ExecGit("rm \"" + fn + "\"", targetdir);
                prm.toremove.RemoveAt(0);
                parent.WriteLog(s);
            }

            for (i = 0; i < prm.tochange.Count; i++)
            {
                prm.tochange.RemoveAt(0);
            }

            // Commit all.
            s = ExecGit("commit -a -m \"Auto commit\"", targetdir);
            parent.WriteLog(s);

            // Push to the remote repository.
            s = ExecGit("push", targetdir);
            parent.WriteLog(s);
            
        }


        private string ExecGitInternal(string args, string targetdir, bool nowindow)
        {
            Environment.CurrentDirectory = targetdir;
            string command = gitcmd;

            ProcessStartInfo psInfo = new ProcessStartInfo();
            psInfo.WorkingDirectory = targetdir;

            psInfo.FileName = command; // 実行するファイル
            psInfo.CreateNoWindow = nowindow; // コンソール・ウィンドウを開かない

            psInfo.UseShellExecute = false; // シェル機能を使用しない
            //psInfo.Arguments = "git " + args;
            psInfo.Arguments = "/c git " + args;
            //psInfo.Arguments = "/c ping localhost";

            psInfo.RedirectStandardInput = true; // これをfalseにすると、余分なログが出る!
            psInfo.RedirectStandardOutput = true; // 標準出力をリダイレクト
            psInfo.RedirectStandardError = true;

            this.parent.WriteLog(psInfo.FileName + "_" + psInfo.Arguments);

            Process p = Process.Start(psInfo); // アプリの実行開始


            string output;

            StreamReader tr;
            tr = p.StandardOutput;

            waiting = true;
            Thread th = new Thread(new ParameterizedThreadStart(worker));
            th.Start(p);

            this.finsuccess = true;

            string strout, strerr;
            output = "";
            while (true)
            {
                // 標準出力の読み取り

                strerr = p.StandardError.ReadLine();
                strout = tr.ReadLine();
                waiting = false;

                if (strout != null)
                {
                    //strout = strout.Replace("\r\r\n", "\n"); // 改行コードの修正
                    if (strout.Length >= 2 && strout.Substring(strout.Length - 3, 2) != "  " && strout != strerr)
                    {
                        parent.WriteLog("stdout>" + strout);
                        output += strout + "\n";
                    }
                }

                if (strerr != null)
                {
                    //strerr = strerr.Replace("\r\r\n", "\n"); // 改行コードの修正
                    if (strerr.Length >= 2 && strerr.Substring(strerr.Length - 3, 2) != "  ")
                    {
                        parent.WriteLog("stderr>" + strerr);
                    }
                }
                if (strout == null && strerr == null)
                    break;
            }

            if (th.ThreadState == System.Threading.ThreadState.Running)
            {
                th.Abort();
                this.finsuccess = true;
            }
            return output;
        
        }
        private string ExecGit(string args, string targetdir)
        {
            return this.ExecGit(args, targetdir, true);
        }
        private string ExecGit(string args, string targetdir, bool nowindow)
        {
            string r = this.ExecGitInternal(args, targetdir, nowindow);

            if (this.finsuccess == false)
            {
                // Retry (showing cmd.exe window).
                r = this.ExecGitInternal(args, targetdir, false);

                if (this.finsuccess == false)
                {
                    MessageBox.Show("Connection time out.\r\nSomething is wrong!");
                }
            }

            parent.RefreshList();
            return r;
        }

        void worker(object obj)
        {
            Process p = (Process)obj;
            System.Threading.Thread.Sleep(WorkerSleepingTime);
            if (waiting == true && p.HasExited == false)
            {
                try
                {
                    parent.WriteLog("Kill process tree (" + p.Id + ") by trouble");
                    this.finsuccess = false;
                    ProcessUtility.KillTree(p.Id);
                }
                catch
                {
                }
            }
        }

        internal void StartCommitChanges(GitJobParameters p)
        {
            //this.SetStartCommitLink(false);
            //this.SetOutput("");
            Thread th = new Thread(new ParameterizedThreadStart(CommitChanges));
            th.Start(p);

            //this.RefreshList();
        }

        internal void StartGitActions(GitJobParameters p, ActionType action)
        {
            Thread th = null;
            switch (action)
            {
                case ActionType.Clone:
                    th = new Thread(new ParameterizedThreadStart(CloneFromRemote));
                    break;
                case ActionType.Pull:
                    th = new Thread(new ParameterizedThreadStart(PullFromRemote));
                    break;
                case ActionType.Status:
                    th = new Thread(new ParameterizedThreadStart(StatusAndRefresh));
                    break;
            }

            th.Start(p);
        }
        
    }


    class GitJobParameters
    {
        public string gitpath;
        public List<string> toadd;
        public List<string> tochange;
        public List<string> toremove;
        public string tarpath;
    }

    enum GitMessageSection
    {
        Other,
        ChangesNotStaged,
        Untracked
    }
}
