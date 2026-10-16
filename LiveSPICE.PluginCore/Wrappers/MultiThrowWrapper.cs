using Circuit;

namespace LiveSPICE.PluginCore;

public class MultiThrowWrapper : ComponentWrapper<IButtonControl>
{
    public MultiThrowWrapper(IButtonControl button, string name) : base(button, name)
    {
    }

    public int NumPositions => Sections.Max(i => i.NumPositions);

    public int Position
    {
        get => Sections[0].Position;
        set
        {
            if (value == Sections[0].Position)
                return;

            foreach (IButtonControl section in Sections)
                section.Position = value;

            NeedRebuild = true;
        }
    }
}
