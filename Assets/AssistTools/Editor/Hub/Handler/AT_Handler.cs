using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public abstract class AT_Handler
{
    public virtual void OnChangeMode() { }
    public virtual void Init() { }
    public virtual void OnEnable() { }
    public virtual void OnDisable() { }
    public virtual void OnUpdate() { }
    public virtual void OnGUI() { }
}
