using Circuit;

namespace LiveSPICEVst
{
    /// <summary>
    /// Simple wrapper around IPotControl to make UI integration easier
    /// </summary>
    public class PotWrapper : ComponentWrapper<IPotControl>
    {
        public PotWrapper(IPotControl pot, string name) : base(pot, name)
        {

        }

        public double PotValue
        {
            get
            {
                return Sections[0].PotValue;
            }

            set
            {
                if (Sections[0].PotValue != value)
                {
                    foreach (var section in Sections)
                    {
                        section.PotValue = value;
                    }

                    NeedUpdate = true;
                }
            }
        }
    }
}
