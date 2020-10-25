using System;
using System.Collections.Generic;
using Util;

namespace WaveAudio
{
    class WaveOut : IDisposable
    {
        private IntPtr waveOut = IntPtr.Zero;
        private List<OutBuffer> buffers;
        private volatile bool disposed = false;

        public WaveOut(int Device, WAVEFORMATEX Format, int BufferSize)
        {
            Log.Global.WriteLine(MessageType.Info, "Opening wave out device '{0}'.", Device);

            // Construct waveOut
            MmException.CheckThrow(Winmm.waveOutOpen(out waveOut, Device, ref Format, IntPtr.Zero, IntPtr.Zero, WaveOutOpenFlags.CALLBACK_NULL));

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

            Log.Global.WriteLine(MessageType.Info, "Closing wave out device.");

            if (waveOut != IntPtr.Zero)
                Winmm.waveOutReset(waveOut);
            if (buffers != null)
            {
                foreach (Buffer i in buffers)
                    i.Dispose(Disposing);
                buffers = null;
            }
            if (waveOut != IntPtr.Zero)
                Winmm.waveOutClose(waveOut);
            waveOut = IntPtr.Zero;
        }

        public void Stop()
        {
            Dispose();
        }

        public OutBuffer GetBuffer()
        {
            foreach (OutBuffer i in buffers)
                if (i.Done)
                    return i;
            return null;
        }
    }
}
