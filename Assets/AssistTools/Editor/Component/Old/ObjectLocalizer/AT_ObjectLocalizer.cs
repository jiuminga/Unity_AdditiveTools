using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using System.Linq;

public class AT_ObjectLocalizer : AT_Component_Base
{
    public List<string> m_lsCSVPath = new List<string>() { };
    public List<KeyValuePair<string, string>> m_lsCSVFile = new List<KeyValuePair<string, string>>();
    private static string[] m_lsType ={
                                  "All",
                                  "LC_Actor_GamePlayer",
                                  "LC_Actor_NetPlayer",
                              };

    private string m_sFinalType;
    private int m_iSelected = 0;
    private List<GameObject> m_lsSelectedObj = new List<GameObject>();

    private string m_sName = "";

    public AT_ObjectLocalizer()
        : base(-80, ToolType.GameDebugger, "对象定位器")
    {
        m_sFinalType = m_lsType[0];
    }

    private void _reset()
    {
        m_lsSelectedObj.Clear();
        m_iSelected = 0;
        m_sFinalType = m_lsType[0];
        m_sName = "";
    }

    public override void OnGUI()
    {
        GUILayout.BeginHorizontal(EditorStyles.objectFieldThumb);

        GUILayout.BeginHorizontal(EditorStyles.miniButton);
        bool bRefind = false;
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.LabelField("类型:", GUILayout.Width(35));
        m_iSelected = EditorGUILayout.Popup(m_iSelected, m_lsType, GUILayout.Width(70));
        if (EditorGUI.EndChangeCheck())
        {
            m_sFinalType = m_lsType[m_iSelected];
            bRefind = true;
        }

        EditorGUI.BeginChangeCheck();
        m_sFinalType = EditorGUILayout.TextField(m_sFinalType);
        GUILayout.EndHorizontal();

        GUILayout.Space(3);

        GUILayout.BeginHorizontal(EditorStyles.miniButton);
        EditorGUILayout.LabelField("关键字:", GUILayout.Width(45));
        m_sName = EditorGUILayout.TextField(m_sName);
        GUILayout.EndHorizontal();

        if (EditorGUI.EndChangeCheck())
        {
            bRefind = true;
        }

        GUILayout.Space(3);
        GUILayout.BeginHorizontal(EditorStyles.miniButton);
        if (GUILayout.Button("检索", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            bRefind = true;
        }
        GUILayout.Space(3);
        if (GUILayout.Button("重置", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            bRefind = false;
            _reset();
        }
        GUILayout.EndHorizontal();
        GUILayout.EndHorizontal();

        if (bRefind)
        {
            m_lsSelectedObj = ATHelper_Unity.GetAllGameObject();
            if (m_sFinalType != m_lsType[0])
            {
                var lst = ATHelper_Unity.Assembly_GameRuntime.GetTypes().Where(t => t.IsSubclassOf(typeof(MonoBehaviour)));
                var eType = lst.Where(a => a.Name == m_sFinalType).FirstOrDefault();
                if (eType != null)
                {
                    m_lsSelectedObj.RemoveAll(a => a.GetComponent(eType) == null);
                }
                else m_lsSelectedObj.Clear();
            }
            if (!string.IsNullOrEmpty(m_sName))
                m_lsSelectedObj.RemoveAll(a => !a.name.Contains(m_sName));
        }
        if (m_lsSelectedObj.Count == 1)
            Selection.activeGameObject = m_lsSelectedObj[0];

        if (m_lsSelectedObj.Count > 20)
        {
            GUILayout.Label("定位对象数目太多，请增加过滤条件");
        }
        else
        {
            foreach (var i in m_lsSelectedObj)
            {
                if (i == Selection.activeGameObject) GUI.contentColor = ATGUILib.LightBlue;
                try
                {
                    ATGUILib.EasyButton(i.name, a => { Selection.activeGameObject = a as GameObject; }, i, EditorStyles.boldLabel);
                }
                catch (System.Exception)
                {
                    _reset();
                }
                GUI.contentColor = Color.white;
            }
        }
    }
}