using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Audio
{
    class WaveDriver : Driver
    {
        private static List<Device> devices = new List<Device>()
        {
            new WaveDevice()
        };
        public override IEnumerable<Device> Devices { get { return devices; } }

        public override string Name
        {
            get { return "Windows Audio"; }
        }
    }
}
