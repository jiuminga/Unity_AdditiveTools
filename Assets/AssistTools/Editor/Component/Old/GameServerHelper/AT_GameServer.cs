/************************************************************************/
/*                     Author：qcr                                      */
/************************************************************************/ 
using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using System.Xml;

public class AT_GameServer : AT_Component_Base
{
    private const string c_sConfig_ServerPath = "ServerPath";

    private static readonly string sr_sBatPath = Path.Combine(ATDefine.Path_Tools, "..\\Server");

    private string m_sServerPath = "";

    private static Process m_kProcess;

    public AT_GameServer() : base(10, ToolType.WorkFlow, "游戏服务器") { }

    public override void Init()
    {
        GetConfig<string>(c_sConfig_ServerPath, ref m_sServerPath);
    }

    public override void OnGUI()
    {
        if (ATGUILib.PathBar("选择你的服务器文件", ATHelper_File.CorrectPath(sr_sBatPath), ref m_sServerPath, "bat"))
        {
            SetConfig<string>(c_sConfig_ServerPath, m_sServerPath);
        }

        GUILayout.BeginHorizontal(EditorStyles.objectFieldThumb);
        if (m_kProcess != null && m_kProcess.HasExited) m_kProcess = null;

        if (ATHelper_File.IsFileExists(m_sServerPath, ".bat") && m_kProcess == null)
        {
            if (GUILayout.Button("启动", GUILayout.Width(50)))
            {
                m_kProcess = ATHelper_Process.RunBat(m_sServerPath);
            }
        }
        else GUILayout.Label("   启动", GUILayout.Width(50));

        if (m_kProcess != null)
        {
            if (GUILayout.Button("停止", GUILayout.Width(50)))
            {
                ATHelper_Process.KillProcessTree(m_kProcess);
            }
        }
        else GUILayout.Label("   停止", GUILayout.Width(50));
        GUILayout.EndHorizontal();
    }
}