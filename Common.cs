using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.IO;

namespace AutoGitClient
{
    static class Common
    {
        static public int WorkerSleepingTime;

        static public string GetPath(string fullpath)
        {
            return fullpath.Substring(0, fullpath.LastIndexOf("\\"));
        }
        static public string GetParentPath(string fullpath)
        {
            if (fullpath.Substring(fullpath.Length - 1) == "\\")
            {
                fullpath = fullpath.Substring(0, fullpath.Length - 1);
            }

            fullpath = fullpath.Substring(0, fullpath.LastIndexOf("\\"));

            if (fullpath.Substring(fullpath.Length - 1) != "\\")
            {
                fullpath += "\\";
            }
            return fullpath;
        }

        static public string GetRepository(string fullpath, string basepath)
        {
            if (fullpath == "")
            {
                return "";
            }

            string tmp;
            
            tmp = fullpath.Substring(basepath.Length);
            int l = tmp.IndexOf("\\");

            if(l < 0){
                // Not found.
                return basepath + tmp + "\\";
            }
            else{
                return basepath + tmp.Substring(0, l) + "\\";
            }
        }

        static public string GetNormalizedGitUrl(string url)
        {
            int l = url.IndexOf("@");

            if (l < 0)
                return url;
            else
                return "https://" + url.Substring(l + 1);
        }
        static public string GetValueFromGitConfig(string key, string repositorypath)
        {
            Hashtable hs = new Hashtable();
            string sl, k, val;

            try{
                StreamReader sr = new StreamReader(repositorypath + ".git\\config");
                
                while((sl = sr.ReadLine()) != null){
                    sl = sl.Trim();
                    if(sl.IndexOf("=") >= 0){
                        k = sl.Substring(0, sl.IndexOf("=") - 1).Trim();
                        
                        val = sl.Substring(sl.IndexOf("=") + 1).Trim();

                        hs.Add(k, val);
                    }
                }
            }
            catch{
            }

            return (string)hs[key];
        }
    }
}
