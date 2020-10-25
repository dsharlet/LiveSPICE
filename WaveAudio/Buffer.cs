using System;
using System.Runtime.InteropServices;

namespace WaveAudio
{
    /// <summary>
    /// Helper to manage the memory associated with a WAVEHDR.
    /// </summary>
    class Buffer : IDisposable
    {
        protected WAVEHDR header;
        protected GCHandle headerPin;
        protected byte[] data;
        protected GCHandle dataPin;

        protected bool disposed = false;

        public IntPtr Data { get { return header.lpData; } }

        public bool Done { get { return (header.dwFlags & WaveHdrFlags.WHDR_DONE) != 0; } }

        private Audio.SampleBuffer samples;
        public Audio.SampleBuffer Samples { get { return samples; } }

        public Buffer(WAVEFORMATEX Format, int Count)
        {
            samples = new Audio.SampleBuffer(Count) { Tag = this };

            int size = BlockAlignedSize(Format, Count);
            data = new byte[size];
            dataPin = GCHandle.Alloc(data, GCHandleType.Pinned);
            header = new WAVEHDR();
            headerPin = GCHandle.Alloc(header, GCHandleType.Pinned);
            header.lpData = dataPin.AddrOfPinnedObject();
            header.dwBufferLength = (uint)size;
            header.dwFlags = 0;
        }

        ~Buffer() { Dispose(false); }

        public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }

        public virtual void Dispose(bool Disposing)
        {
            if (disposed) return;

            if (headerPin.IsAllocated)
                headerPin.Free();

            if (dataPin.IsAllocated)
                dataPin.Free();

            disposed = true;
        }

        private static int BlockAlignedSize(WAVEFORMATEX Format, int Count)
        {
            int align = Format.nBlockAlign;
            int size = Count * Format.wBitsPerSample / 8;

            return size + (align - (size % align)) % align;
        }
    }
}
