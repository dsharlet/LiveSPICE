using System;

using AudioPlugSharp;

namespace MockVst
{
    /// <summary>
    /// Dummy VST host class to use for testing
    /// </summary>
    class DummyHost : IAudioHost
    {
        public double SampleRate { get { return 44100; } }
        public EAudioBitsPerSample BitsPerSample { get { return EAudioBitsPerSample.Bits64; } }
        public UInt32 MaxAudioBufferSize { get { return 64; } }
        public double BPM { get { return 120; } }
        public double SamplePosition { get { return 0; } }
    }
}
