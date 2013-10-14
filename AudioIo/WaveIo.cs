using System;
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
        public delegate void SampleHandler(double[] Samples, int Rate);
             
        private IntPtr hWaveIn, hWaveOut;
        private WaveFormatEx format;
        private List<WaveBuffer> buffers;
        private SampleHandler callback;
        private WaveApi.Callback waveProc = new WaveApi.Callback(WaveProc);
        private volatile bool disposed = false;

        private double[] samples;

        private GCHandle handle;

        public WaveIo(SampleHandler SampleCallback, int Rate, int Channels, int Bits, double Latency)
        {
            handle = GCHandle.Alloc(this);

            // Create the format used by this stream.
            format = new WaveFormatEx(Rate, Bits, Channels);

            callback = SampleCallback;

            int Buffer = (int)Math.Ceiling(Latency * Rate * Bits * Channels / 8);

            int BufferCount = 4;
            int BufferSize = Buffer / 4;
            BufferSize = BufferSize - (format.BlockAlign - (Buffer % format.BlockAlign)) % format.BlockAlign;

            samples = new double[BufferSize * 8 / Bits];

            // Construct waveOut
            MmException.CheckThrow(WaveApi.waveOutOpen(out hWaveOut, -1, ref format, waveProc, (IntPtr)handle, WaveApi.CALLBACK_FUNCTION));

            // Construct waveIn
            MmException.CheckThrow(WaveApi.waveInOpen(out hWaveIn, -1, ref format, waveProc, (IntPtr)handle, WaveApi.CALLBACK_FUNCTION));
            buffers = new List<WaveBuffer>(BufferCount);
            for (int i = 0; i < BufferCount; ++i)
            {
                WaveBuffer buffer = new WaveBuffer(hWaveIn, hWaveOut, BufferSize);
                buffer.Record();

                buffers.Add(buffer);
            }

            MmException.CheckThrow(WaveApi.waveInStart(hWaveIn));
        }

        ~WaveIo() { Dispose(false); }

        public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }

        private void Dispose(bool Disposing)
        {
            if (disposed) return;
            disposed = true;

            WaveApi.waveOutReset(hWaveOut);
            WaveApi.waveInStop(hWaveIn);

            if (Disposing)
            {
                foreach (WaveBuffer i in buffers)
                    i.Dispose();
            }
            
            buffers.Clear();
            WaveApi.waveInClose(hWaveIn);
            WaveApi.waveOutClose(hWaveOut);

            if (handle.IsAllocated)
                handle.Free();
        }
        
        private void OnWimData(WaveHdr hdr)
        {
            if (disposed)
                return;

            WaveBuffer buffer = (WaveBuffer)((GCHandle)hdr.User).Target;
            Fixed16x1ToDouble(hdr.Data, samples, samples.Length);
            callback(samples, format.SamplesPerSec);
            DoubleToFixed16x1(samples, hdr.Data, samples.Length);
            buffer.PlayHeader.BufferLength = hdr.BytesRecorded;
            buffer.Play();
        }

        private void OnWomDone(WaveHdr hdr)
        {
            if ((hdr.Flags & Whdr.Done) == 0 || disposed)
                return;

            WaveBuffer buffer = (WaveBuffer)((GCHandle)hdr.User).Target;
            buffer.Record();
        }

        private static void WaveProc(IntPtr hWave, int uMsg, IntPtr dwInstance, ref WaveHdr dwParam1, IntPtr dwParam2)
        {
            try
            {
                switch (uMsg)
                {
                    case WaveApi.MM_WIM_DATA: ((WaveIo)((GCHandle)dwInstance).Target).OnWimData(dwParam1); break;
                    case WaveApi.MM_WOM_DONE: ((WaveIo)((GCHandle)dwInstance).Target).OnWomDone(dwParam1); break;
                }
            }
            catch (System.Exception e)
            {
                Debug.Fail(e.GetType().ToString(), e.ToString());
            }
        }
    }
}
