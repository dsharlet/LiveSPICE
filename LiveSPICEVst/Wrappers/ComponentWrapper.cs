using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LiveSPICEVst
{
    public abstract class ComponentWrapper<T> : IComponentWrapper
    {
        public string Name { get; private set; }
        public bool NeedUpdate { get; set; }
        public bool NeedRebuild { get; set; }
        protected List<T> Sections { get; } = new List<T>();

        public ComponentWrapper(T component, string name)
        {
            Name = name;
            AddSection(component);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void SetProperty<T>(ref T field, T newValue, [CallerMemberName] string propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, newValue))
            {
                field = newValue;
                OnPropertyChanged(propertyName);
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void AddSection(T section)
        {
            Sections.Add(section);
        }
    }
}
