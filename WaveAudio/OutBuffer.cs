using System;
using System.Runtime.InteropServices;

namespace WaveAudio
{
    /// <summary>
    /// Helper to manage the memory associated with a WAVEHDR.
    /// </summary>
    class OutBuffer : Buffer
    {
        private IntPtr waveOut;

        public OutBuffer(IntPtr WaveOut, WAVEFORMATEX Format, int Count) : base(Format, Count)
        {
            waveOut = WaveOut;

            MmException.CheckThrow(Winmm.waveOutPrepareHeader(WaveOut, header, Marshal.SizeOf(header)));
            header.dwFlags |= WaveHdrFlags.WHDR_DONE;
        }

        ~OutBuffer() { Dispose(false); }

        public override void Dispose(bool Disposing)
        {
            if (disposed) return;

            Winmm.waveOutUnprepareHeader(waveOut, header, Marshal.SizeOf(header));

            base.Dispose(Disposing);
        }

        public void Play()
        {
            header.dwFlags &= ~WaveHdrFlags.WHDR_DONE;
            MmException.CheckThrow(Winmm.waveOutWrite(waveOut, header, Marshal.SizeOf(header)));
        }
    }
}
