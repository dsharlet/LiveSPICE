using SharpSoundDevice;
using System;

namespace MockVst
{
    /// <summary>
    /// Dummy VST host class to use for testing
    /// </summary>
    class DummyHostInfo : IHostInfo
    {
        public double BPM { get { return 120; } }

        public double SamplePosition { get { return 0; } }

        public double SampleRate { get { return 44100; } }

        public int BlockSize { get { return 64; } }

        public int TimeSignatureNum => throw new NotImplementedException();

        public int TimeSignatureDen => throw new NotImplementedException();

        public string HostVendor => throw new NotImplementedException();

        public string HostName => throw new NotImplementedException();

        public uint HostVersion => throw new NotImplementedException();

        public void SendEvent(int pluginSenderId, Event ev)
        {
        }
    }
}
