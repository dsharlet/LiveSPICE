using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Audio
{
    class WaveChannel : Channel
    {
        private int device;
        public int Device { get { return device; } }

        private string name;
        public override string Name { get { return name; } }

        public WaveChannel(string Name, int Device) { name = Name; device = Device; }
    }

    class WaveDevice : Device
    {
        public override Channel[] InputChannels
        {
            get
            {
                List<WaveChannel> channels = new List<WaveChannel>();

                int count = Winmm.waveInGetNumDevs();
                for (int i = 0; i < count; ++i)
                {
                    WAVEINCAPS caps = new WAVEINCAPS();
                    MmException.CheckThrow(Winmm.waveInGetDevCaps(new IntPtr(i), ref caps, (uint)Marshal.SizeOf(caps)));

                    channels.Add(new WaveChannel(caps.szPname, i));
                }

                return channels.ToArray();
            }
        }

        public override Channel[] OutputChannels
        {
            get
            {
                List<WaveChannel> channels = new List<WaveChannel>();

                int count = Winmm.waveOutGetNumDevs();
                for (int i = 0; i < count; ++i)
                {
                    WAVEOUTCAPS caps = new WAVEOUTCAPS();
                    MmException.CheckThrow(Winmm.waveOutGetDevCaps(new IntPtr(i), ref caps, (uint)Marshal.SizeOf(caps)));

                    channels.Add(new WaveChannel(caps.szPname, i));
                }

                return channels.ToArray();
            }
        }

        public WaveDevice() : base("Windows Audio") { }

        public override Stream Open(Stream.SampleHandler Callback, Channel InputChannel, Channel OutputChannel, double SampleRate, int BitsPerSample, double Latency)
        {
            return new WaveStream(
                Callback,
                ((WaveChannel)InputChannel).Device,
                ((WaveChannel)OutputChannel).Device,
                (int)SampleRate, 1, BitsPerSample, Latency);
        }
    }
}
