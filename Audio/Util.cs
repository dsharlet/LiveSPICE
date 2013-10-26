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
        [DllImport("NativeAudioUtils.dll", CallingConvention = CallingConvention.Cdecl)] public static extern void LEf32ToLEi16(float[] In, IntPtr Out, int Count);
        [DllImport("NativeAudioUtils.dll", CallingConvention = CallingConvention.Cdecl)] public static extern void LEf64ToLEi16(double[] In, IntPtr Out, int Count);
        [DllImport("NativeAudioUtils.dll", CallingConvention = CallingConvention.Cdecl)] public static extern void LEf32ToLEi32(float[] In, IntPtr Out, int Count);
        [DllImport("NativeAudioUtils.dll", CallingConvention = CallingConvention.Cdecl)] public static extern void LEf64ToLEi32(double[] In, IntPtr Out, int Count);
    }
}
