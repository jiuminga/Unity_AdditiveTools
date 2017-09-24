using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

static public class ATHelper_Process
{
    static public Process RunBat(string batPath)
    {
        Process pro = new Process();
        FileInfo file = new FileInfo(batPath);
        pro.StartInfo.WorkingDirectory = file.Directory.FullName;
        pro.StartInfo.FileName = batPath;
        pro.StartInfo.CreateNoWindow = false;
        pro.Start();
        return pro;
    }

    static public void OpenUrl(string url)
    {
        Process.Start(url);
    }

    static public void KillProcessTree(Process rootProcess)
    {
        Process.Start("CMD.exe", "/C taskkill /PID " + rootProcess.Id + " /T");
    }
}
