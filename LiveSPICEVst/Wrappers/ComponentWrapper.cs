using System.Collections.Generic;

namespace LiveSPICEVst
{
    public abstract class ComponentWrapper<T> : IComponentWrapper
    {
        public string Name { get; private set; }
        public bool NeedRebuild { get; set; } = false;

        protected List<T> Sections { get; } = new List<T>();

        public ComponentWrapper(T component, string name)
        {
            Name = name;
            AddSection(component);
        }

        public void AddSection(T section)
        {
            Sections.Add(section);
        }
    }
}
