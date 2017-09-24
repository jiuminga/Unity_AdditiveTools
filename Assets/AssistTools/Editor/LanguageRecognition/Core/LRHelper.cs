using System.Collections.Generic;
using System.Text;

public static class LRHelper
{
    public static List<string> SplitFile(string sData)
    {
        StringBuilder sTest = new StringBuilder(sData);
        var k = sTest.Replace("\r\n", "\n").Replace("\uFEFF", "");
        return new List<string>(k.ToString().Split('\n'));
    }
}
