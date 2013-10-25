using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Audio
{
    class WaveDriver : Driver
    {
        public WaveDriver()
        {
            devices = new List<Device>() { new WaveDevice() };
        }

        public override string Name
        {
            get { return "Windows Audio"; }
        }
    }
}
