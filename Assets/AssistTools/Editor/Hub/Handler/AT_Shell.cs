using System;
using System.Collections.Generic;
using UnityEditor;

[InitializeOnLoad]
public class AT_Shell : AT_Handler
{
    public FormatedLineWrapper m_kFormatLines;
    public static Dictionary<string, Type> m_dicCommandHandler = new Dictionary<string, Type>();

    static AT_Shell()
    {
        var types = ATHelper_Unity.Assembly_GameEditor.GetTypes();
        foreach (var t in types)
        {
            if (!t.IsSubclassOf(typeof(AT_Component_Base))) continue;
            var lsAttr = t.GetCustomAttributes(typeof(UnityShellAttribute), false);
            if (lsAttr != null)
            {
                foreach (var o in lsAttr)
                {
                    string s = (o as UnityShellAttribute).Command;
                    if (m_dicCommandHandler.ContainsKey(s))
                        UnityEngine.Debug.LogError(string.Format("Command {0} Have Been Registered By {1}, Error In {2}", s, m_dicCommandHandler[s].Name, t.Name));
                    m_dicCommandHandler[s] = t;
                }
            }
        }
        Lexer_UnityShell.Instacnce.Keywords.UnionWith(m_dicCommandHandler.Keys);
    }
}
