using Circuit;

namespace LiveSPICEVst
{
    /// <summary>
    /// Simple wrapper around IButtonControl to make UI integration easier
    /// </summary>
    public class DoubleThrowWrapper : ComponentWrapper<IButtonControl>
    {
        bool engaged = false;

        public DoubleThrowWrapper(IButtonControl button, string name) : base(button, name)
        {
        }

        public bool Engaged
        {
            get
            {
                return engaged;
            }

            set
            {
                if (value != engaged)
                {
                    engaged = !engaged;

                    foreach (var button in Sections)
                    {
                        button.Click();
                    }

                    NeedRebuild = true;
                }
            }
        }
    }
}
