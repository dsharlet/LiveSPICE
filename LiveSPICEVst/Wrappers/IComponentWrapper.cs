using System.ComponentModel;

namespace LiveSPICEVst
{
    public interface IComponentWrapper: INotifyPropertyChanged
    {
        string Name { get; }
        bool NeedRebuild { get; set; }
        bool NeedUpdate { get; set; }
    }
}