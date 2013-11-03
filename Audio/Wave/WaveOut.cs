using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;

namespace Audio
{
    class WaveOut : IDisposable
    {                     
        private IntPtr waveOut = IntPtr.Zero;
        private List<WaveOutBuffer> buffers;
        private volatile bool disposed = false;
        
        public WaveOut(int Device, WAVEFORMATEX Format, int BufferSize)
        {            
            // Construct waveOut
            MmException.CheckThrow(Winmm.waveOutOpen(out waveOut, Device, ref Format, null, IntPtr.Zero, WaveOutOpenFlags.CALLBACK_NULL));

            // Create buffers.
            buffers = new List<WaveOutBuffer>();
            for (int i = 0; i < 4; ++i)
                buffers.Add(new WaveOutBuffer(waveOut, Format, BufferSize));
        }

        ~WaveOut() { Dispose(false); }

        public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }
        
        private void Dispose(bool Disposing)
        {
            if (disposed) return;
            disposed = true;

            if (waveOut != IntPtr.Zero)
                Winmm.waveOutReset(waveOut);
            if (buffers != null)
            {
                foreach (WaveBuffer i in buffers)
                    i.Dispose(Disposing);
                buffers.Clear();
            }
            if (waveOut != IntPtr.Zero)
                Winmm.waveOutClose(waveOut);
        }

        public void Stop()
        {
            Dispose();
        }

        public WaveOutBuffer GetBuffer()
        {
            while (!disposed)
            {
                foreach (WaveOutBuffer i in buffers)
                    if (i.Done)
                        return i;
            }
            return null;
        }
    }
}
