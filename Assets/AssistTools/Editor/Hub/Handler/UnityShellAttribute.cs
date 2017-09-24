using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class UnityShellAttribute : Attribute
{
    public string Command { get; set; }
}
