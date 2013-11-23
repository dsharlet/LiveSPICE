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
        [DllImport("NativeUtil.dll", CallingConvention = CallingConvention.Cdecl)] public static extern void LEi16ToLEf32(IntPtr In, IntPtr Out, int Count);
        [DllImport("NativeUtil.dll", CallingConvention = CallingConvention.Cdecl)] public static extern void LEi16ToLEf64(IntPtr In, IntPtr Out, int Count);
        [DllImport("NativeUtil.dll", CallingConvention = CallingConvention.Cdecl)] public static extern void LEi32ToLEf32(IntPtr In, IntPtr Out, int Count);
        [DllImport("NativeUtil.dll", CallingConvention = CallingConvention.Cdecl)] public static extern void LEi32ToLEf64(IntPtr In, IntPtr Out, int Count);
        [DllImport("NativeUtil.dll", CallingConvention = CallingConvention.Cdecl)] public static extern void LEf32ToLEf64(IntPtr In, IntPtr Out, int Count);

        [DllImport("NativeUtil.dll", CallingConvention = CallingConvention.Cdecl)] public static extern void LEf32ToLEi16(IntPtr In, IntPtr Out, int Count);
        [DllImport("NativeUtil.dll", CallingConvention = CallingConvention.Cdecl)] public static extern void LEf64ToLEi16(IntPtr In, IntPtr Out, int Count);
        [DllImport("NativeUtil.dll", CallingConvention = CallingConvention.Cdecl)] public static extern void LEf32ToLEi32(IntPtr In, IntPtr Out, int Count);
        [DllImport("NativeUtil.dll", CallingConvention = CallingConvention.Cdecl)] public static extern void LEf64ToLEi32(IntPtr In, IntPtr Out, int Count);
        [DllImport("NativeUtil.dll", CallingConvention = CallingConvention.Cdecl)] public static extern void LEf64ToLEf32(IntPtr In, IntPtr Out, int Count);
        
        [DllImport("NativeUtil.dll", CallingConvention = CallingConvention.Cdecl)] public static extern double Amplify(IntPtr x, int Count, double Gain);

        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)] public static extern void CopyMemory(IntPtr dest, IntPtr src, int size);
        [DllImport("Kernel32.dll", EntryPoint = "RtlZeroMemory", SetLastError = false)] public static extern void ZeroMemory(IntPtr dest, int size);
    }
}
