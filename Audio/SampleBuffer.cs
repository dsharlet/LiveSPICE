using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Audio
{
    public enum SampleType
    {
        i16 = 0,
        i32 = 1,
        f32 = 2,
        f64 = 3,
    };

    /// <summary>
    /// This object provides lazy conversion between raw sample buffers and double[].
    /// The SampleBuffer object maintains an internal state to indicate whether the raw buffer or 
    /// sample array are valid. Neither or both can be valid simultaneously.
    /// </summary>
    public class SampleBuffer : IDisposable
    {
        private IntPtr raw = IntPtr.Zero;
        private bool rawValid = false;

        private double[] samples;
        private bool samplesValid = false;

        private bool locked = false;

        /// <summary>
        /// Number of samples contained in this buffer.
        /// </summary>
        public int Count { get { return samples.Length; } }

        private object tag = null;
        /// <summary>
        /// User defined tag object.
        /// </summary>
        public object Tag { get { return tag; } set { tag = value; } }

        public SampleBuffer(int Count)
        {
            samples = new double[Count];
            raw = Marshal.AllocHGlobal(Count * sizeof(double));
        }

        ~SampleBuffer() { FreeRaw(); }
        public void Dispose() { FreeRaw(); }
        private void FreeRaw()
        {
            if (raw != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(raw);
                raw = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Lock the sample array.
        /// </summary>
        /// <param name="Read">True if the sample array will be read from.</param>
        /// <param name="Write">True if the sample array will be written to.</param>
        /// <returns>The sample array.</returns>
        public double[] LockSamples(bool Read, bool Write)
        {
            if (locked)
                throw new InvalidOperationException("SampleBuffer is already locked.");

            if (Read)
                SyncSamples();
            if (Write)
            {
                samplesValid = true;
                rawValid = false;
            }
            locked = true;
            return samples;
        }

        /// <summary>
        /// Lock the raw buffer.
        /// </summary>
        /// <param name="Buffer"></param>
        /// <param name="Read">True if the raw buffer will be read from.</param>
        /// <param name="Write">True if the raw buffer will be written to.</param>
        public IntPtr LockRaw(bool Read, bool Write)
        {
            if (locked)
                throw new InvalidOperationException("SampleBuffer is already locked.");

            if (Read)
                SyncRaw();
            if (Write)
            {
                rawValid = true;
                samplesValid = false;
            }
            locked = true;
            return raw;
        }

        /// <summary>
        /// Unlock the buffer.
        /// </summary>
        public void Unlock() { locked = false; }

        /// <summary>
        /// Update the raw buffer.
        /// </summary>
        public void SyncRaw()
        {
            if (samplesValid)
            {
                Marshal.Copy(samples, 0, raw, Count);
                rawValid = true;
            }
        }

        /// <summary>
        /// Update the sample array.
        /// </summary>
        public void SyncSamples()
        {
            if (rawValid)
            {
                Marshal.Copy(raw, samples, 0, Count);
                samplesValid = true;
            }
        }

        /// <summary>
        /// Set this buffer to have the zero signal.
        /// </summary>
        public void Clear()
        {
            Util.ZeroMemory(raw, Count * sizeof(double));
            samplesValid = false;
            rawValid = true;
        }
    }

    public class RawLock : IDisposable
    {
        private IntPtr raw;
        private SampleBuffer target;

        public RawLock(SampleBuffer Target, bool Read, bool Write)
        {
            target = Target;
            raw = Target.LockRaw(Read, Write);
        }

        ~RawLock() { Unlock(); }
        public void Dispose() { Unlock(); }
        private void Unlock() { if (raw != IntPtr.Zero) target.Unlock(); raw = IntPtr.Zero; }
        
        public int Count { get { return target.Count; } }

        public static implicit operator IntPtr(RawLock Lock) { return Lock.raw; }
    }

    public class SamplesLock : IDisposable, IEnumerable
    {
        private double[] samples;
        private SampleBuffer target;

        public SamplesLock(SampleBuffer Target, bool Read, bool Write)
        {
            target = Target;
            samples = Target.LockSamples(Read, Write);
        }

        ~SamplesLock() { Unlock(); }
        public void Dispose() { Unlock(); }
        private void Unlock() { if (samples != null) target.Unlock(); samples = null; }
        
        public int Count { get { return samples.Length; } }

        public double this[int i] { get { return samples[i]; } set { samples[i] = value; } }

        public static implicit operator double[](SamplesLock Lock) { return Lock.samples; }

        // IEnumerable
        IEnumerator IEnumerable.GetEnumerator() { return samples.GetEnumerator(); }
    }
}
