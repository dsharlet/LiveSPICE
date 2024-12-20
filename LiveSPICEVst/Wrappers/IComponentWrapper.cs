namespace LiveSPICEVst
{
    public interface IComponentWrapper
    {
        string Name { get; }
        bool NeedRebuild { get; set;  }
    }
}