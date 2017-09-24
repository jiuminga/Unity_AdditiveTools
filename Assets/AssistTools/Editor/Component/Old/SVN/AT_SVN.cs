/************************************************************************/
/*                     Author：qcr                                      */
/************************************************************************/
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Linq;

public class AT_SVN : AT_Component_Base
{
    class PathDescribe
    {
        public string m_sPath;
        public string m_sDescribe;

        public PathDescribe(string sA, string sB)
        {
            m_sPath = sA;
            m_sDescribe = sB;
        }
    }

    private const string c_sConfig_UpdatePath = "UpdatePath";
    private const string c_sSplit = "#Split#";


    int m_iID_CardLayout = ATGUILib.GenerateGroupID();

    int m_iID_Update = ATGUILib.GenerateGroupID();
    int m_iID_Commit = ATGUILib.GenerateGroupID();
    int m_iID_Resolve = ATGUILib.GenerateGroupID();

    private Stack<int> m_stkRm = new Stack<int>();

    public AT_SVN() : base(-100, ToolType.WorkFlow, "版本控制(SVN)") { }

    private List<PathDescribe> m_lsUpdatePath = new List<PathDescribe>();

    #region LifeCircle
    public override void Init()
    {
        string sPath = "";
        GetConfig<string>(c_sConfig_UpdatePath, ref sPath);
        m_lsUpdatePath = SplitPath(sPath);
    }

    #endregion

    #region GUI
    public override void OnGUI()
    {
        ATGUILib.CardLayout(m_iID_CardLayout, ATGUILib.BrightBlue,
                     new ATGUILib.Card(m_iID_Update, "更新", Draw_SVNUpdate),
                     new ATGUILib.Card(m_iID_Commit, "提交", Draw_SVNCommit),
                     new ATGUILib.Card(m_iID_Resolve, "解决", Draw_SVNResolve));
    }

    private void Draw_SVNUpdate()
    {
        bool bChanged = false;
        m_stkRm.Clear();

        GUILayout.BeginVertical(EditorStyles.objectFieldThumb);
        for (int i = 0; i < m_lsUpdatePath.Count; ++i)
        {
            string s = m_lsUpdatePath[i].m_sPath;
            GUILayout.BeginVertical(EditorStyles.objectFieldThumb);

            GUILayout.BeginHorizontal();
            ATGUILib.ChangeableButton("更新", !string.IsNullOrEmpty(m_lsUpdatePath[i].m_sPath), a => SVNUpdate((string)a), m_lsUpdatePath[i].m_sPath, 40);
            EditorGUI.BeginChangeCheck();
            GUI.contentColor = ATGUILib.EnhanceColor(Color.green);
            m_lsUpdatePath[i].m_sDescribe = EditorGUILayout.TextField(m_lsUpdatePath[i].m_sDescribe);
            GUI.contentColor = Color.white;
            if (EditorGUI.EndChangeCheck()) bChanged = true;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (ATGUILib.PathBar("更新目录", ATHelper_File.CorrectPath(ATDefine.Path_Project + "/.."), ref s, "", 190, true, false))
            {
                m_lsUpdatePath[i].m_sPath = s;
                bChanged = true;
            }
            if (GUILayout.Button("-", EditorStyles.miniButton))
            {
                bChanged = true;
                m_stkRm.Push(i);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.Space(5);
        }

        for (int i = 0; i < m_stkRm.Count; ++i)
        {
            m_lsUpdatePath.RemoveAt(m_stkRm.Pop());
        }
        if (bChanged)
        {
            SetConfig<string>(c_sConfig_UpdatePath, CombinePath(m_lsUpdatePath));
        }
        GUILayout.Space(10);
        if (GUILayout.Button("新增地址"))
        {
            m_lsUpdatePath.Add(new PathDescribe("", "---设置更新目录备注---"));
        }
        GUILayout.EndVertical();

        EditorGUILayout.TextArea(sInfo);
    }

    private void Draw_SVNCommit()
    {

    }

    private void Draw_SVNResolve()
    {

    }
    #endregion

    #region Assist Func
    private List<PathDescribe> SplitPath(string sInfo)
    {
        return new List<PathDescribe>(sInfo.Split(';').Select(
            a =>
            {
                string s = a;
                string s1, s2;
                int i = s.IndexOf(c_sSplit);
                if (i >= 0) { s1 = s.Substring(0, i); s2 = s.Substring(i + c_sSplit.Length); }
                else { s1 = s; s2 = ""; }
                return new PathDescribe(s1, s2);
            }));
    }

    private string CombinePath(List<PathDescribe> lsPath)
    {
        string sRet = "";
        foreach (var p in lsPath)
        {
            sRet += (p.m_sPath + c_sSplit + p.m_sDescribe + ";");
        }
        if (sRet.Length > 0 && sRet[sRet.Length - 1] == ';')
            sRet = sRet.Substring(0, sRet.Length - 1);
        return sRet;
    }
    #endregion

    #region SVN Opreate
    Process kSvn;

    private void SVNUpdate(string sPath)
    {
        ProcessStartInfo kStartSvn = new ProcessStartInfo();
        kStartSvn.FileName = "TortoiseProc";
        kStartSvn.Arguments = "/command:update /path:" + sPath;
        kStartSvn.UseShellExecute = false;
        kStartSvn.CreateNoWindow = true;
        kStartSvn.RedirectStandardOutput = true;
        kSvn = new Process();
        kSvn.StartInfo = kStartSvn;
        kSvn.Start();
        kSvn.WaitForExit(1000);
        using (StreamReader sr = kSvn.StandardOutput)
        {
            String str = sr.ReadLine();
            while (null != str)
            {
                sInfo += str;
                str = sr.ReadLine();
            }
        }
        kSvn.Kill();
        new Thread(Log).Start();
    }

    public void Log()
    {
        try
        {
            while (kSvn != null && !kSvn.HasExited)
            {
                sInfo += kSvn.StandardOutput.ReadToEnd();
                Thread.Sleep(100);
            }
        }
        catch (System.Exception ex)
        {
            sInfo += ex.ToString();
        }
    }

    string sInfo = "";
    #endregion
}