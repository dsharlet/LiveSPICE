using Circuit;

namespace LiveSPICE.PluginCore;

public class DoubleThrowWrapper : ComponentWrapper<IButtonControl>
{
    private bool engaged;

    public DoubleThrowWrapper(IButtonControl button, string name) : base(button, name)
    {
    }

    public int Position => Sections[0].Position;

    public bool Engaged
    {
        get => engaged;
        set
        {
            if (value == engaged)
                return;

            engaged = value;
            foreach (IButtonControl button in Sections)
                button.Click();

            NeedRebuild = true;
        }
    }
}
