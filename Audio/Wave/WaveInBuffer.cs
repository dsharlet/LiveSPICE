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
    class WaveInBuffer : WaveBuffer
    {
        private IntPtr waveIn;
        
        public WaveInBuffer(IntPtr WaveIn, WAVEFORMATEX Format, int Count) : base(Format, Count)
        {
            waveIn = WaveIn;

            MmException.CheckThrow(Winmm.waveInPrepareHeader(waveIn, ref header, Marshal.SizeOf(header)));
        }

        ~WaveInBuffer() { Dispose(false); }

        public override SampleBuffer NewSampleBuffer() { return SampleBuffer.NewInputBuffer(Buffer, type, Samples); }

        public override void Dispose(bool Disposing)
        {
            if (disposed) return;

            // Not checked intentionally.
            Winmm.waveInUnprepareHeader(waveIn, ref header, Marshal.SizeOf(header));

            base.Dispose(Disposing);
        }
        
        public void Record()
        {
            header.dwFlags &= ~WaveHdrFlags.WHDR_DONE;
            header.dwBufferLength = (uint)size;
            MmException.CheckThrow(Winmm.waveInAddBuffer(waveIn, ref header, Marshal.SizeOf(header)));
        }
    }
}
