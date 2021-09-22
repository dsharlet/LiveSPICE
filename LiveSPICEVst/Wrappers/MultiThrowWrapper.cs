using Circuit;

namespace LiveSPICEVst
{
    public class MultiThrowWrapper : ComponentWrapper<IButtonControl>
    {
        public MultiThrowWrapper(IButtonControl button, string name) : base(button, name)
        {
        }

        public int Position
        {
            get
            {
                return Sections[0].Position;
            }

            set
            {
                if (value != Sections[0].Position)
                {
                    Sections[0].Position = value;

                    NeedRebuild = true;
                }
            }
        }
    }
}
