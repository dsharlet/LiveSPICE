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
    class WaveStream : Stream, IDisposable
    {                     
        private IntPtr hWaveIn, hWaveOut;
        private WAVEFORMATEX format;
        private List<WaveBuffer> buffers;
        private SampleHandler callback;
        private volatile bool disposed = false;
        private Thread proc;
        
        private GCHandle handle;

        public WaveStream(SampleHandler SampleCallback, int Input, int Output, double Latency)
        {
            handle = GCHandle.Alloc(this);

            int Rate = 48000;
            int Bits = 16;
            int Channels = 1;

            // Create the format used by this stream.
            format = new WAVEFORMATEX(Rate, Bits, Channels);

            callback = SampleCallback;
            
            // Construct waveOut
            MmException.CheckThrow(Winmm.waveOutOpen(out hWaveOut, Output, ref format, null, (IntPtr)handle, WaveOutOpenFlags.CALLBACK_NULL));

            // Construct waveIn
            MmException.CheckThrow(Winmm.waveInOpen(out hWaveIn, Input, ref format, null, (IntPtr)handle, WaveInOpenFlags.CALLBACK_NULL));

            // Create buffers.
            int buffer = (int)Math.Ceiling(Latency / 2 * Rate * Channels);
            buffers = new List<WaveBuffer>();
            for (int i = 0; i < 6; ++i)
            {
                WaveBuffer b = new WaveBuffer(hWaveIn, hWaveOut, FormatSampleType(format), buffer, BlockAlignedSize(format, buffer));
                b.Record();
                buffers.Add(b);
            }

            proc = new Thread(new ThreadStart(Proc));
            proc.Start();

            MmException.CheckThrow(Winmm.waveInStart(hWaveIn));
        }

        ~WaveStream() { Dispose(false); }

        public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }
        
        private void Dispose(bool Disposing)
        {
            if (disposed) return;
            disposed = true;

            proc.Join();

            Winmm.waveOutReset(hWaveOut);
            Winmm.waveInStop(hWaveIn);
            foreach (WaveBuffer i in buffers)
                i.Dispose(Disposing);
            buffers.Clear();
            Winmm.waveInClose(hWaveIn);
            Winmm.waveOutClose(hWaveOut);

            if (handle.IsAllocated)
                handle.Free();
        }

        public override void Stop()
        {
            Dispose();
        }

        private void Proc()
        {
            while (!disposed)
            {
                foreach (WaveBuffer i in buffers)
                {
                    if ((i.RecordHeader.dwFlags & WaveHdrFlags.WHDR_DONE) != 0)
                    {
                        callback(i.Buffer, i.Buffer, format.nSamplesPerSec);

                        i.Buffer.ToBuffer();

                        i.PlayHeader.dwBufferLength = (uint)i.Buffer.Size;
                        i.RecordHeader.dwFlags = i.RecordHeader.dwFlags & ~WaveHdrFlags.WHDR_DONE;
                        i.Play();
                    }
                    if (i.PlayHeader.dwFlags.HasFlag(WaveHdrFlags.WHDR_DONE))
                    {
                        i.PlayHeader.dwFlags = i.PlayHeader.dwFlags & ~WaveHdrFlags.WHDR_DONE;
                        i.Record();
                    }
                }
            }
        }

        private static SampleType FormatSampleType(WAVEFORMATEX Format)
        {
            switch (Format.wBitsPerSample)
            {
                case 16: return SampleType.i16;
                case 32: return SampleType.i32;
                default: throw new NotImplementedException("Unsupported sample type.");
            }
        }

        private static int BlockAlignedSize(WAVEFORMATEX Format, int Count)
        {
            int align = Format.nBlockAlign;
            int size = Count * Format.wBitsPerSample / 8;

            return size + (align - (size % align)) % align;
        }
    }
}
