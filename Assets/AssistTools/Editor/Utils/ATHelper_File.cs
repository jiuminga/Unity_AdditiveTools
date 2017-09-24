using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;


static public class ATHelper_File
{
    static private DirectoryInfo ATDir;

    static public List<FileInfo> GetFilesInDir(string sDir, string sExtendName = "")
    {
        DirectoryInfo dir = new DirectoryInfo(sDir);
        if (dir.Exists)
        {
            return new List<FileInfo>(dir.GetFiles().Where(a => a.Name.Contains(sExtendName)));
        }
        else UnityEngine.Debug.LogError("Dir Not Exists");
        return new List<FileInfo>();
    }

    static public FileInfo FindFileInAT(string sFile)
    {
        if (ATDir == null)
            ATDir = FindDir(ATDefine.ATDir);
        return FindFile(sFile, ATDir);
    }

    static public DirectoryInfo FindDirInAT(string sFile)
    {
        if (ATDir == null)
            ATDir = FindDir(ATDefine.ATDir);
        return FindDir(sFile, ATDir);
    }

    static public bool IsFileExists(string sFilePath, string sExtendName = "")
    {
        if (string.IsNullOrEmpty(sFilePath)) return false;
        if (!sFilePath.Contains(sExtendName)) return false;
        FileInfo e = new FileInfo(sFilePath);
        return e.Exists;
    }

    static public DirectoryInfo FindDir(string s, DirectoryInfo SrcDir = null)
    {
        if (SrcDir == null)
            SrcDir = new DirectoryInfo(Environment.CurrentDirectory);
        if (SrcDir.Exists)
        {
            var lsDir = SrcDir.GetDirectories();
            var dir = lsDir.Where(f => f.Name == s).FirstOrDefault();
            if (dir != null) return dir;
            foreach (var temp in lsDir)
            {
                dir = FindDir(s, temp);
                if (dir != null) return dir;
            }
        }
        return null;
    }

    static public FileInfo FindFile(string s, DirectoryInfo SrcDir = null)
    {
        if (SrcDir == null)
            SrcDir = new DirectoryInfo(Environment.CurrentDirectory);
        if (SrcDir.Exists)
        {
            var file = SrcDir.GetFiles().Where(f => f.Name == s).FirstOrDefault();
            if (file != null) return file;
            var lsDir = SrcDir.GetDirectories();
            foreach (var temp in lsDir)
            {
                file = FindFile(s, temp);
                if (file != null) return file;
            }
        }
        return null;
    }

    static public string CorrectPath(string srcPath)
    {
        var sb = new StringBuilder(100);
        sb.Append(srcPath);
        sb.Replace(@"\", "/");
        //string sRet = "";
        List<string> ls = new List<string>(sb.ToString().Split('/'));
        sb.Length = 0;
        List<string> lsF = new List<string>();
        for (int i = 0; i < ls.Count; ++i)
        {
            if (ls[i] == "..")
            {
                lsF.RemoveAt(lsF.Count - 1);
            }
            else lsF.Add(ls[i]);
        }
        for (int i = 0; i < lsF.Count; ++i)
        {
            sb.Append(lsF[i]).Append("/");
        }
        sb.Remove(sb.Length - 1, 1); //= sRet.Substring(0, sRet.Length - 1);
        //sb.Replace(":/", "://");
        return sb.ToString();
    }
}
