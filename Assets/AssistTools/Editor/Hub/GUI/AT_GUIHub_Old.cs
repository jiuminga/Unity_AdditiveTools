/************************************************************************/
/*                     Author：qcr                                      */
/************************************************************************/
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using System.Linq;

public class AT_GUIHub_Old:EditorWindow
{
    private const string m_sIcon = "CF.png";

    private List<AT_Component_Base> m_lsATs;
    private Dictionary<AT_Component_Base, int> m_dicATID;
    private Dictionary<AT_Component_Base, bool> m_dicATInited;
    private AT_Component_Base m_kCurProcessTool;
    private bool m_bInited;

    private static AT_GUIHub_Old m_kInstance;
    public static AT_GUIHub_Old Instance
    {
        get
        {
            if (m_kInstance == null)
                m_kInstance = EditorWindow.GetWindow<AT_GUIHub_Old>("辅助工具集");
            return m_kInstance;
        }
    }

    public AT_GUIHub_Old()
    {
        m_lsATs = new List<AT_Component_Base>();
        m_dicATID = new Dictionary<AT_Component_Base, int>();
        m_dicATInited = new Dictionary<AT_Component_Base, bool>();
        m_bInited = false;
    }

    #region Menu
    //[MenuItem("Windplay/辅助工具集 &T", false, 7)]
    static public void Menu_OpenWindow()
    {
        AT_GUIHub_Old window = EditorWindow.GetWindow<AT_GUIHub_Old>("辅助工具集");
        window.minSize = new Vector2(300, 200);
        window.Init();
        window.Show();
        m_kInstance = window;
    }
    #endregion

    #region  LifeCircle
    void Init()
    {
        titleContent = ATGUILib.GetGUIContent(ATHelper_File.FindFileInAT(m_sIcon).FullName, "辅助工具集");
        m_bInited = true;
        m_lsATs.Clear();

        m_dicATID.Clear();
        var types = ATHelper_Unity.Assembly_GameEditor.GetTypes().Where(t => t.IsSubclassOf(typeof(AT_Component_Base)));
        foreach (var t in types)
        {
            var tool = System.Activator.CreateInstance(t) as AT_Component_Base;
            m_lsATs.Add(tool);
            m_dicATID[tool] = ATGUILib.GenerateGroupID();
            m_dicATInited[tool] = false;
        }
        m_lsATs.Sort((a, b) => a.m_iSortValue.CompareTo(b.m_iSortValue));
    }

    void OnEnable()
    {
        if (!m_bInited) Init();
        foreach (var tool in m_lsATs)
        {
            if (ATGUILib.UIGroupExpanded(m_dicATID[tool]))
            {
                tool.OnEnable();
            }
        }
        EditorApplication.update += OnUpdate;
    }

    void OnDisable()
    {
        foreach (var tool in m_lsATs)
        {
            if (ATGUILib.UIGroupExpanded(m_dicATID[tool]))
            {
                tool.OnDisable();
            }
        }
        EditorApplication.update -= OnUpdate;
    }

    void OnUpdate()
    {
        foreach (var tool in m_lsATs)
        {
            tool.OnUpdate();
        }
    }

    #endregion

    #region GUI
    int m_iID_CardLayout = ATGUILib.GenerateGroupID();
    int m_iID_WorkFlow = ATGUILib.GenerateGroupID();
    int m_iID_GameDebugger = ATGUILib.GenerateGroupID();
    int m_iID_ExtendTools = ATGUILib.GenerateGroupID();

    void OnGUI()
    {
        if (GUILayout.Button("重新载入工具集"))
        {
            Init();
        }
        ATGUILib.CardLayout(m_iID_CardLayout, ATGUILib.LightOrange,
                            new ATGUILib.Card(m_iID_WorkFlow, "工作流", Draw_WorkFlowTools),
                            new ATGUILib.Card(m_iID_GameDebugger, "游戏调试", Draw_GameDebugerTools),
                            new ATGUILib.Card(m_iID_ExtendTools, "拓展工具", Draw_ExtendTools));
    }

    void Draw_WorkFlowTools() { Draw_Card(ToolType.WorkFlow); }
    void Draw_GameDebugerTools() { Draw_Card(ToolType.GameDebugger); }
    void Draw_ExtendTools() { Draw_Card(ToolType.Extend); }

    void Draw_Card(ToolType eToolType)
    {
        foreach (var tool in m_lsATs.Where(t => t.m_eToolType == eToolType))
        {
            m_kCurProcessTool = tool;
            ATGUILib.ExpandableUIGroup(m_dicATID[tool], tool.m_sToolName, OnToolExpand, OnToolChangeState, tool.m_bDirctOpen);
            GUILayout.Space(5);
        }
    }

    private void OnToolExpand()
    {
        if (!m_dicATInited[m_kCurProcessTool])
        {
            m_kCurProcessTool.Init();
            m_dicATInited[m_kCurProcessTool] = true;
        }
        m_kCurProcessTool.OnGUI();
    }

    private void OnToolChangeState(bool bCurState)
    {
        if (bCurState)
            m_kCurProcessTool.OnExpand();
        else m_kCurProcessTool.OnShrink();
    }
    #endregion
}