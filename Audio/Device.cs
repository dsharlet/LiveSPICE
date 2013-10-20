using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Audio
{
    public abstract class Channel
    {
        public abstract string Name { get; }
    };

    /// <summary>
    /// Devices describe the supported audio stream properties.
    /// </summary>
    public abstract class Device
    {
        private string name;
        public string Name { get { return name; } }

        public abstract Channel[] InputChannels { get; }
        public abstract Channel[] OutputChannels { get; }

        protected Device(string Name) { name = Name; }

        public abstract Stream Open(Stream.SampleHandler Callback, Channel InputChannel, Channel OutputChannel, double SampleRate, int BitsPerSample, double Latency);
    }
}
