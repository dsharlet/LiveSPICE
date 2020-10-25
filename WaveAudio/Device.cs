using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace WaveAudio
{
    class Channel : Audio.Channel
    {
        private int device;
        public int Device { get { return device; } }

        private string name;
        public override string Name { get { return name; } }

        public Channel(string Name, int Device) { name = Name; device = Device; }

        public override string ToString()
        {
            return name;
        }
    }

    class Device : Audio.Device
    {
        public Device() : base("Windows Audio")
        {
            List<Channel> channels = new List<Channel>();
            int count = Winmm.waveOutGetNumDevs();
            for (int i = 0; i < count; ++i)
            {
                WAVEOUTCAPS caps = new WAVEOUTCAPS();
                MmException.CheckThrow(Winmm.waveOutGetDevCaps(new IntPtr(i), ref caps, (uint)Marshal.SizeOf(caps)));
                channels.Add(new Channel(caps.szPname, i));
            }
            outputs = channels.ToArray();

            channels = new List<Channel>();
            count = Winmm.waveInGetNumDevs();
            for (int i = 0; i < count; ++i)
            {
                WAVEINCAPS caps = new WAVEINCAPS();
                MmException.CheckThrow(Winmm.waveInGetDevCaps(new IntPtr(i), ref caps, (uint)Marshal.SizeOf(caps)));
                channels.Add(new Channel(caps.szPname, i));
            }
            inputs = channels.ToArray();
        }

        public override Audio.Stream Open(Audio.Stream.SampleHandler Callback, Audio.Channel[] Input, Audio.Channel[] Output)
        {
            return new Stream(
                Callback,
                Input.Cast<Channel>().ToArray(),
                Output.Cast<Channel>().ToArray(),
                0.05);
        }
    }
}
