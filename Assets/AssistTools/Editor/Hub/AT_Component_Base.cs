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
using System.Diagnostics;
using System.Linq;
using System.Collections;
using System.Xml;

public enum ToolType
{
    WorkFlow,
    GameDebugger,
    Extend,
}

public abstract class AT_Component_Base
{
    // 排序时，值越小在越上面
    public readonly int m_iSortValue = 0;
    public readonly ToolType m_eToolType = ToolType.Extend;
    public readonly string m_sToolName = string.Empty;
    public readonly bool m_bDirctOpen = false;

    public const string c_sType = "Type";
    public const string c_sValue = "Value";

    protected XmlDocument Config;
    private string m_sConfigPath;

    protected AT_Component_Base(int iSortValue, ToolType eToolType, string sToolName, bool bDirectOpen = false)
    {
        m_iSortValue = iSortValue;
        m_eToolType = eToolType;
        m_sToolName = sToolName;
        m_bDirctOpen = bDirectOpen;
    }

    protected AT_Component_Base()
    {
    }
    #region Shell
    public virtual string[] RigisterCommands { get { return null; } }
    #endregion

    #region Setting
    private void OpenConfig()
    {
        string sModuleName = GetType().Name;
        XmlDocument xmlDoc = new XmlDocument();
        try
        {
            var kFile = ATHelper_File.FindFileInAT(sModuleName + ".xml");
            if (kFile != null)
            {
                m_sConfigPath = kFile.FullName;
                xmlDoc.Load(m_sConfigPath);
            }
            else
            {
                kFile = ATHelper_File.FindFileInAT(sModuleName + ".cs");
                m_sConfigPath = Path.Combine(kFile.DirectoryName, sModuleName + ".xml");
                XmlTextWriter xmlWriter;
                xmlWriter = new XmlTextWriter(m_sConfigPath, Encoding.Unicode);
                xmlWriter.Formatting = Formatting.Indented;
                xmlWriter.WriteStartDocument();
                xmlWriter.WriteStartElement(sModuleName);
                xmlWriter.WriteEndElement();
                xmlWriter.Close();

                xmlDoc.Load(m_sConfigPath);
            }
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError("Error Module:" + sModuleName);
            UnityEngine.Debug.LogError(ex);
        }
        Config = xmlDoc;
    }

    protected bool GetConfig<T>(string sKey, ref string result)
    {
        if (Config == null) OpenConfig();

        XmlNodeList nodeList = Config.SelectSingleNode(GetType().Name).ChildNodes;
        var t = typeof(T);

        foreach (XmlNode xn in nodeList)
        {
            XmlElement xe = (XmlElement)xn;
            if (xe.Name == sKey && xe.GetAttribute(c_sType) == t.Name)
            {
                result = xe.InnerText;
                return true;
            }
        }
        return false;
    }

    protected void SetConfig<T>(string sKey, string sValue)
    {
        if (Config == null) OpenConfig();

        XmlNode root = Config.SelectSingleNode(GetType().Name);
        XmlNodeList nodeList = root.ChildNodes;
        var t = typeof(T);

        foreach (XmlNode xn in nodeList)
        {
            XmlElement xe = (XmlElement)xn;
            if (xe.Name == sKey && xe.GetAttribute(c_sType) == t.Name)
            {
                xe.InnerText = sValue;
                Config.Save(m_sConfigPath);
                return;
            }
        }

        XmlElement xe1 = Config.CreateElement(sKey);
        xe1.SetAttribute(c_sType, typeof(T).Name);
        xe1.InnerText = sValue;
        root.AppendChild(xe1);
        Config.Save(m_sConfigPath);
    }

    protected void RemoveConfig<T>(string sKey)
    {
        if (Config == null) OpenConfig();

        XmlNode root = Config.SelectSingleNode(GetType().Name);
        XmlNodeList nodeList = root.ChildNodes;
        var t = typeof(T);

        foreach (XmlNode xn in nodeList)
        {
            XmlElement xe = (XmlElement)xn;
            if (xe.Name == sKey && xe.GetAttribute(c_sType) == t.Name)
            {
                root.RemoveChild(xn);
                Config.Save(m_sConfigPath);
                return;
            }
        }
    }
    #endregion

    #region LifeCircle
    public virtual void Init() { }

    public virtual void OnEnable() { }

    public virtual void OnDisable() { }

    public virtual void OnExpand() { }

    public virtual void OnShrink() { }

    public virtual void OnUpdate() { }

    public virtual void OnGUI() { }
    #endregion

    #region Assist Func
    protected bool CheckRunning()
    {
        GUI.contentColor = new Color(0.8f, 0.2f, 0.2f);
        if (!Application.isPlaying) EditorGUILayout.LabelField("-----请先启动游戏-----");
        GUI.contentColor = Color.white;
        return Application.isPlaying;
    }
    #endregion
}

