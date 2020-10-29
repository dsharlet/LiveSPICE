using System;
using System.Runtime.InteropServices;

namespace Audio
{
    public static class Util
    {
        // C# is great, but man generics suck compared to templates.
        public static unsafe void LEi16ToLEf64(IntPtr In, IntPtr Out, int Count)
        {
            short* from = (short*)In.ToPointer();
            double* to = (double*)Out.ToPointer();
            double scale = 1.0 / ((1 << 15) - 1);
            for (int i = 0; i < Count; i++)
                to[i] = from[i] * scale;
        }
        public static unsafe void LEi32ToLEf64(IntPtr In, IntPtr Out, int Count)
        {
            int* from = (int*)In.ToPointer();
            double* to = (double*)Out.ToPointer();
            double scale = 1.0 / ((1L << 31) - 1);
            for (int i = 0; i < Count; i++)
                to[i] = from[i] * scale;
        }
        public static unsafe void LEf32ToLEf64(IntPtr In, IntPtr Out, int Count)
        {
            float* from = (float*)In.ToPointer();
            double* to = (double*)Out.ToPointer();
            for (int i = 0; i < Count; i++)
                to[i] = from[i];
        }

        public static unsafe void LEf64ToLEi16(IntPtr In, IntPtr Out, int Count)
        {
            double* from = (double*)In.ToPointer();
            short* to = (short*)Out.ToPointer();
            double max = (1 << 15) - 1;
            for (int i = 0; i < Count; i++)
                to[i] = (short)Math.Max(Math.Min(from[i] * max, max), -max);
        }
        public static unsafe void LEf64ToLEi32(IntPtr In, IntPtr Out, int Count)
        {
            double* from = (double*)In.ToPointer();
            int* to = (int*)Out.ToPointer();
            double max = (1L << 31) - 1;
            for (int i = 0; i < Count; i++)
                to[i] = (int)Math.Max(Math.Min(from[i] * max, max), -max);
        }
        public static unsafe void LEf64ToLEf32(IntPtr In, IntPtr Out, int Count)
        {
            double* from = (double*)In.ToPointer();
            float* to = (float*)Out.ToPointer();
            for (int i = 0; i < Count; i++)
                to[i] = (float)from[i];
        }

        public static unsafe double Amplify(IntPtr Samples, int Count, double Gain)
        {
            double* s = (double*)Samples.ToPointer();
            double peak = 0.0;
            for (int i = 0; i < Count; i++)
            {
                s[i] = s[i] * Gain;
                // TODO: Absolute value of s[i]?
                peak = Math.Max(peak, s[i]);
            }
            return peak;
        }

        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)] public static extern void CopyMemory(IntPtr dest, IntPtr src, int size);
        [DllImport("Kernel32.dll", EntryPoint = "RtlZeroMemory", SetLastError = false)] public static extern void ZeroMemory(IntPtr dest, int size);
    }
}
