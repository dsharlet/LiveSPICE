using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace AudioIo
{
    /// <summary>
    /// Helper to manage the memory associated with a WAVEHDR.
    /// </summary>
    class WaveBuffer : IDisposable
    {
        private GCHandle handle;
        private IntPtr hWaveIn, hWaveOut;
        public WaveHdr RecordHeader, PlayHeader;
        private GCHandle pinHdrIn, pinHdrOut;
        private IntPtr buffer;
        private int size;
        private bool disposed = false;

        public IntPtr Buffer { get { return buffer; } }
        public int Size { get { return size; } }

        public WaveBuffer(IntPtr hWaveIn, IntPtr hWaveOut, int Size)
        {
            handle = GCHandle.Alloc(this);

            this.hWaveIn = hWaveIn;
            this.hWaveOut = hWaveOut;

            buffer = Marshal.AllocHGlobal(Size);
            size = Size;

            RecordHeader = new WaveHdr();
            RecordHeader.Data = buffer;
            RecordHeader.User = (IntPtr)handle;
            RecordHeader.BufferLength = size;
            RecordHeader.Flags = 0;
            pinHdrIn = GCHandle.Alloc(RecordHeader, GCHandleType.Pinned);
            MmException.CheckThrow(WaveApi.waveInPrepareHeader(hWaveIn, ref RecordHeader, Marshal.SizeOf(RecordHeader)));

            PlayHeader = new WaveHdr();
            PlayHeader.Data = buffer;
            PlayHeader.User = (IntPtr)handle;
            PlayHeader.BufferLength = size;
            PlayHeader.Flags = 0;
            pinHdrOut = GCHandle.Alloc(PlayHeader, GCHandleType.Pinned);
            MmException.CheckThrow(WaveApi.waveOutPrepareHeader(hWaveOut, ref PlayHeader, Marshal.SizeOf(PlayHeader)));
        }

        ~WaveBuffer() { Dispose(false); }

        public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }

        public void Dispose(bool Disposing)
        {
            if (disposed) return;

            // Not checked intentionally.
            WaveApi.waveInUnprepareHeader(hWaveIn, ref RecordHeader, Marshal.SizeOf(RecordHeader));
            WaveApi.waveOutUnprepareHeader(hWaveOut, ref PlayHeader, Marshal.SizeOf(PlayHeader));

            if (buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(buffer);
                buffer = IntPtr.Zero;
            }

            if (handle.IsAllocated)
                handle.Free();

            if (pinHdrIn.IsAllocated)
                pinHdrIn.Free();
            if (pinHdrOut.IsAllocated)
                pinHdrOut.Free();

            disposed = true;
        }
        
        public void Record()
        {
            RecordHeader.BufferLength = size;
            MmException.CheckThrow(WaveApi.waveInAddBuffer(hWaveIn, ref RecordHeader, Marshal.SizeOf(RecordHeader)));
        }

        public void Play()
        {
            MmException.CheckThrow(WaveApi.waveOutWrite(hWaveOut, ref PlayHeader, Marshal.SizeOf(PlayHeader)));
        }
    }
}
