using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace WaveAudio
{
    /// <summary>
    /// Helper to manage the memory associated with a WAVEHDR.
    /// </summary>
    class InBuffer : Buffer
    {
        private IntPtr waveIn;
        
        public InBuffer(IntPtr WaveIn, WAVEFORMATEX Format, int Count) : base(Format, Count)
        {
            waveIn = WaveIn;

            MmException.CheckThrow(Winmm.waveInPrepareHeader(waveIn, ref header, Marshal.SizeOf(header)));
            header.dwFlags |= WaveHdrFlags.WHDR_DONE;
        }

        ~InBuffer() { Dispose(false); }

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
            MmException.CheckThrow(Winmm.waveInAddBuffer(waveIn, ref header, Marshal.SizeOf(header)));
        }
    }
}
