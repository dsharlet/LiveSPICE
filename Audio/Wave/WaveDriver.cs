using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Audio
{
    class WaveDriver : Driver
    {
        public override string Name
        {
            get { return "Windows Audio"; }
        }

        public override IEnumerable<Device> Devices { get { yield return new WaveDevice(); } }
    }
}
