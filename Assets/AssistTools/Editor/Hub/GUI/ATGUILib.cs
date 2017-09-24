/************************************************************************/
/*                     Author：qcr                                      */
/************************************************************************/

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Reflection;

public class ATGUILib : EditorWindow
{
    static public readonly Color LightBlue = new Color(0.2f, 0.7f, 1);
    static public readonly Color BrightBlue = new Color(0, 0.82f, 1);
    static public readonly Color LightOrange = new Color(1, 0.45f, 0);
    static private int s_iUIGroupID = 0;
    static public int GenerateGroupID() { return ++s_iUIGroupID; }

    #region ScrollView
    static private Dictionary<int, Vector2> m_dicScrollViewPos = new Dictionary<int, Vector2>();
    public class ScorllView : IDisposable
    {
        public ScorllView(int iID, int iHeight = -1)
        {
            if (!m_dicScrollViewPos.ContainsKey(iID)) m_dicScrollViewPos[iID] = Vector2.zero;
            m_dicScrollViewPos[iID] = GUILayout.BeginScrollView(m_dicScrollViewPos[iID], false, true, iHeight > 0 ? GUILayout.Height(iHeight) : GUILayout.MinHeight(0));
        }
        public void Dispose()
        {
            GUILayout.EndScrollView();
        }
    }
    #endregion

    #region Color
    public static Color DiffColor(Color kColor)
    {
        return new Color(1 - kColor.r, 1 - kColor.g, 1 - kColor.b);
    }

    public static Color EnhanceColor(Color kColor, float fEnhanceParam = 1.5f)
    {
        return new Color(kColor.r * fEnhanceParam, kColor.g * fEnhanceParam, kColor.b * fEnhanceParam);
    }

    #endregion

    #region Base Layout
    public class V : IDisposable
    {
        public V(GUIStyle style = null) { if (style == null)style = EditorStyles.textArea; GUILayout.BeginVertical(style); }
        public void Dispose()
        {
            GUILayout.EndVertical();
        }
    }

    public class H : IDisposable
    {
        public H(GUIStyle style = null) { if (style == null)style = EditorStyles.textArea; GUILayout.BeginHorizontal(style); }
        public void Dispose()
        {
            GUILayout.EndHorizontal();
        }
    }
    #endregion

    #region Expandable UIGroup
    private static Dictionary<int, bool> m_dicGroupExpand = new Dictionary<int, bool>();

    static public bool UIGroupExpanded(int iGroupID)
    {
        bool b = false;
        m_dicGroupExpand.TryGetValue(iGroupID, out b);
        return b;
    }

    public static void ExpandableUIGroup(int iGroupID, Action titleFunc, Action bodyFunc, Action<bool> changeState, bool bExpand = false)
    {
        if (m_dicGroupExpand.ContainsKey(iGroupID))
        {
            bExpand = m_dicGroupExpand[iGroupID];
        }
        else m_dicGroupExpand[iGroupID] = bExpand;

        if (bExpand)
        {
            GUILayout.BeginVertical(EditorStyles.miniButton);
        }
        else GUI.backgroundColor = Color.gray;

        GUILayout.BeginHorizontal(EditorStyles.toolbarButton);
        if (GUILayout.Toggle(false, bExpand ? "  \u25BC  " : "  \u25B2  ", EditorStyles.miniButton, GUILayout.Width(30)))
        {
            m_dicGroupExpand[iGroupID] = !bExpand;
            changeState(!bExpand);
        }
        titleFunc();
        GUILayout.EndHorizontal();

        if (bExpand)
        {
            bodyFunc();
            EditorGUILayout.EndVertical();
        }
        else GUI.backgroundColor = Color.white;
    }

    public static void ExpandableUIGroup(int iGroupID, string title, Color color, Action bodyFunc, Action<bool> changeState, bool bExpand = false)
    {
        ExpandableUIGroup(iGroupID, () => { GUI.contentColor = EnhanceColor(color, 1.3f); EditorGUILayout.LabelField(title, EditorStyles.boldLabel); GUI.contentColor = Color.white; }, bodyFunc, changeState, bExpand);
    }

    public static void ExpandableUIGroup(int iGroupID, string title, Action bodyFunc, Action<bool> changeState, bool bExpand = false)
    {
        ExpandableUIGroup(iGroupID, title, Color.yellow, bodyFunc, changeState, bExpand);
    }
    #endregion

    #region ListView
    static public void ListView(int iGroupID, object[,] info, int iHeight = -1)
    {
        float fWidth = (AT_GUIHub_Old.Instance.position.width - 100) / info.GetLength(1);
        using (var sv = new ScorllView(iGroupID, iHeight))
        {
            for (int i = 0; i < info.GetLength(0); ++i)
            {
                using (var h = new H(EditorStyles.objectFieldThumb))
                {
                    for (int j = 0; j < info.GetLength(1); ++j)
                    {
                        EditorGUILayout.LabelField(info[i, j].ToString(), EditorStyles.textField, GUILayout.Width(fWidth));
                    }
                }
            }
        }
    }

    static public void ListView(int iGroupID, List<List<object>> info, int iHeight = -1)
    {
        float fWidth = (AT_GUIHub_Old.Instance.position.width - 100) / info[0].Count;
        using (var sv = new ScorllView(iGroupID, iHeight))
        {
            for (int i = 0; i < info.Count; ++i)
            {
                using (var h = new H(EditorStyles.objectFieldThumb))
                {
                    for (int j = 0; j < info[0].Count; ++j)
                    {
                        EditorGUILayout.LabelField(info[i][j].ToString(), EditorStyles.textField, GUILayout.Width(fWidth));
                    }
                }
            }
        }
    }
    #endregion

    #region CardLayout
    private static Dictionary<int, int> m_dicCardRecord = new Dictionary<int, int>();

    public struct Card
    {
        public int iID;
        public string title;
        public Action bodyfunc;

        public Card(int i, string sInfo, Action body) { title = sInfo; bodyfunc = body; iID = i; }
    }

    public static void CardLayout(int CardGroupID, Color kColor, params Card[] cards)
    {
        if (cards.Length < 1) return;
        int iSelectedGroup = cards[0].iID;
        if (m_dicCardRecord.ContainsKey(CardGroupID))
            iSelectedGroup = m_dicCardRecord[CardGroupID];
        else m_dicCardRecord[CardGroupID] = cards[0].iID;

        GUILayout.BeginVertical(EditorStyles.miniButton);
        GUILayout.BeginHorizontal();
        GUILayout.BeginHorizontal(EditorStyles.toolbar);
        int iSelected = 0;
        for (int i = 0; i < cards.Length; ++i)
        {
            bool bSelect = cards[i].iID == iSelectedGroup;
            GUI.contentColor = bSelect ? EnhanceColor(kColor) : EnhanceColor(DiffColor(kColor));

            if (!bSelect)
            {
                if (GUILayout.Button(cards[i].title, EditorStyles.wordWrappedLabel, GUILayout.Width(60)))
                    m_dicCardRecord[CardGroupID] = cards[i].iID;
            }
            else
            {
                GUILayout.Label(cards[i].title, EditorStyles.textArea, GUILayout.Width(60), GUILayout.Height(27));
                iSelected = i;
            }
            GUI.contentColor = Color.white;
            if (i != cards.Length - 1) GUILayout.Label("|", GUILayout.Width(7));
        }
        GUILayout.EndHorizontal();
        GUILayout.Label(" ");
        GUILayout.EndHorizontal();
        GUILayout.BeginVertical(EditorStyles.textArea);
        cards[iSelected].bodyfunc();
        GUILayout.EndVertical();
        GUILayout.EndVertical();
    }
    #endregion

    #region SearchBar
    static public Dictionary<int, string> m_dicSearchRecord = new Dictionary<int, string>();
    static public void SearchBar(int iUIGroupID, Action<string> searchFunc, int iWindth = 100)
    {
        GUILayout.BeginHorizontal(EditorStyles.miniButton, GUILayout.Width(iWindth + 40));
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.LabelField("检索：", GUILayout.Width(40));
        if (!m_dicSearchRecord.ContainsKey(iUIGroupID)) m_dicSearchRecord[iUIGroupID] = "";
        m_dicSearchRecord[iUIGroupID] = EditorGUILayout.TextField(m_dicSearchRecord[iUIGroupID], GUILayout.Width(iWindth));
        if (EditorGUI.EndChangeCheck())
        {
            searchFunc(m_dicSearchRecord[iUIGroupID]);
        }
        GUILayout.EndHorizontal();
    }
    #endregion

    #region PathBar
    static public bool PathBar(string title, string sSelectPath, ref string sInfo, string sFileType = "", int iReduceWindth = 130, bool bOpenFolder = false, bool bUseBgBoard = true)
    {
        bool bChanged = false;
        if (bUseBgBoard)
            GUILayout.BeginHorizontal(EditorStyles.objectFieldThumb);
        else GUILayout.BeginHorizontal();
        GUILayout.Label("路径:", GUILayout.Width(40));
        GUILayout.Label(string.IsNullOrEmpty(sInfo) ? "---无效路径---" : sInfo, bUseBgBoard ? EditorStyles.objectFieldThumb : EditorStyles.label, GUILayout.Width(AT_GUIHub_Old.Instance.position.width - iReduceWindth));
        if (GUILayout.Button("...", EditorStyles.miniButton, GUILayout.Width(30)))
        {
            sInfo = bOpenFolder ? EditorUtility.OpenFolderPanel(title, sSelectPath, "") : EditorUtility.OpenFilePanel(title, sSelectPath, sFileType);
            bChanged = true;
        }
        GUILayout.EndHorizontal();
        return bChanged;
    }
    #endregion

    #region EasyButton
    static public void EasyButton(string sName, Action<object> func, object param = null, int width = -1)
    {
        EasyButton(sName, func, param, EditorStyles.miniButton, width);
    }

    static public void EasyButton(string sName, Action<object> func, object param, GUIStyle eds, int width = -1)
    {
        if (GUILayout.Button(sName, eds, width < 0 ? GUILayout.MinWidth(width) : GUILayout.Width(width)))
        {
            func(param);
        }
    }
    #endregion

    #region  ChangeableButton
    static public void ChangeableButton(string sName, bool bEnable, Action<object> func, object param = null, int width = -1)
    {
        if (bEnable)
        {
            EasyButton(sName, func, param, width);
        }
        else
        {
            GUILayout.Label(sName, width < 0 ? GUILayout.MinWidth(width) : GUILayout.Width(width));
        }
    }
    #endregion

    #region GridLayout
    static public void GridLayout(int iCount, int width, int intervalX, int intervalY, Action<int, int, int> gridFunc)
    {
        int iTrueWidth = (int)(AT_GUIHub_Old.Instance.position.width - 50);
        int iColumn = Math.Max(1, iTrueWidth / (width + intervalX));
        GUILayout.BeginVertical(EditorStyles.textArea);
        int iWidgetWidth = iTrueWidth / iColumn - intervalX;
        for (int i = 0; i < iCount; ++i)
        {
            if (i % iColumn == 0)
            {
                if (i != 0) GUILayout.EndHorizontal();
                GUILayout.Space(intervalY);
                GUILayout.BeginHorizontal();
            }
            else GUILayout.Space(intervalX);
            gridFunc(i, iColumn, iWidgetWidth);
        }
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
    }
    #endregion

    #region Assist Fun
    static public GUIContent GetGUIContent(string path, string sName)
    {
        if (!path.StartsWith("Assets"))
        {
            int iStart = path.IndexOf("Assets");
            path = path.Substring(iStart, path.Length - iStart);
        }
        UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));
        if (asset)
        {
            return new GUIContent(sName, AssetDatabase.GetCachedIcon(path));
        }
        return null;
    }
    #endregion
}



