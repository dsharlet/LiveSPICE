using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;

namespace WaveAudio
{
    class WaveIn : IDisposable
    {                     
        private IntPtr waveIn = IntPtr.Zero;
        private List<InBuffer> buffers;
        private volatile bool disposed = false;
        private EventWaitHandle callback = new AutoResetEvent(false);

        public EventWaitHandle Callback { get { return callback; } }
        
        public WaveIn(int Device, WAVEFORMATEX Format, int BufferSize)
        {   
            // Construct waveIn
            MmException.CheckThrow(Winmm.waveInOpen(out waveIn, Device, ref Format, callback.Handle, IntPtr.Zero, WaveInOpenFlags.CALLBACK_EVENT));

            // Create buffers.
            buffers = new List<InBuffer>();
            for (int i = 0; i < 4; ++i)
            {
                InBuffer b = new InBuffer(waveIn, Format, BufferSize);
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
                foreach (InBuffer i in buffers)
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

        public InBuffer GetBuffer()
        {
            while (!disposed)
            {
                foreach (InBuffer i in buffers)
                    if (i.Done)
                        return i;
            }
            return null;
        }
    }
}
