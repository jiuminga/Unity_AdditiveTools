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

public class AT_TAPD : AT_Component_Base
{
    public AT_TAPD() : base(3, ToolType.WorkFlow, "TAPD") { }

    public override void OnGUI()
    {
        GUILayout.Label("等待开发");
    }
}