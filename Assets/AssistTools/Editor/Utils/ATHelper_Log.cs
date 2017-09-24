using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

static public class ATHelper_Log
{
    static public void Output(string sInfo, string sName = null)
    {
        sName = sName ?? DateTime.Now.ToString();
        string sPath = ATDefine.Path_Log + Path.DirectorySeparatorChar + sName;

        FileStream fs = File.Open(sPath, FileMode.OpenOrCreate);
        fs.Close();
        StreamWriter sw = new StreamWriter(sPath, false);
        sw.Write(sInfo);
        sw.Flush();
        sw.Close();
    }

}
