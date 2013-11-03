using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Audio
{
    /// <summary>
    /// Helper to manage the memory associated with a WAVEHDR.
    /// </summary>
    class WaveOutBuffer : WaveBuffer
    {
        private IntPtr waveOut;
        
        public WaveOutBuffer(IntPtr WaveOut, WAVEFORMATEX Format, int Count) : base(Format, Count)
        {
            waveOut = WaveOut;

            MmException.CheckThrow(Winmm.waveOutPrepareHeader(WaveOut, ref header, Marshal.SizeOf(header)));
            header.dwFlags |= WaveHdrFlags.WHDR_DONE;
        }

        ~WaveOutBuffer() { Dispose(false); }

        public override void Dispose(bool Disposing)
        {
            if (disposed) return;

            Winmm.waveOutUnprepareHeader(waveOut, ref header, Marshal.SizeOf(header));

            base.Dispose(Disposing);
        }

        public override SampleBuffer NewSampleBuffer() 
        {
            SampleBuffer buffer = SampleBuffer.NewOutputBuffer(Buffer, type, Samples);
            buffer.Tag = this;
            return buffer;
        }

        public void Play()
        {
            header.dwFlags &= ~WaveHdrFlags.WHDR_DONE;
            MmException.CheckThrow(Winmm.waveOutWrite(waveOut, ref header, Marshal.SizeOf(header)));
        }
    }
}
