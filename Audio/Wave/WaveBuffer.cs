﻿using System;
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
    class WaveBuffer : IDisposable
    {
        private GCHandle handle;
        private IntPtr hWaveIn, hWaveOut;
        public WAVEHDR RecordHeader, PlayHeader;
        private GCHandle pinHdrIn, pinHdrOut;
        private SampleBuffer buffer;
        private int size;

        private bool disposed = false;

        public SampleBuffer Buffer { get { return buffer; } }

        public WaveBuffer(IntPtr hWaveIn, IntPtr hWaveOut, SampleType Type, int Count, int Size)
        {
            handle = GCHandle.Alloc(this);

            this.hWaveIn = hWaveIn;
            this.hWaveOut = hWaveOut;

            size = Size;
            buffer = new SampleBuffer(Marshal.AllocHGlobal(Size), Type, Count);

            RecordHeader = new WAVEHDR();
            RecordHeader.lpData = buffer;
            RecordHeader.dwUser = (IntPtr)handle;
            RecordHeader.dwBufferLength = (uint)size;
            RecordHeader.dwFlags = 0;
            pinHdrIn = GCHandle.Alloc(RecordHeader, GCHandleType.Pinned);
            MmException.CheckThrow(Winmm.waveInPrepareHeader(hWaveIn, ref RecordHeader, Marshal.SizeOf(RecordHeader)));

            PlayHeader = new WAVEHDR();
            PlayHeader.lpData = buffer;
            PlayHeader.dwUser = (IntPtr)handle;
            PlayHeader.dwBufferLength = (uint)size;
            PlayHeader.dwFlags = 0;
            pinHdrOut = GCHandle.Alloc(PlayHeader, GCHandleType.Pinned);
            MmException.CheckThrow(Winmm.waveOutPrepareHeader(hWaveOut, ref PlayHeader, Marshal.SizeOf(PlayHeader)));
        }

        ~WaveBuffer() { Dispose(false); }

        public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }

        public void Dispose(bool Disposing)
        {
            if (disposed) return;

            // Not checked intentionally.
            Winmm.waveInUnprepareHeader(hWaveIn, ref RecordHeader, Marshal.SizeOf(RecordHeader));
            Winmm.waveOutUnprepareHeader(hWaveOut, ref PlayHeader, Marshal.SizeOf(PlayHeader));

            buffer.Dispose();

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
            RecordHeader.dwBufferLength = (uint)size;
            MmException.CheckThrow(Winmm.waveInAddBuffer(hWaveIn, ref RecordHeader, Marshal.SizeOf(RecordHeader)));
        }

        public void Play()
        {
            MmException.CheckThrow(Winmm.waveOutWrite(hWaveOut, ref PlayHeader, Marshal.SizeOf(PlayHeader)));
        }
    }
}
