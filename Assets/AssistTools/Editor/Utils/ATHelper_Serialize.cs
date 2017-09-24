using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

static public class ATHelper_Serialize
{
    static public T Deserialize<T>(string sData)
    {
        //TODO
        var t = typeof(T);
        object obj = sData;

        if (t == typeof(int)) obj = int.Parse(sData);
        else if (t == typeof(bool)) obj = bool.Parse(sData);
        else if (t == typeof(float)) obj = float.Parse(sData);
        return (T)obj;
    }

    static public string Serialize<T>(T obj)
    {
        //TODO
        return obj.ToString();
    }
}
