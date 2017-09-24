/************************************************************************/
/*                     Author：qcr                                      */
/************************************************************************/
using UnityEngine;
using System.IO;
public class ATDefine
{
    public const string ATDir = "AssistTools";

    static public readonly string Path_Project = Application.dataPath;
    static public readonly string Path_Tools = ATHelper_File.CorrectPath(Path.Combine(Path_Project, "..\\..\\Tools\\Tools"));
    static public readonly string Path_Home = ATHelper_File.FindDirInAT("Home").FullName;
    static public readonly string Path_Config = ATHelper_File.FindDirInAT("Config").FullName;
    static public readonly string Path_Log = ATHelper_File.FindDirInAT("Log").FullName;

    static public string Path_Assemblies = ATHelper_File.CorrectPath(Path.Combine(ATDefine.Path_Project, "../Library/ScriptAssemblies/"));

    public const char SplitChar = '|';
}
