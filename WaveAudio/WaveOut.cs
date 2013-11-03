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
    class WaveOut : IDisposable
    {                     
        private IntPtr waveOut = IntPtr.Zero;
        private List<OutBuffer> buffers;
        private volatile bool disposed = false;
        private AutoResetEvent callback = new AutoResetEvent(false);

        public WaitHandle Callback { get { return callback; } }

        public WaveOut(int Device, WAVEFORMATEX Format, int BufferSize)
        {            
            // Construct waveOut
            MmException.CheckThrow(Winmm.waveOutOpen(out waveOut, Device, ref Format, callback.SafeWaitHandle.DangerousGetHandle(), IntPtr.Zero, WaveOutOpenFlags.CALLBACK_NULL));

            // Create buffers.
            buffers = new List<OutBuffer>();
            for (int i = 0; i < 4; ++i)
                buffers.Add(new OutBuffer(waveOut, Format, BufferSize));
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
                foreach (Buffer i in buffers)
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

        public OutBuffer GetBuffer()
        {
            while (!disposed)
            {
                foreach (OutBuffer i in buffers)
                    if (i.Done)
                        return i;
            }
            return null;
        }
    }
}
