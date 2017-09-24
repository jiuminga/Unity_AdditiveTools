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

public class AT_WindowsOperate : AT_Component_Base
{
    public AT_WindowsOperate() : base(3, ToolType.Extend, "Windows窗口操作") { }

    private int i = 0;
    private string sInfo = "";
    private bool m_blisten = false;
    private ATUtils_WinAPI.KeyboardHook hook;

    public override void OnGUI()
    {
        i = EditorGUILayout.IntField(i);

        sInfo = EditorGUILayout.TextArea(sInfo);
        if (GUILayout.Button("显示"))
        {
            ATUtils_WinAPI.ShowWindow(new IntPtr(i), 3);
        }

        if (GUILayout.Button("隐藏"))
        {
            ATUtils_WinAPI.ShowWindow(new IntPtr(i), 2);
        }

        if (GUILayout.Button("捕获ListView文本"))
        {
            var e = new ATUtils_WinAPI.ListViewCatecher.ListView();
            ATUtils_WinAPI.ListViewCatecher.DoCatch(i, e);
            foreach (var k in e.Items)
            {
                foreach (var d in k)
                {
                    UnityEngine.Debug.LogError(d);
                }
            }
        }

        if (GUILayout.Button("窗口位置"))
        {
            ATUtils_WinAPI.RECT rect = new ATUtils_WinAPI.RECT();
            ATUtils_WinAPI.GetWindowRect(i, ref rect);
            UnityEngine.Debug.LogError("L:" + rect.Left + " R:" + rect.Right + " T:" + rect.Top + " :B" + rect.Bottom);
        }

        if (GUILayout.Button("发送信息"))
        {
            ATUtils_WinAPI.SendMsg2Window(new IntPtr(i), sInfo);
        }
        if (GUILayout.Button("模拟按键"))
        {
            for (int index = 0; index < sInfo.Length; ++index)
                ATUtils_WinAPI.SendMessage((IntPtr)i, ATUtils_WinAPI.WM_CHAR, sInfo[index], 0);
        }

        using (var h = new ATGUILib.H())
        {
            ATGUILib.ChangeableButton("开始监听按键", !m_blisten, a =>
                {
                    hook = new ATUtils_WinAPI.KeyboardHook();
                    hook.KeyDownEvent += (x, y) => UnityEngine.Debug.LogError("KeyDown:" + (char)y.KeyCode);
                    //hook.KeyPressEvent += (x, y) => UnityEngine.Debug.LogError("KeyPress" + y.KeyCode);
                    hook.KeyUpEvent += (x, y) => UnityEngine.Debug.LogError("KeyUp:" + (char)y.KeyCode);
                    hook.Start();
                    m_blisten = true;
                }
                );

            ATGUILib.ChangeableButton("停止监听按键", m_blisten, a =>
                {
                    hook.Stop();
                    m_blisten = false;
                }
                );
        }
    }
}