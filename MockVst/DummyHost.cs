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

        public uint CurrentAudioBufferSize => 256;
        public long CurrentProjectSample => 0;
        public bool IsPlaying => true;

        public void BeginEdit(int parameter)
        {
        }

        public void EndEdit(int parameter)
        {
        }

        public void PerformEdit(int parameter, double normalizedValue)
        {
        }

        public void ProcessAllEvents()
        {
        }

        public int ProcessEvents()
        {
            return 0;
        }

        public void SendCC(int channel, int ccNumber, int ccValue, int sampleOffset)
        {
        }

        public void SendNoteOff(int channel, int noteNumber, float velocity, int sampleOffset)
        {
        }

        public void SendNoteOn(int channel, int noteNumber, float velocity, int sampleOffset)
        {
        }

        public void SendPolyPressure(int channel, int noteNumber, float pressure, int sampleOffset)
        {
        }
    }
}
