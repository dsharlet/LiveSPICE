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
                return Sections[0].Position;
            }

            set
            {
                if (Sections[0].Position != value)
                {
                    bool needUpdate = false;
                    foreach (var section in Sections)
                    {
                        section.Position = value;
                        needUpdate |= !section.Dynamic;
                    }

                    NeedUpdate = needUpdate;
                    OnPropertyChanged();
                }
            }
        }
    }
}
