using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;

public class AT_SpeedAdjust : AT_Component_Base
{
    public List<string> m_lsCSVPath = new List<string>() { };
    public List<KeyValuePair<string, string>> m_lsCSVFile = new List<KeyValuePair<string, string>>();

    public AT_SpeedAdjust()
        : base(3, ToolType.GameDebugger, "变速齿轮")
    {

    }

    public override void OnGUI()
    {
        GUILayout.BeginHorizontal(EditorStyles.objectFieldThumb);
        GUILayout.Label("   游戏速度:", GUILayout.Width(80));
        Time.timeScale = EditorGUILayout.Slider(Time.timeScale, 0, 10);
        if (GUILayout.Button("重置", EditorStyles.miniButton, GUILayout.Width(80)))
        {
            Time.timeScale = 1;
        }
        GUILayout.EndHorizontal();
    }
}