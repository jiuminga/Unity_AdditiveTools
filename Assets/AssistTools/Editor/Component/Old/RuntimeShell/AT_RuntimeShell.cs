/************************************************************************/
/*                     Author：qcr                                      */
/************************************************************************/
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
//using System.Diagnostics;
using System.CodeDom.Compiler;
using Microsoft.CSharp;

public class AT_RuntimeShell : AT_Component_Base
{
    public AT_RuntimeShell() : base(3, ToolType.Extend, "Runtime Shell") { }

    private string m_sInfo = string.Empty;
    private CompilerResults m_kCompileResult = null;
    private StringBuilder m_kSB;

    private static string Path_Assemblies;
    private static string Path_Assemblies_Run = ATHelper_File.CorrectPath(Path.Combine(ATDefine.Path_Project, "../Temp/UnityVS_bin/Debug/"));
    //private static string Path_Assemblies_Run = ATUtils.CorrectPath(Path.Combine(ATDefine.Path_Project, "../Library/ScriptAssemblies/"));
    private static string Path_Unity;
    private const string InnerPath_Managed = "Data/Managed/";

    private const string Config_UnityPath = "UnityPath";

    private string c_sPandNameSpace = @"
                    using UnityEngine;
                    using UnityEditor;
                    using System;
                    using System.Collections.Generic;
                    using System.Reflection;
                    using System.Text;
                    using System.IO;";

    private string c_sPandClass = @"
                    class AT_Runtime{
                        public static void Func(){
        Debug.LogError(Assembly.GetAssembly(typeof(ClientAPP)).Location);
";


    private string c_sPandTail = @"
                    }}";

    public override void Init()
    {
        m_kSB = new StringBuilder(2000);
        if (GetConfig<string>(Config_UnityPath, ref Path_Unity))
        {
            Path_Assemblies = ATHelper_File.CorrectPath(Path.Combine(Path_Unity, InnerPath_Managed));
        }
    }

    public override void OnGUI()
    {
        if (string.IsNullOrEmpty(Path_Unity) || !new DirectoryInfo(Path_Assemblies).Exists)
        {
            string sPath = "";
            if (ATGUILib.PathBar("设置Unity路径", "", ref sPath, "", 130, true))
            {
                if (sPath.EndsWith("Editor"))
                {
                    Path_Assemblies = ATHelper_File.CorrectPath(Path.Combine(sPath, InnerPath_Managed));
                    if (new DirectoryInfo(Path_Assemblies).Exists)
                    {
                        Path_Unity = sPath;
                        SetConfig<string>(Config_UnityPath, sPath);
                    }
                    else Path_Assemblies = "";
                }
            }
            return;
        }


        GUI.backgroundColor = ATGUILib.EnhanceColor(ATGUILib.LightBlue);
        EditorGUI.BeginChangeCheck();
        m_sInfo = EditorGUILayout.TextArea(m_sInfo, GUILayout.Height(400));
        GUI.backgroundColor = Color.white;
        if (EditorGUI.EndChangeCheck())
        {
            m_kCompileResult = null;

        }

        GUILayout.Space(10);

        EditorGUILayout.TextArea(GetInfo(), GUILayout.Height(200));

        GUILayout.Space(10);

        GUILayout.BeginHorizontal();
        ATGUILib.ChangeableButton("编译", m_kCompileResult == null, Complile);
        ATGUILib.ChangeableButton("运行", m_kCompileResult != null && !m_kCompileResult.Errors.HasErrors, Run);
        GUILayout.EndHorizontal();
    }

    public void Complile(object obj)
    {
        using (var provider = new CSharpCodeProvider())
        {
            var options = new CompilerParameters();
            options.GenerateInMemory = true;
            options.ReferencedAssemblies.Add(Path_Assemblies + "UnityEditor.dll");
            options.ReferencedAssemblies.Add(Path_Assemblies + "UnityEngine.dll");
            options.ReferencedAssemblies.Add(Path_Assemblies_Run + "Assembly-CSharp.dll");
            //options.ReferencedAssemblies.Add(@"C:\Project_Work\CF1\Client\Library\ScriptAssemblies\Assembly-CSharp.dll");
            //options.ReferencedAssemblies.Add(Path_Assemblies_Run + "Assembly-CSharp-Editor.dll");

            m_kSB.Length = 0;
            m_kSB.Append(c_sPandNameSpace);
            m_kSB.Append(c_sPandClass);
            m_kSB.Append(m_sInfo);
            m_kSB.Append(c_sPandTail);
            m_kCompileResult = provider.CompileAssemblyFromSource(options, m_kSB.ToString());
        }
    }

    public void Run(object obj)
    {
        Debug.LogError(Assembly.GetAssembly(typeof(Debug)).Location);
        Type t = m_kCompileResult.CompiledAssembly.GetType("AT_Runtime");
        t.InvokeMember("Func", BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.Public, null, null, null);
    }

    private string GetInfo()
    {
        if (m_kCompileResult != null && m_kCompileResult.Errors.HasErrors)
        {
            m_kSB.Length = 0;
            foreach (CompilerError e in m_kCompileResult.Errors)
            {
                m_kSB.AppendFormat("{0,-10} | {1} \n", e.Line, e.ErrorText);
            }
            return m_kSB.ToString();
        }
        return string.Empty;
    }
}