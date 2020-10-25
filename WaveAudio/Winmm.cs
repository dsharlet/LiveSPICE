using System;
using System.Runtime.InteropServices;

namespace WaveAudio
{
    public enum MMRESULT : uint
    {
        NOERROR = 0,
        ERROR = 1,
        BADDEVICEID = 2,
        NOTENABLED = 3,
        ALLOCATED = 4,
        INVALHANDLE = 5,
        NODRIVER = 6,
        NOMEM = 7,
        NOTSUPPORTED = 8,
        BADERRNUM = 9,
        INVALFLAG = 10,
        INVALPARAM = 11,
        HANDLEBUSY = 12,
        INVALIDALIAS = 13,
        BADDB = 14,
        KEYNOTFOUND = 15,
        READERROR = 16,
        WRITEERROR = 17,
        DELETEERROR = 18,
        VALNOTFOUND = 19,
        NODRIVERCB = 20,
        BADFORMAT = 32,
        STILLPLAYING = 33,
        UNPREPARED = 34
    }

    public class MmException : Exception
    {
        private MMRESULT result;

        public MmException(MMRESULT Result) : base(Result.ToString())
        {
            result = Result;
        }

        public MMRESULT Result { get { return result; } }

        public static void CheckThrow(MMRESULT result)
        {
            if (result != MMRESULT.NOERROR)
                throw new MmException(result);
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public struct WAVEFORMATEX
    {
        public ushort wFormatTag;
        public ushort nChannels;
        public uint nSamplesPerSec;
        public uint nAvgBytesPerSec;
        public ushort nBlockAlign;
        public ushort wBitsPerSample;
        public ushort cbSize;

        public WAVEFORMATEX(int Rate, int Bits, int Channels)
        {
            wFormatTag = (ushort)1;
            nChannels = (ushort)Channels;
            nSamplesPerSec = (uint)Rate;
            wBitsPerSample = (ushort)Bits;
            cbSize = 0;

            nBlockAlign = (ushort)(Channels * (Bits / 8));
            nAvgBytesPerSec = (uint)(Rate * Channels * Bits) / 8;
        }
    }

    [Flags]
    public enum WaveHdrFlags : uint
    {
        WHDR_DONE = 1,
        WHDR_PREPARED = 2,
        WHDR_BEGINLOOP = 4,
        WHDR_ENDLOOP = 8,
        WHDR_INQUEUE = 16
    }

    // This needs to be a class so pinning works correctly(?)
    [StructLayout(LayoutKind.Sequential)]
    public class WAVEHDR
    {
        public IntPtr lpData; // pointer to locked data buffer
        public uint dwBufferLength; // length of data buffer
        public uint dwBytesRecorded; // used for input only
        public IntPtr dwUser; // for client's use
        public WaveHdrFlags dwFlags; // assorted flags (see defines)
        public uint dwLoops; // loop control counter
        public IntPtr lpNext; // PWaveHdr, reserved for driver
        public IntPtr reserved; // reserved for driver
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Auto)]
    public struct WAVEOUTCAPS
    {
        public ushort wMid;
        public ushort wPid;
        public uint vDriverVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szPname;
        public uint dwFormats;
        public ushort wChannels;
        public ushort wReserved1;
        public uint dwSupport;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Auto)]
    public struct WAVEINCAPS
    {
        public ushort wMid;
        public ushort wPid;
        public uint vDriverVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szPname;
        public uint dwFormats;
        public ushort wChannels;
        public ushort wReserved1;
    }

    [Flags]
    public enum WaveInOpenFlags : uint
    {
        CALLBACK_NULL = 0,
        CALLBACK_FUNCTION = 0x30000,
        CALLBACK_EVENT = 0x50000,
        CALLBACK_WINDOW = 0x10000,
        CALLBACK_THREAD = 0x20000,
        WAVE_FORMAT_QUERY = 1,
        WAVE_MAPPED = 4,
        WAVE_FORMAT_DIRECT = 8
    }

    [Flags]
    public enum WaveOutOpenFlags : uint
    {
        CALLBACK_NULL = 0,
        CALLBACK_FUNCTION = 0x30000,
        CALLBACK_EVENT = 0x50000,
        CALLBACK_WINDOW = 0x10000,
        CALLBACK_THREAD = 0x20000,
        WAVE_FORMAT_QUERY = 1,
        WAVE_MAPPED = 4,
        WAVE_FORMAT_DIRECT = 8
    }

    public class Winmm
    {
        public const int MM_WOM_OPEN = 0x3BB;
        public const int MM_WOM_CLOSE = 0x3BC;
        public const int MM_WOM_DONE = 0x3BD;

        public const int MM_WIM_OPEN = 0x3BE;
        public const int MM_WIM_CLOSE = 0x3BF;
        public const int MM_WIM_DATA = 0x3C0;

        public delegate void Callback(IntPtr hw, int uMsg, IntPtr dwUser, ref WAVEHDR dwParam1, IntPtr dwParam2);

        [DllImport("winmm.dll")]
        public static extern int waveOutGetNumDevs();
        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern MMRESULT waveOutGetDevCaps(IntPtr hwo, ref WAVEOUTCAPS pwoc, uint cbwoc);
        [DllImport("winmm.dll")]
        public static extern MMRESULT waveOutPrepareHeader(IntPtr hWaveOut, WAVEHDR lpWaveOutHdr, int uSize);
        [DllImport("winmm.dll")]
        public static extern MMRESULT waveOutUnprepareHeader(IntPtr hWaveOut, WAVEHDR lpWaveOutHdr, int uSize);
        [DllImport("winmm.dll")]
        public static extern MMRESULT waveOutWrite(IntPtr hWaveOut, WAVEHDR lpWaveOutHdr, int uSize);
        [DllImport("winmm.dll")]
        public static extern MMRESULT waveOutOpen(out IntPtr hWaveOut, int uDeviceID, ref WAVEFORMATEX lpFormat, Callback dwCallback, IntPtr dwInstance, WaveOutOpenFlags dwFlags);
        [DllImport("winmm.dll")]
        public static extern MMRESULT waveOutOpen(out IntPtr hWaveOut, int uDeviceID, ref WAVEFORMATEX lpFormat, IntPtr dwCallback, IntPtr dwInstance, WaveOutOpenFlags dwFlags);
        [DllImport("winmm.dll")]
        public static extern MMRESULT waveOutReset(IntPtr hWaveOut);
        [DllImport("winmm.dll")]
        public static extern MMRESULT waveOutClose(IntPtr hWaveOut);
        [DllImport("winmm.dll")]
        public static extern MMRESULT waveOutPause(IntPtr hWaveOut);
        [DllImport("winmm.dll")]
        public static extern MMRESULT waveOutRestart(IntPtr hWaveOut);
        [DllImport("winmm.dll")]
        public static extern MMRESULT waveOutGetPosition(IntPtr hWaveOut, out int lpInfo, int uSize);
        [DllImport("winmm.dll")]
        public static extern MMRESULT waveOutSetVolume(IntPtr hWaveOut, int dwVolume);
        [DllImport("winmm.dll")]
        public static extern MMRESULT waveOutGetVolume(IntPtr hWaveOut, out int dwVolume);
        [DllImport("winmm.dll")]
        public static extern MMRESULT waveOutMessage(IntPtr hwi, int uMsg, IntPtr dwParam1, IntPtr dwParam2);

        [DllImport("winmm.dll")]
        public static extern int waveInGetNumDevs();
        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern MMRESULT waveInGetDevCaps(IntPtr hwo, ref WAVEINCAPS pwic, uint cbwoc);
        [DllImport("winmm.dll")]
        public static extern MMRESULT waveInAddBuffer(IntPtr hwi, WAVEHDR pwh, int cbwh);
        [DllImport("winmm.dll")]
        public static extern MMRESULT waveInClose(IntPtr hwi);
        [DllImport("winmm.dll")]
        public static extern MMRESULT waveInOpen(out IntPtr phwi, int uDeviceID, ref WAVEFORMATEX lpFormat, Callback dwCallback, IntPtr dwInstance, WaveInOpenFlags dwFlags);
        [DllImport("winmm.dll")]
        public static extern MMRESULT waveInOpen(out IntPtr phwi, int uDeviceID, ref WAVEFORMATEX lpFormat, IntPtr dwCallback, IntPtr dwInstance, WaveInOpenFlags dwFlags);
        [DllImport("winmm.dll")]
        public static extern MMRESULT waveInPrepareHeader(IntPtr hWaveIn, WAVEHDR lpWaveInHdr, int uSize);
        [DllImport("winmm.dll")]
        public static extern MMRESULT waveInUnprepareHeader(IntPtr hWaveIn, WAVEHDR lpWaveInHdr, int uSize);
        [DllImport("winmm.dll")]
        public static extern MMRESULT waveInReset(IntPtr hwi);
        [DllImport("winmm.dll")]
        public static extern MMRESULT waveInStart(IntPtr hwi);
        [DllImport("winmm.dll")]
        public static extern MMRESULT waveInStop(IntPtr hwi);
        [DllImport("winmm.dll")]
        public static extern MMRESULT waveInMessage(IntPtr hwi, int uMsg, IntPtr dwParam1, IntPtr dwParam2);
    }
}
