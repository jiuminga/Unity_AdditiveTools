/************************************************************************/
/*                     Author：qcr                                      */
/************************************************************************/ 
using UnityEngine;

public class AT_JIRA : AT_Component_Base
{
    public AT_JIRA() : base(3, ToolType.WorkFlow, "JIRA", true) { }

    public override void OnGUI()
    {
        if (GUILayout.Button("打开"))
        {
            ATHelper_Process.OpenUrl(@"http://192.168.10.113:8080/secure/Dashboard.jspa");
        }
    }
}