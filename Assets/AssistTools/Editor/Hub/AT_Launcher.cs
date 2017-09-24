using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using UnityEditor;
using UnityEngine;

public class AT_Launcher : EditorWindow
{
    private const string m_sIcon = "CF.png";
    private const string c_sUseGUI = "UseGUI";
    private const string c_sPreference = "Perference";
    private XmlDocument Config;

    public readonly List<AT_Component_Base> m_lsATs;
    private AT_Component_Base m_kCurProcessTool;
    private AT_Handler m_kHandler;
    private bool m_bInited;

    private static AT_Launcher m_kInstance;
    public static AT_Launcher Instance
    {
        get
        {
            if (m_kInstance == null)
                m_kInstance = EditorWindow.GetWindow<AT_Launcher>("辅助工具集");
            return m_kInstance;
        }
    }

    public AT_Launcher()
    {
        m_lsATs = new List<AT_Component_Base>();
        m_bInited = false;
    }

    #region Menu
    [MenuItem("Windplay/辅助工具集 &T", false, 7)]
    static public void Menu_OpenWindow()
    {
        AT_Launcher window = EditorWindow.GetWindow<AT_Launcher>("辅助工具集");

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

        var types = ATHelper_Unity.Assembly_GameEditor.GetTypes().Where(t => t.IsSubclassOf(typeof(AT_Component_Base)));
        foreach (var t in types)
        {
            var tool = System.Activator.CreateInstance(t) as AT_Component_Base;
            m_lsATs.Add(tool);
        }
        m_lsATs.Sort((a, b) => a.m_iSortValue.CompareTo(b.m_iSortValue));

        Config = ATHelper_Xml.OpenXml(c_sPreference);
        bool bUseGUI = false;
        Config.GetConfig(c_sUseGUI, ref bUseGUI);
        UseGUI = bUseGUI;
        minSize = new Vector2(300, 200);
    }

    void OnEnable()
    {
        if (!m_bInited) Init();
        EditorApplication.update -= OnUpdate;
        EditorApplication.update += OnUpdate;
        m_kHandler.OnEnable();
    }

    void OnDisable()
    {
        EditorApplication.update -= OnUpdate;
        m_kHandler.OnDisable();
    }

    void OnUpdate()
    {
        m_kHandler.OnUpdate();
    }
    #endregion

    #region Func
    public bool UseGUI
    {
        set
        {
            if (m_kHandler != null && m_kHandler is AT_GUIHub == value) return;
            if (m_kHandler != null) m_kHandler.OnChangeMode();
            m_kHandler = value ? (AT_Handler)new AT_GUIHub() : new AT_Shell();
            m_kHandler.Init();
            Config.SetConfig(c_sUseGUI, value);
        }
    }
    #endregion
}
