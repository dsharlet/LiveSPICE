using System;
using System.Runtime.InteropServices;

namespace AudioIo
{
    public enum MmResult : uint
    {
        NoError         = 0,
        ERROR           = 1,
        BADDEVICEID     = 2,
        NOTENABLED      = 3,
        ALLOCATED       = 4,
        INVALHANDLE     = 5,
        NODRIVER        = 6,
        NOMEM           = 7,
        NOTSUPPORTED    = 8,
        BADERRNUM       = 9,
        INVALFLAG       = 10,
        INVALPARAM      = 11,
        HANDLEBUSY      = 12,
        INVALIDALIAS    = 13,
        BADDB           = 14,
        KEYNOTFOUND     = 15,
        READERROR       = 16,
        WRITEERROR      = 17,
        DELETEERROR     = 18,
        VALNOTFOUND     = 19,
        NODRIVERCB      = 20,
        BADFORMAT       = 32,
        STILLPLAYING    = 33,
        UNPREPARED      = 34
    }

    public class MmException : Exception
    {
        private MmResult result;

        public MmException(MmResult Result) : base(Result.ToString())
        {
            result = Result;
        }

        public MmResult Result { get { return result; } }

        public static void CheckThrow(MmResult result)
        {
            if (result != MmResult.NoError)
                throw new MmException(result);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WaveFormatEx
    {
        public short FormatTag;
        public short Channels;
        public int SamplesPerSec;
        public int AvgBytesPerSec;
        public short BlockAlign;
        public short BitsPerSample;
        public short Size;

        public WaveFormatEx(int Rate, int Bits, int Channels)
        {
            this.FormatTag = (short)1;
            this.Channels = (short)Channels;
            this.SamplesPerSec = Rate;
            this.BitsPerSample = (short)Bits;
            this.Size = 0;

            this.BlockAlign = (short)(Channels * (Bits / 8));
            this.AvgBytesPerSec = (Rate * Channels * Bits) / 8;
        }
    }
    
    [Flags]
    public enum Whdr : int
    {
        Done        = 0x00000001,
        Prepared    = 0x00000002,
        BeginLoop   = 0x00000004,
        EndLoop     = 0x00000008,
        InQueue     = 0x00000010,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WaveHdr
    {
        public IntPtr Data;
        public int BufferLength;
        public int BytesRecorded;
        public IntPtr User;
        public Whdr Flags;
        public int Loops;
        public IntPtr Next;
        public int Reserved;
    }
    
    public class WaveApi
    {
        public const int MM_WOM_OPEN = 0x3BB;
        public const int MM_WOM_CLOSE = 0x3BC;
        public const int MM_WOM_DONE = 0x3BD;

        public const int MM_WIM_OPEN = 0x3BE;
        public const int MM_WIM_CLOSE = 0x3BF;
        public const int MM_WIM_DATA = 0x3C0;

        public const int CALLBACK_FUNCTION = 0x00030000;
        
        public delegate void Callback(IntPtr hw, int uMsg, IntPtr dwUser, ref WaveHdr dwParam1, IntPtr dwParam2);

        [DllImport("winmm.dll")]
        public static extern MmResult waveOutGetNumDevs();
        [DllImport("winmm.dll")]
        public static extern MmResult waveOutPrepareHeader(IntPtr hWaveOut, ref WaveHdr lpWaveOutHdr, int uSize);
        [DllImport("winmm.dll")]
        public static extern MmResult waveOutUnprepareHeader(IntPtr hWaveOut, ref WaveHdr lpWaveOutHdr, int uSize);
        [DllImport("winmm.dll")]
        public static extern MmResult waveOutWrite(IntPtr hWaveOut, ref WaveHdr lpWaveOutHdr, int uSize);
        [DllImport("winmm.dll")]
        public static extern MmResult waveOutOpen(out IntPtr hWaveOut, int uDeviceID, ref WaveFormatEx lpFormat, Callback dwCallback, IntPtr dwInstance, int dwFlags);
        [DllImport("winmm.dll")]
        public static extern MmResult waveOutReset(IntPtr hWaveOut);
        [DllImport("winmm.dll")]
        public static extern MmResult waveOutClose(IntPtr hWaveOut);
        [DllImport("winmm.dll")]
        public static extern MmResult waveOutPause(IntPtr hWaveOut);
        [DllImport("winmm.dll")]
        public static extern MmResult waveOutRestart(IntPtr hWaveOut);
        [DllImport("winmm.dll")]
        public static extern MmResult waveOutGetPosition(IntPtr hWaveOut, out int lpInfo, int uSize);
        [DllImport("winmm.dll")]
        public static extern MmResult waveOutSetVolume(IntPtr hWaveOut, int dwVolume);
        [DllImport("winmm.dll")]
        public static extern MmResult waveOutGetVolume(IntPtr hWaveOut, out int dwVolume);
        [DllImport("winmm.dll")]
        public static extern MmResult waveOutMessage(IntPtr hwi, int uMsg, IntPtr dwParam1, IntPtr dwParam2);

        [DllImport("winmm.dll")]
        public static extern MmResult waveInGetNumDevs();
        [DllImport("winmm.dll")]
        public static extern MmResult waveInAddBuffer(IntPtr hwi, ref WaveHdr pwh, int cbwh);
        [DllImport("winmm.dll")]
        public static extern MmResult waveInClose(IntPtr hwi);
        [DllImport("winmm.dll")]
        public static extern MmResult waveInOpen(out IntPtr phwi, int uDeviceID, ref WaveFormatEx lpFormat, Callback dwCallback, IntPtr dwInstance, int dwFlags);
        [DllImport("winmm.dll")]
        public static extern MmResult waveInPrepareHeader(IntPtr hWaveIn, ref WaveHdr lpWaveInHdr, int uSize);
        [DllImport("winmm.dll")]
        public static extern MmResult waveInUnprepareHeader(IntPtr hWaveIn, ref WaveHdr lpWaveInHdr, int uSize);
        [DllImport("winmm.dll")]
        public static extern MmResult waveInReset(IntPtr hwi);
        [DllImport("winmm.dll")]
        public static extern MmResult waveInStart(IntPtr hwi);
        [DllImport("winmm.dll")]
        public static extern MmResult waveInStop(IntPtr hwi);
        [DllImport("winmm.dll")]
        public static extern MmResult waveInMessage(IntPtr hwi, int uMsg, IntPtr dwParam1, IntPtr dwParam2);
    }
}
