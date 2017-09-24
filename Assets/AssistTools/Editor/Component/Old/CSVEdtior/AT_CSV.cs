/************************************************************************/
/*                     Author：qcr                                      */
/************************************************************************/
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Linq;

public class AT_CSV : AT_Component_Base
{
    static public readonly string Path_CSV = ATDefine.Path_Tools + "\\CSV\\CSV";
    static public readonly string c_sBat_UpdateDB = ATDefine.Path_Tools + "\\CSV\\2 CSV导入Sqlite.bat";

    public List<string> m_lsCSVPath = new List<string>() { Path_CSV };
    public List<FileInfo> m_lsCSVFile = new List<FileInfo>();
    public List<int> m_lsSelectedCSV = new List<int>();
    private int m_id_SearchBar = ATGUILib.GenerateGroupID();

    public AT_CSV() : base(0, ToolType.WorkFlow, "CSV编辑") { }

    public override void Init()
    {
        foreach (var path in m_lsCSVPath)
        {
            m_lsCSVFile = ATHelper_File.GetFilesInDir(path, ".csv");
            int i = 0;
            m_lsSelectedCSV.AddRange(m_lsCSVFile.Select(a => i++));
                //ATUtils.TransIEnum<int>(m_lsCSVFile, a => i++));
        }
    }

    public override void OnGUI()
    {
        GUILayout.Space(5);
        GUILayout.BeginHorizontal(EditorStyles.objectFieldThumb);
        if (GUILayout.Button("更新DB", GUILayout.Width(70)))
        {
            UpdateDB();
        }
        GUILayout.Space(5);
        ATGUILib.SearchBar(m_id_SearchBar, _searchFunc);
        GUILayout.EndHorizontal();
        GUILayout.Space(5);

        if (m_lsSelectedCSV.Count == 0) return;
        ATGUILib.GridLayout(m_lsSelectedCSV.Count, 120, 10, 5, (a, b, c) =>
        {
            int index = m_lsSelectedCSV[a];
            string sName = m_lsCSVFile[index].Name;
            ATGUILib.EasyButton(sName.Substring(0, sName.Length - 4), _openCSV, index, EditorStyles.boldLabel, c);
        });
    }

    private void _searchFunc(string sInfo)
    {
        m_lsSelectedCSV.Clear();
        for (int i = 0; i < m_lsCSVFile.Count; ++i)
        {
            string sName = m_lsCSVFile[i].Name;
            if (sName.Substring(0, sName.Length - 4).IndexOf(sInfo, StringComparison.OrdinalIgnoreCase) >= 0)
                m_lsSelectedCSV.Add(i);
        }
    }

    private void _openCSV(object k)
    {
        int index = (int)k;
        Process.Start(m_lsCSVFile[index].FullName);
    }

    public static void UpdateDB()
    {
        if (!File.Exists(c_sBat_UpdateDB))
        {
            UnityEngine.Debug.LogError("Target Path Is Not Exists");
            return;
        }
        ATHelper_Process.RunBat(c_sBat_UpdateDB);
    }
}