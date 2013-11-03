using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Audio
{
    /// <summary>
    /// Helper to manage the memory associated with a WAVEHDR.
    /// </summary>
    abstract class WaveBuffer : IDisposable
    {
        protected GCHandle handle;
        protected WAVEHDR header;
        protected GCHandle pin;
        protected double[] samples;
        protected int size;
        protected SampleType type;

        protected bool disposed = false;

        public SampleType Type { get { return type; } }
        public IntPtr Buffer { get { return header.lpData; } }
        public double[] Samples { get { return samples; } }
        public int Size { get { return size; } }

        public bool Done { get { return (header.dwFlags & WaveHdrFlags.WHDR_DONE) != 0; } }

        public WaveBuffer(WAVEFORMATEX Format, int Count)
        {
            handle = GCHandle.Alloc(this);

            type = FormatSampleType(Format);
            size = BlockAlignedSize(Format, Count);
            samples = new double[Count];

            header = new WAVEHDR();
            header.lpData = Marshal.AllocHGlobal(size);
            header.dwUser = (IntPtr)handle;
            header.dwBufferLength = (uint)size;
            header.dwFlags = 0;
            pin = GCHandle.Alloc(header, GCHandleType.Pinned);
        }

        ~WaveBuffer() { Dispose(false); }

        public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }

        public virtual void Dispose(bool Disposing)
        {
            if (disposed) return;

            if (pin.IsAllocated)
                pin.Free();

            if (header.lpData != IntPtr.Zero)
                Marshal.FreeHGlobal(header.lpData);

            if (handle.IsAllocated)
                handle.Free();

            disposed = true;
        }

        public abstract SampleBuffer NewSampleBuffer();

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
