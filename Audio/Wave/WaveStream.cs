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
        [DllImport("NativeAudioUtils.dll")]
        public static extern void Fixed16x1ToFloat(IntPtr Fixed, float[] Float, int Count);
        [DllImport("NativeAudioUtils.dll")]
        public static extern void Fixed16x1ToDouble(IntPtr Fixed, double[] Float, int Count);
        [DllImport("NativeAudioUtils.dll")]
        public static extern void FloatToFixed16x1(float[] Float, IntPtr Fixed, int Count);
        [DllImport("NativeAudioUtils.dll")]
        public static extern void DoubleToFixed16x1(double[] Float, IntPtr Fixed, int Count);
                     
        private IntPtr hWaveIn, hWaveOut;
        private WAVEFORMATEX format;
        private List<WaveBuffer> buffers;
        private SampleHandler callback;
        private volatile bool disposed = false;
        private Thread proc;
        
        private double[] samples;

        private GCHandle handle;

        public WaveStream(SampleHandler SampleCallback, int Input, int Output, int Rate, int Channels, int Bits, double Latency)
        {
            handle = GCHandle.Alloc(this);

            // Create the format used by this stream.
            format = new WAVEFORMATEX(Rate, Bits, Channels);

            callback = SampleCallback;

            int Buffer = (int)Math.Ceiling(Latency * Rate * Bits * Channels / 8);

            int BufferCount = 6;
            int BufferSize = Buffer / 4;
            BufferSize = BufferSize - (format.nBlockAlign - (Buffer % format.nBlockAlign)) % format.nBlockAlign;

            samples = new double[BufferSize * 8 / Bits];

            // Construct waveOut
            MmException.CheckThrow(Winmm.waveOutOpen(out hWaveOut, Output, ref format, null, (IntPtr)handle, WaveOutOpenFlags.CALLBACK_NULL));

            // Construct waveIn
            MmException.CheckThrow(Winmm.waveInOpen(out hWaveIn, Input, ref format, null, (IntPtr)handle, WaveInOpenFlags.CALLBACK_NULL));
            buffers = new List<WaveBuffer>(BufferCount);
            for (int i = 0; i < BufferCount; ++i)
            {
                WaveBuffer buffer = new WaveBuffer(hWaveIn, hWaveOut, BufferSize);
                buffer.Record();

                buffers.Add(buffer);
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
                        Fixed16x1ToDouble(i.Buffer, samples, samples.Length);
                        callback(samples);
                        DoubleToFixed16x1(samples, i.Buffer, samples.Length);

                        i.PlayHeader.dwBufferLength = (uint)i.Size;
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
    }
}
