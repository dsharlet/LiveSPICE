using System.Collections.Generic;

namespace WaveAudio
{
    public class Driver : Audio.Driver
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
