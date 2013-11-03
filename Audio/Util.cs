using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Audio
{
    public static class Util
    {
        [DllImport("NativeAudioUtils.dll", CallingConvention = CallingConvention.Cdecl)] public static extern void LEi16ToLEf32(IntPtr In, float[] Out, int Count);
        [DllImport("NativeAudioUtils.dll", CallingConvention = CallingConvention.Cdecl)] public static extern void LEi16ToLEf64(IntPtr In, double[] Out, int Count);
        [DllImport("NativeAudioUtils.dll", CallingConvention = CallingConvention.Cdecl)] public static extern void LEi32ToLEf32(IntPtr In, float[] Out, int Count);
        [DllImport("NativeAudioUtils.dll", CallingConvention = CallingConvention.Cdecl)] public static extern void LEi32ToLEf64(IntPtr In, double[] Out, int Count);
        [DllImport("NativeAudioUtils.dll", CallingConvention = CallingConvention.Cdecl)] public static extern void LEf32ToLEf64(IntPtr In, double[] Out, int Count);

        [DllImport("NativeAudioUtils.dll", CallingConvention = CallingConvention.Cdecl)] public static extern void LEf32ToLEi16(float[] In, IntPtr Out, int Count);
        [DllImport("NativeAudioUtils.dll", CallingConvention = CallingConvention.Cdecl)] public static extern void LEf64ToLEi16(double[] In, IntPtr Out, int Count);
        [DllImport("NativeAudioUtils.dll", CallingConvention = CallingConvention.Cdecl)] public static extern void LEf32ToLEi32(float[] In, IntPtr Out, int Count);
        [DllImport("NativeAudioUtils.dll", CallingConvention = CallingConvention.Cdecl)] public static extern void LEf64ToLEi32(double[] In, IntPtr Out, int Count);
        [DllImport("NativeAudioUtils.dll", CallingConvention = CallingConvention.Cdecl)] public static extern void LEf64ToLEf32(double[] In, IntPtr Out, int Count);

        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, int size);
        [DllImport("Kernel32.dll", EntryPoint = "RtlZeroMemory", SetLastError = false)]
        public static extern void ZeroMemory(IntPtr dest, int size);

        public static void SamplesToLEf64(IntPtr In, SampleType Type, double[] Out)
        {
            switch (Type)
            {
                case SampleType.i16: Util.LEi16ToLEf64(In, Out, Out.Length); break;
                case SampleType.i32: Util.LEi32ToLEf64(In, Out, Out.Length); break;
                case SampleType.f32: Util.LEf32ToLEf64(In, Out, Out.Length); break;
                case SampleType.f64: Marshal.Copy(In, Out, 0, Out.Length); break;
                default: throw new NotImplementedException("Unsupported sample type.");
            }
        }

        public static void LEf64ToSamples(double[] In, IntPtr Out, SampleType Type)
        {
            switch (Type)
            {
                case SampleType.i16: Util.LEf64ToLEi16(In, Out, In.Length); break;
                case SampleType.i32: Util.LEf64ToLEi32(In, Out, In.Length); break;
                case SampleType.f32: Util.LEf64ToLEf32(In, Out, In.Length); break;
                case SampleType.f64: Marshal.Copy(In, 0, Out, In.Length); break;
                default: throw new NotImplementedException("Unsupported sample type.");
            }
        }

        public static void CopySamples(IntPtr In, SampleType InType, IntPtr Out, SampleType OutType, int Count)
        {
            if (InType == OutType)
                CopyMemory(Out, In, Count * SampleSize(OutType));
            else
                throw new NotImplementedException("Unsupported copy operation.");
        }

        public static void ZeroSamples(IntPtr Buffer, SampleType Type, int Count)
        {
            ZeroMemory(Buffer, SampleSize(Type) * Count);
        }

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
