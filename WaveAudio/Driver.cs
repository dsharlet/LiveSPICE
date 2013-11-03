using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WaveAudio
{
    class Driver : Audio.Driver
    {
        public Driver()
        {
            devices = new List<Audio.Device>() { new Device() };
        }

        public override string Name
        {
            get { return "Windows Audio"; }
        }
    }
}
