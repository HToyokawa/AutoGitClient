using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading;

namespace AutoGitClient
{
    // Job = a git command (git add/rm/status/pull/clone <- excluding "git commit/push")
    // JobExecuter = a thread which execute the job
    //      "git commit/push" is executed after a series of "git add/rm" commands are executed.
    //      JobExecuter is assigned to each git repositories.
    // JobManager = a single object which holds JobExecuters and dispatches added jobs to proper JobExecuters.


    class JobManager
    {
        // Make 1 instance only!
        // It should be implemented as singleton.

        private JobExecuterList el;
        private frmBase parent;
        private string basepath;C:\Users\Toyokawa\Documents\VS.net\AutoGitClient\Common.cs

        public JobList GetAllJobs()
        {
            JobList jl = new JobList();

            int i;
            for (i = 0; i < el.Count; i++)
            {
                jl.AddRange(this.el[i].GetJobs());
            }

            return jl;
        }

        public JobManager(frmBase parent, string basepath)
        {
            this.el = new JobExecuterList();
            this.parent = parent;
            this.basepath = basepath;
        }

        public void AddJob(Job job)
        {
            int i;

            string path;

            if (job.Action == ActionType.Clone)
            {
                path = this.basepath;
            }
            else
            {
                path = Common.GetRepository(job.FullPath, this.basepath);
            }
            for (i = 0; i < this.el.Count; i++)
            {
                if(this.el[i].path == path){
                    this.el[i].AddJob(job);
                    break;
                }
            }
            if (i >= this.el.Count)
            {
                // Not found.

                JobExecuter je = new JobExecuter(path);
                je.git.parent = this.parent;
                this.el.Add(je);
                je.AddJob(job);
            }
        }

        public void ExecuteAllJobs()
        {
            // FLUSH!
            int i;
            for (i = 0; i < this.el.Count; i++)
            {
                this.el[i].StartExecute();
            }
        }
    }

    class JobExecuter
    {
        private JobList jobl;
        public string path;
        private bool executing = false;
        public GitManager git;

        public JobList GetJobs()
        {
            return jobl;
        }

        public JobExecuter(string path)
        {
            this.jobl = new JobList();
            this.git = new GitManager();
            this.path = path;
        }

        public void AddJob(Job job)
        {
            lock (jobl.SyncRoot)
            {
                jobl.Add(job);
            }
        }
        public void StartExecute()
        {
            // Start the 1st domino!!!
            if (executing == true)
                return;

            JobList targetl = new JobList();

            // TODO: Exclusive control is necessary.
            this.executing = true;

            Job job;
            while (this.jobl.Count > 0)
            {
                while (true)
                {
                    if (this.jobl.Count <= 0)
                        break;

                    job = this.jobl[0];

                    if (job.Action != ActionType.Added
                        && job.Action != ActionType.Changed
                        && job.Action != ActionType.Removed
                        && targetl.Count > 0)
                    {
                        break;
                    }

                    this.jobl.RemoveAt(0);
                    targetl.Add(job);
                }
                this.Execute(targetl);
                targetl.Clear();
            }

            // TODO: Exclusive control is necessary.
            this.executing = false;
        }

        private void Execute(JobList jobq)
        {
            List<string> tochange = new List<string>();
            List<string> toadd = new List<string>();
            List<string> toremove = new List<string>();
            GitJobParameters p = new GitJobParameters();
            
            p.tochange = tochange;
            p.toadd = toadd;
            p.toremove = toremove;
            p.gitpath = this.path;

            Job j;

            if (jobq[0].Action == ActionType.Added
                || jobq[0].Action == ActionType.Changed
                || jobq[0].Action == ActionType.Removed)
            {
                // git add/rm are packed into a series and git commit/push are executed after executing the git add/rm Jobs.
                while(jobq.Count > 0)
                {
                    j = jobq[0];
                    jobq.RemoveAt(0);
                    switch (j.Action)
                    {
                        case ActionType.Changed:
                            p.tochange.Add(j.FullPath);
                            break;
                        case ActionType.Added:
                            p.toadd.Add(j.FullPath);
                            break;
                        case ActionType.Removed:
                            p.toremove.Add(j.FullPath);
                            break;
                    }
                }

                this.git.StartCommitChanges(p);
            }
            else
            {
                // git status/clone/push is executed alone.

                p.tarpath = jobq[0].FullPath;
                this.git.StartGitActions(p, jobq[0].Action);
            }
        }
    }

    class JobList : ArrayList
    {
        public new Job this[int index]
        {
            get
            {
                return (Job)base[index];
            }
            set
            {
                base[index] = value;
            }
        }

        public int Add(Job value)
        {
            return base.Add(value);
        }
    }
    class JobExecuterList : ArrayList
    {
        public new JobExecuter this[int index]
        {
            get
            {
                return (JobExecuter)base[index];
            }
            set
            {
                base[index] = value;
            }
        }

        public int Add(JobExecuter value)
        {
            return base.Add(value);
        }
    }
    class Job{
        public string FullPath = "";
        public ActionType Action = ActionType.Changed;
        public string Repo = "";

        public Job(string fullpath, ActionType actiontype, string basepath)
        {
            this.FullPath = fullpath;
            this.Action = actiontype;
            this.Repo = Common.GetRepository(fullpath, basepath);
        }
    }

    enum ActionType
    {
        Added,
        Changed,
        Removed,
        Clone,
        Pull,
        Status
    }
}
