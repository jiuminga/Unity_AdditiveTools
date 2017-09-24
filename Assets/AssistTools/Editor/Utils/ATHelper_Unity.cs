/************************************************************************/
/*                     Author：qcr                                      */
/************************************************************************/
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

static public class ATHelper_Unity
{
    static public Assembly Assembly_GameEditor
    {
        get
        {
            mAssembly_GameRuntime = mAssembly_GameRuntime ?? Assembly.LoadFrom(ATDefine.Path_Assemblies + "Assembly-CSharp-Editor.dll");
            return mAssembly_GameRuntime;
        }
    }
    static public Assembly mAssembly_GameEditor;

    static public Assembly Assembly_GameRuntime
    {
        get
        {
            mAssembly_GameRuntime = mAssembly_GameRuntime ?? Assembly.LoadFrom(ATDefine.Path_Assemblies + "Assembly-CSharp.dll");
            return mAssembly_GameRuntime;
        }
    }
    static public Assembly mAssembly_GameRuntime;

    static public List<GameObject> GetAllGameObject()
    {
        return new List<GameObject>(UnityEngine.Object.FindObjectsOfType<GameObject>());
    }

    static public List<GameObject> GetAllSubGameObject(GameObject go)
    {
        var lsRet = new List<GameObject> { go };
        foreach (Transform child in go.transform)
        {
            lsRet.AddRange(GetAllSubGameObject(child.gameObject));
        }
        return lsRet;
    }
}