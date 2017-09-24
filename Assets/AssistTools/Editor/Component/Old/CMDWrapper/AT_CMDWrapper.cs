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
using System.Threading;

public class AT_CMDWrapper : AT_Component_Base
{
    public AT_CMDWrapper() : base(3, ToolType.Extend, "cmd封装") { }

    private string sInfo = "";
    private string sCmd = "";
    private IntPtr m_iptrCmdwh = new IntPtr(0);
    Process kCmd;

    public override void OnExpand()
    {
        sInfo = "";
        CMD();
    }

    public override void OnShrink()
    {
        try
        {
            kCmd.Kill();
            kCmd.Close();
            kCmd = null;
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError(ex);
        }
    }

    public override void OnGUI()
    {
        EditorGUILayout.LabelField(sInfo, EditorStyles.textArea, GUILayout.Width(AT_GUIHub_Old.Instance.position.width - 50), GUILayout.Height(Math.Min(500, AT_GUIHub_Old.Instance.position.height - 100)));
        sCmd = EditorGUILayout.TextArea(sCmd);
        if (sCmd.EndsWith("\n") && kCmd != null && !kCmd.HasExited)
        {
            try
            {
                ATUtils_WinAPI.SendMsg2Window(m_iptrCmdwh, sCmd);
            }
            catch (Exception ex)
            {
                sInfo += ex.ToString();
            }
            finally
            {
                sCmd = "";
            }
        }
    }

    private void CMD()
    {
        ProcessStartInfo kStartCmd = new ProcessStartInfo();
        kStartCmd.FileName = "cmd";
        //kStartCmd.UseShellExecute = false;
        //kStartCmd.CreateNoWindow = false;
        kStartCmd.Arguments = "/k";
        //kStartCmd.RedirectStandardOutput = true;
        //kStartCmd.RedirectStandardError = true;
        //kStartCmd.RedirectStandardInput = true;
        kCmd = new Process();
        kCmd.StartInfo = kStartCmd;

        try
        {
            kCmd.Start();
            m_iptrCmdwh = kCmd.MainWindowHandle;
            //kCmd.OutputDataReceived += (a, d) => sInfo += d.Data.Replace("\r", "\r\n");
            //kCmd.ErrorDataReceived += (a, d) => sInfo += d.Data.Replace("\r", "\r\n");
            //kCmd.EnableRaisingEvents = true;
            //kCmd.BeginOutputReadLine();
            //kCmd.BeginErrorReadLine();
            //ATUtils_WinAPI.ShowWindow(m_iptrCmdwh, 2);
            //kCmd.WaitForExit();
        }
        catch (System.Exception ex)
        {
            sInfo += ex;
        }
    }
}