using System.Collections.Generic;

namespace LiveSPICE.PluginCore;

public abstract class ComponentWrapper<T> : IComponentWrapper
{
    protected ComponentWrapper(T component, string name)
    {
        Name = name;
        AddSection(component);
    }

    public string Name { get; }

    public bool NeedUpdate { get; set; }

    public bool NeedRebuild { get; set; }

    protected List<T> Sections { get; } = new List<T>();

    public void AddSection(T section)
    {
        Sections.Add(section);
    }
}
