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

public class AT_PythonShell : AT_Component_Base
{
    public AT_PythonShell() : base(3, ToolType.Extend, "Python Shell") { }

    public override void OnGUI()
    {
        GUILayout.Label("等待开发");
    }
}