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
    class WaveIn : IDisposable
    {                     
        private IntPtr waveIn = IntPtr.Zero;
        private List<WaveInBuffer> buffers;
        private volatile bool disposed = false;
        private AutoResetEvent callback = new AutoResetEvent(false);

        public WaitHandle Callback { get { return callback; } }
        
        public WaveIn(int Device, WAVEFORMATEX Format, int BufferSize)
        {   
            // Construct waveIn
            MmException.CheckThrow(Winmm.waveInOpen(out waveIn, Device, ref Format, callback.SafeWaitHandle.DangerousGetHandle(), IntPtr.Zero, WaveInOpenFlags.CALLBACK_EVENT));

            // Create buffers.
            buffers = new List<WaveInBuffer>();
            for (int i = 0; i < 4; ++i)
            {
                WaveInBuffer b = new WaveInBuffer(waveIn, Format, BufferSize);
                b.Record();
                buffers.Add(b);
            }
            
            MmException.CheckThrow(Winmm.waveInStart(waveIn));
        }

        ~WaveIn() { Dispose(false); }

        public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }
        
        private void Dispose(bool Disposing)
        {
            if (disposed) return;
            disposed = true;

            if (waveIn != IntPtr.Zero)
                Winmm.waveInStop(waveIn);
            if (buffers != null)
            {
                foreach (WaveInBuffer i in buffers)
                    i.Dispose(Disposing);
                buffers.Clear();
            }
            if (waveIn != IntPtr.Zero)
                Winmm.waveInClose(waveIn);
        }

        public void Stop()
        {
            Dispose();
        }

        public WaveInBuffer GetBuffer()
        {
            while (!disposed)
            {
                foreach (WaveInBuffer i in buffers)
                    if (i.Done)
                        return i;
            }
            return null;
        }
    }
}
