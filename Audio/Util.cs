using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Audio
{
    public static class Util
    {
        // C# is great, but man generics suck compared to templates.
        // TODO: simd?
        public static unsafe void LEi16ToLEf64(IntPtr source, IntPtr destination, uint count)
        {
            short* from = (short*)source.ToPointer();
            double* to = (double*)destination.ToPointer();
            double scale = 1.0 / ((1 << 15) - 1);
            for (int i = 0; i < count; i++)
                to[i] = from[i] * scale;
        }
        public static unsafe void LEi32ToLEf64(IntPtr source, IntPtr destination, uint count)
        {
            int* from = (int*)source.ToPointer();
            double* to = (double*)destination.ToPointer();
            double scale = 1.0 / ((1L << 31) - 1);
            for (int i = 0; i < count; i++)
                to[i] = from[i] * scale;
        }
        public static unsafe void LEf32ToLEf64(IntPtr source, IntPtr destination, uint count)
        {
            float* from = (float*)source.ToPointer();
            double* to = (double*)destination.ToPointer();
            for (int i = 0; i < count; i++)
                to[i] = from[i];
        }

        public static unsafe void LEf64ToLEi16(IntPtr source, IntPtr destination, uint count)
        {
            double* from = (double*)source.ToPointer();
            short* to = (short*)destination.ToPointer();
            double max = (1 << 15) - 1;
            for (int i = 0; i < count; i++)
                to[i] = (short)Math.Max(Math.Min(from[i] * max, max), -max);
        }
        public static unsafe void LEf64ToLEi32(IntPtr source, IntPtr destination, uint count)
        {
            double* from = (double*)source.ToPointer();
            int* to = (int*)destination.ToPointer();
            double max = (1L << 31) - 1;
            for (int i = 0; i < count; i++)
                to[i] = (int)Math.Max(Math.Min(from[i] * max, max), -max);
        }
        public static unsafe void LEf64ToLEf32(IntPtr source, IntPtr destination, uint count)
        {
            double* from = (double*)source.ToPointer();
            float* to = (float*)destination.ToPointer();
            for (int i = 0; i < count; i++)
                to[i] = (float)from[i];
        }

        public static unsafe double Amplify(IntPtr Samples, uint count, double Gain)
        {
            double* s = (double*)Samples.ToPointer();
            double peak = 0.0;
            for (int i = 0; i < count; i++)
            {
                s[i] = s[i] * Gain;
                // TODO: Absolute value of s[i]?
                peak = Math.Max(peak, s[i]);
            }
            return peak;
        }

        public static unsafe void CopyMemory(IntPtr destination, IntPtr source, uint count) => Unsafe.CopyBlock(destination.ToPointer(), source.ToPointer(), count);

        public static unsafe void ZeroMemory(IntPtr startAddress, uint count) => Unsafe.InitBlockUnaligned(startAddress.ToPointer(), 0, count);
    }
}
