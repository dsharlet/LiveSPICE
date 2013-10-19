using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;

namespace AudioIo
{
    public class WaveIo : IDisposable
    {
        [DllImport("NativeAudioUtils.dll")]
        public static extern void Fixed16x1ToFloat(IntPtr Fixed, float[] Float, int Count);
        [DllImport("NativeAudioUtils.dll")]
        public static extern void Fixed16x1ToDouble(IntPtr Fixed, double[] Float, int Count);
        [DllImport("NativeAudioUtils.dll")]
        public static extern void FloatToFixed16x1(float[] Float, IntPtr Fixed, int Count);
        [DllImport("NativeAudioUtils.dll")]
        public static extern void DoubleToFixed16x1(double[] Float, IntPtr Fixed, int Count);

        /// <summary>
        /// Handler for accepting new samples in and writing output samples out.
        /// </summary>
        /// <param name="Samples"></param>
        public delegate void SampleHandler(double[] Samples);
             
        private IntPtr hWaveIn, hWaveOut;
        private WaveFormatEx format;
        private List<WaveBuffer> buffers;
        private SampleHandler callback;
        private WaveApi.Callback waveInProc = new WaveApi.Callback(WaveInProc);
        private WaveApi.Callback waveOutProc = new WaveApi.Callback(WaveOutProc);
        private volatile bool disposed = false;
        private Thread proc;

        private ConcurrentQueue<WaveBuffer> played = new ConcurrentQueue<WaveBuffer>();
        private ConcurrentQueue<WaveBuffer> recorded = new ConcurrentQueue<WaveBuffer>();

        private double[] samples;

        private GCHandle handle;

        public WaveIo(SampleHandler SampleCallback, int Rate, int Channels, int Bits, double Latency)
        {
            handle = GCHandle.Alloc(this);

            // Create the format used by this stream.
            format = new WaveFormatEx(Rate, Bits, Channels);

            callback = SampleCallback;

            int Buffer = (int)Math.Ceiling(Latency * Rate * Bits * Channels / 8);

            int BufferCount = 6;
            int BufferSize = Buffer / 4;
            BufferSize = BufferSize - (format.BlockAlign - (Buffer % format.BlockAlign)) % format.BlockAlign;

            samples = new double[BufferSize * 8 / Bits];

            // Construct waveOut
            MmException.CheckThrow(WaveApi.waveOutOpen(out hWaveOut, -1, ref format, waveOutProc, (IntPtr)handle, WaveApi.CALLBACK_FUNCTION));

            // Construct waveIn
            MmException.CheckThrow(WaveApi.waveInOpen(out hWaveIn, -1, ref format, waveInProc, (IntPtr)handle, WaveApi.CALLBACK_FUNCTION));
            buffers = new List<WaveBuffer>(BufferCount);
            for (int i = 0; i < BufferCount; ++i)
            {
                WaveBuffer buffer = new WaveBuffer(hWaveIn, hWaveOut, BufferSize);
                buffer.Record();

                buffers.Add(buffer);
            }

            proc = new Thread(new ThreadStart(Proc));
            proc.Start();

            MmException.CheckThrow(WaveApi.waveInStart(hWaveIn));
        }

        ~WaveIo() { Dispose(false); }

        public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }

        private void Dispose(bool Disposing)
        {
            if (disposed) return;
            disposed = true;

            proc.Join();

            WaveApi.waveOutReset(hWaveOut);
            WaveApi.waveInStop(hWaveIn);
            foreach (WaveBuffer i in buffers)
                i.Dispose(Disposing);
            buffers.Clear();
            WaveApi.waveInClose(hWaveIn);
            WaveApi.waveOutClose(hWaveOut);

            if (handle.IsAllocated)
                handle.Free();
        }

        private void Proc()
        {
            while (!disposed)
            {
                WaveBuffer buffer;
                if (recorded.TryDequeue(out buffer))
                {
                    Fixed16x1ToDouble(buffer.Buffer, samples, samples.Length);
                    callback(samples);
                    DoubleToFixed16x1(samples, buffer.Buffer, samples.Length);
                    buffer.PlayHeader.BufferLength = buffer.Size;
                    buffer.Play();
                }

                if (played.TryDequeue(out buffer))
                    buffer.Record();
            }
        }
        
        private static void WaveInProc(IntPtr hWave, int uMsg, IntPtr dwInstance, ref WaveHdr dwParam1, IntPtr dwParam2)
        {
            if (uMsg != WaveApi.MM_WIM_DATA)
                return;

            WaveIo This = (WaveIo)((GCHandle)dwInstance).Target;
            WaveBuffer buffer = (WaveBuffer)((GCHandle)dwParam1.User).Target;

            This.recorded.Enqueue(buffer);
        }

        private static void WaveOutProc(IntPtr hWave, int uMsg, IntPtr dwInstance, ref WaveHdr dwParam1, IntPtr dwParam2)
        {
            if (uMsg != WaveApi.MM_WOM_DONE)
                return;
            
            WaveIo This = (WaveIo)((GCHandle)dwInstance).Target;
            WaveBuffer buffer = (WaveBuffer)((GCHandle)dwParam1.User).Target;

            This.played.Enqueue(buffer);
        }
    }
}
