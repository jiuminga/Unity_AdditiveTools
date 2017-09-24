using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public class LC_Test
{
    [MenuItem("Windplay/测试 &D", false, 7)]
    static void Test()
    {
        var s =
        #region A
            @"using System.IO;";
        #endregion
        var k1 = s.Replace("\r\n", "\n");
        var e1 = new List<string>(k1.Split('\n'));
        var fls = LRHelper_UnityShell.Lex(e1);
        //foreach(var line in fls)
        //{
        //    string s = "";
        //    foreach(var token in line.tokens)
        //    {
        //        s += token.ToString();
        //    }
        //    Debug.LogError(s);
        //}
        var tree = LRHelper_UnityShell.Parse(fls, SD_Assembly.GetCompilationUnitScope(SD_Assembly.UnityAssembly.CSharp, "aa"));
        //var tree = LRHelper_CSharp.Parse(fls, SD_Assembly.GetCompilationUnitScope(path));
        LRHelper_UnityShell.SysmbolResolve(fls);
        Debug.LogError(tree);
    }
}
