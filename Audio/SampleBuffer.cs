using System;
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
    /// Sample buffer object, provides lazy conversion between native pointers and double[].
    /// </summary>
    public class SampleBuffer : IDisposable
    {
        private IntPtr buffer;
        private int count;
        private SampleType type;
        private double[] samples;

        private bool samplesValid;
        
        public void ToBuffer()
        {
            if (!samplesValid)
                return;
            
            switch (type)
            {
                case SampleType.i16: Util.LEf64ToLEi16(samples, buffer, count); break;
                case SampleType.i32: Util.LEf64ToLEi32(samples, buffer, count); break;
                case SampleType.f32: Util.LEf64ToLEf32(samples, buffer, count); break;
                case SampleType.f64: Marshal.Copy(samples, 0, buffer, count); break;
                default: throw new NotImplementedException("Unsupported sample type.");
            }
            samplesValid = false;
        }

        public void ToSamples()
        {
            if (samplesValid)
                return;

            if (samples == null)
                samples = new double[count];

            switch (type)
            {
                case SampleType.i16: Util.LEi16ToLEf64(buffer, samples, count); break;
                case SampleType.i32: Util.LEi32ToLEf64(buffer, samples, count); break;
                case SampleType.f32: Util.LEf32ToLEf64(buffer, samples, count); break;
                case SampleType.f64: Marshal.Copy(buffer, samples, 0, count); break;
                default: throw new NotImplementedException("Unsupported sample type.");
            }
            samplesValid = true;
        }

        public IntPtr Buffer { get { ToBuffer(); return buffer; } }
        public int Size { get { return count * SampleSize(type); } }
        public SampleType Type { get { return type; } }
        public int Count { get { return count; } }
        public double[] Samples { get { ToSamples(); return samples; } }

        public double this[int i] { get { return Samples[i]; } set { Samples[i] = value; } }

        public SampleBuffer(IntPtr Buffer, SampleType Type, int Count)
        {
            count = Count;
            type = Type;
            buffer = Buffer;
            samplesValid = false;
        }
        public SampleBuffer(IntPtr Buffer, SampleType Type, double[] Samples)
        {
            count = Samples.Length;
            type = Type;
            buffer = Buffer;
            samples = Samples;
            samplesValid = false;
        }
        ~SampleBuffer() { Dispose(false); }

        public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }
        
        private bool disposed = false;
        private void Dispose(bool Disposing)
        {
            if (disposed) return;

            if (buffer != IntPtr.Zero)
                Marshal.FreeHGlobal(buffer);
            buffer = IntPtr.Zero;

            disposed = true;
        }

        public static implicit operator double[](SampleBuffer x) { return x.Samples; }
        public static implicit operator IntPtr(SampleBuffer x) { return x.Buffer; }

        private static int SampleSize(SampleType Type)
        {
            switch (Type)
            {
                case SampleType.i16: return 2;
                case SampleType.i32: return 4;
                case SampleType.f32: return 4;
                case SampleType.f64: return 8;
                default: throw new NotImplementedException("Unsupported sample type.");
            }
        }
    }
}
