using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

public static class ATHelper_Xml
{
    public const string c_sType = "Type";
    public const string c_sValue = "Value";
    public const string c_sXMLExtension = ".xml";

    static public XmlDocument OpenXml(string sModuleName)
    {
        if (!sModuleName.Contains(c_sXMLExtension)) sModuleName += c_sXMLExtension;
        XmlDocument xmlDoc = new XmlDocument();
        string sPath = ATDefine.Path_Config + Path.DirectorySeparatorChar + sModuleName;
        try
        {
            if (File.Exists(sPath))
            {
                xmlDoc.Load(sPath);
            }
            else
            {
                XmlTextWriter xmlWriter;
                xmlWriter = new XmlTextWriter(sPath, Encoding.Unicode);
                xmlWriter.Formatting = Formatting.Indented;
                xmlWriter.WriteStartDocument();
                xmlWriter.WriteStartElement(sModuleName);
                xmlWriter.WriteEndElement();
                xmlWriter.Close();

                xmlDoc.Load(sPath);
            }
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError("Error Module:" + sModuleName);
            UnityEngine.Debug.LogError(ex);
        }
        return xmlDoc;
    }

    static public bool GetConfig<T>(this XmlDocument Config, string sKey, ref T result)
    {
        XmlNodeList nodeList = Config.ChildNodes[1].ChildNodes;
        var t = typeof(T);

        foreach (XmlNode xn in nodeList)
        {
            XmlElement xe = (XmlElement)xn;
            if (xe.Name == sKey && xe.GetAttribute(c_sType) == t.Name)
            {
                result = ATHelper_Serialize.Deserialize<T>(xe.InnerText);
                return true;
            }
        }
        return false;
    }

    static public void SetConfig<T>(this XmlDocument Config, string sKey, T pValue)
    {
        string sPath = ATDefine.Path_Config + Path.DirectorySeparatorChar + Config.ChildNodes[1].Name;

        XmlNode root = Config.ChildNodes[1];
        XmlNodeList nodeList = root.ChildNodes;
        var t = typeof(T);

        foreach (XmlNode xn in nodeList)
        {
            XmlElement xe = (XmlElement)xn;
            if (xe.Name == sKey && xe.GetAttribute(c_sType) == t.Name)
            {
                xe.InnerText = ATHelper_Serialize.Serialize<T>(pValue);
                Config.Save(sPath);
                return;
            }
        }

        XmlElement xe1 = Config.CreateElement(sKey);
        xe1.SetAttribute(c_sType, typeof(T).Name);
        xe1.InnerText = ATHelper_Serialize.Serialize<T>(pValue);
        root.AppendChild(xe1);
        Config.Save(sPath);
    }

    static public void RemoveConfig<T>(this XmlDocument Config, string sKey)
    {
        string sPath = ATDefine.Path_Config + Path.DirectorySeparatorChar + Config.ChildNodes[1].Name;

        XmlNode root = Config.ChildNodes[1];
        XmlNodeList nodeList = root.ChildNodes;
        var t = typeof(T);

        foreach (XmlNode xn in nodeList)
        {
            XmlElement xe = (XmlElement)xn;
            if (xe.Name == sKey && xe.GetAttribute(c_sType) == t.Name)
            {
                root.RemoveChild(xn);
                Config.Save(sPath);
                return;
            }
        }
    }
}
