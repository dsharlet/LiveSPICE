using System;
using System.Diagnostics;
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
    public class SampleBuffer
    {
        private IntPtr raw;
        private int count;
        private SampleType type;
        private bool rawValid = false;

        private double[] samples;
        private bool samplesValid = false;

        private bool locked = false;

        /// <summary>
        /// Type of samples contained in this buffer.
        /// </summary>
        public SampleType Type { get { return type; } }
        /// <summary>
        /// Number of samples contained in this buffer.
        /// </summary>
        public int Count { get { return count; } }

        private object tag = null;
        /// <summary>
        /// User defined tag object.
        /// </summary>
        public object Tag { get { return tag; } set { tag = value; } }

        private SampleBuffer(IntPtr Raw, SampleType Type, int Count)
        {
            count = Count;
            type = Type;
            raw = Raw;
        }

        private SampleBuffer(IntPtr Raw, SampleType Type, double[] Samples)
            : this(Raw, Type, Samples.Length)
        {
            samples = Samples;
        }

        /// <summary>
        /// Create a new buffer for an input buffer. The raw buffer is marked valid.
        /// </summary>
        /// <param name="Buffer"></param>
        /// <param name="Type"></param>
        /// <param name="Samples"></param>
        /// <returns></returns>
        public static SampleBuffer NewInputBuffer(IntPtr Raw, SampleType Type, double[] Samples)
        {
            return new SampleBuffer(Raw, Type, Samples) { rawValid = true };
        }

        /// <summary>
        /// Create a new buffer for an output buffer. Neither buffer nor array is marked valid.
        /// </summary>
        /// <param name="Buffer"></param>
        /// <param name="Type"></param>
        /// <param name="Samples"></param>
        /// <returns></returns>
        public static SampleBuffer NewOutputBuffer(IntPtr Buffer, SampleType Type, double[] Samples)
        {
            return new SampleBuffer(Buffer, Type, Samples);
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
                rawValid = false;
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
                samplesValid = false;
            locked = true;
            return raw;
        }

        /// <summary>
        /// Unlock the buffer.
        /// </summary>
        public void Unlock()
        {
            if (!locked)
                throw new InvalidOperationException("SampleBuffer is not locked.");

            locked = false;
        }

        /// <summary>
        /// Update the raw buffer.
        /// </summary>
        public void SyncRaw()
        {
            if (samplesValid)
                Util.LEf64ToSamples(samples, raw, type);
            rawValid = true;
        }

        /// <summary>
        /// Update the sample array.
        /// </summary>
        public void SyncSamples()
        {
            if (rawValid)
                Util.SamplesToLEf64(raw, type, samples);
            samplesValid = true;
        }

        /// <summary>
        /// Set this buffer to have the zero signal.
        /// </summary>
        public void Clear()
        {
            Util.ZeroSamples(raw, type, count);
            samplesValid = false;
            rawValid = true;
        }

        /// <summary>
        /// Copy samples from another SampleBuffer.
        /// </summary>
        /// <param name="From"></param>
        public void Copy(SampleBuffer From)
        {
            IntPtr a = From.LockRaw(true, false);
            IntPtr b = LockRaw(false, true);
            Util.CopySamples(a, From.Type, b, type, Count);
            Unlock();
            From.Unlock();
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

        private void Unlock() { if (raw != IntPtr.Zero) Unlock(); raw = IntPtr.Zero; }

        public void Dispose() { Unlock(); }

        public int Count { get { return target.Count; } }
        public SampleType Type { get { return target.Type; } }

        public static implicit operator IntPtr(RawLock Lock) { return Lock.raw; }
    }

    public class SamplesLock : IDisposable
    {
        private double[] samples;
        private SampleBuffer target;

        public SamplesLock(SampleBuffer Target, bool Read, bool Write)
        {
            target = Target;
            samples = Target.LockSamples(Read, Write);
        }

        ~SamplesLock() { Unlock(); }

        private void Unlock() { if (samples != null) target.Unlock(); samples = null; }

        public void Dispose() { Unlock(); }

        public int Count { get { return samples.Length; } }

        public double this[int i] { get { return samples[i]; } set { samples[i] = value; } }

        public static implicit operator double[](SamplesLock Lock) { return Lock.samples; }
    }
}
