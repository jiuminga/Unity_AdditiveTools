using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;

public class AT_ActorCheckOut : AT_Component_Base
{
    public List<string> m_lsCSVPath = new List<string>() { };
    public List<KeyValuePair<string, string>> m_lsCSVFile = new List<KeyValuePair<string, string>>();

    public AT_ActorCheckOut()
        : base(3, ToolType.GameDebugger, "查看角色")
    {

    }

    public override void OnGUI()
    {
        GUILayout.Label("Success");
    }
}