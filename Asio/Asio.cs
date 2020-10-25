using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Util;

namespace Asio
{
    enum ASIOError : int
    {
        OK = 0,
        SUCCESS = 0x3f4847a0,
        NotPresent = -1000,
        HWMalfunction,
        InvalidParameter,
        InvalidMode,
        SPNotAdvancing,
        NoClock,
        NoMemory
    }

    enum ASIOSampleType : int
    {
        Int16MSB = 0,
        Int24MSB = 1,
        Int32MSB = 2,
        Float32MSB = 3,
        Float64MSB = 4,

        Int32MSB16 = 8,
        Int32MSB18 = 9,
        Int32MSB20 = 10,
        Int32MSB24 = 11,

        Int16LSB = 16,
        Int24LSB = 17,
        Int32LSB = 18,
        Float32LSB = 19,
        Float64LSB = 20,

        Int32LSB16 = 24,
        Int32LSB18 = 25,
        Int32LSB20 = 26,
        Int32LSB24 = 27,

        DSDInt8LSB1 = 32,
        DSDInt8MSB1 = 33,
        DSDInt8NER8 = 40,

        LastEntry
    };

    enum ASIOBool : int
    {
        False = 0,
        True = 1,
    }

    class AsioException : Exception
    {
        private ASIOError error;
        public ASIOError Error { get { return error; } }

        public AsioException(ASIOError Error) : base(String.Format("Error in ASIO object: {0}", Error)) { error = Error; }
        public AsioException(string Message, ASIOError Error) : base(Message) { error = Error; }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Ansi)]
    struct ASIOClockSource
    {
        public int index;
        public int associatedChannel;
        public int associatedGroup;
        public ASIOBool isCurrentSource;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string name;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Ansi)]
    struct ASIOChannelInfo
    {
        public int channel;
        public ASIOBool isInput;
        public ASIOBool isActive;
        public int channelGroup;
        public ASIOSampleType type;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string name;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    struct ASIOBufferInfo
    {
        public ASIOBool isInput;
        public int channelNum;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public IntPtr[] buffers;
    };


    [Flags]
    enum ASIOTimeCodeFlags : uint
    {
        Valid = 1,
        Running = 1 << 1,
        Reverse = 1 << 2,
        Onspeed = 1 << 3,
        Still = 1 << 4,
        SpeedValid = 1 << 8
    };

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    struct ASIOTimeCode
    {
        public double speed;
        public long timeCodeSamples;
        public ASIOTimeCodeFlags flags;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] future;
    };

    [Flags]
    enum AsioTimeInfoFlags : uint
    {
        SystemTimeValid = 1,
        SamplePositionValid = 1 << 1,
        SampleRateValid = 1 << 2,
        SpeedValid = 1 << 3,
        SampleRateChanged = 1 << 4,
        ClockSourceChanged = 1 << 5
    };

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    struct AsioTimeInfo
    {
        public double speed;
        public long systemTime;
        public long samplePosition;
        public double sampleRate;
        public AsioTimeInfoFlags flags;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public byte[] reserved;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    struct ASIOTime
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public int[] reserved;
        public AsioTimeInfo timeInfo;
        public ASIOTimeCode timeCode;
    };

    enum ASIOMessageSelector : int
    {
        SelectorSupported = 1,
        EngineVersion,
        ResetRequest,
        BufferSizeChange,
        ResyncRequest,
        LatenciesChanged,
        SupportsTimeInfo,
        SupportsTimeCode,
        MMCCommand,
        SupportsInputMonitor,
        SupportsInputGain,
        SupportsInputMeter,
        SupportsOutputGain,
        SupportsOutputMeter,
        Overload,
        NumMessageSelectors
    };

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    struct ASIOCallbacks
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void BufferSwitch(int bufferIndex, ASIOBool directProcess);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void SampleRateDidChange(double sRate);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate int AsioMessage(ASIOMessageSelector selector, int value, IntPtr message, IntPtr opt);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate IntPtr BufferSwitchTimeInfo(IntPtr _params, int doubleBufferIndex, ASIOBool directProcess);

        public BufferSwitch bufferSwitch;
        public SampleRateDidChange sampleRateDidChange;
        public AsioMessage asioMessage;
        public BufferSwitchTimeInfo bufferSwitchTimeInfo;
    };

    class AsioObject : IDisposable
    {
        private VTable vtbl;
        private Guid clsid;
        public Guid ClassId { get { return clsid; } }
        private IntPtr _this = IntPtr.Zero;

        public AsioObject(Guid ClassId)
        {
            clsid = ClassId;

            const uint CLSCTX_INPROC_SERVER = 1;

            Log.Global.WriteLine(MessageType.Info, "AsioObject.AsioObject(ClassId='{0}')", ClassId);

            int hr = CoCreateInstance(ref ClassId, null, CLSCTX_INPROC_SERVER, ref ClassId, out _this);
            if (hr != 0)
                throw new COMException("CoCreateInstance failed", hr);

            vtbl = new VTable(Marshal.ReadIntPtr(_this));
        }

        ~AsioObject() { Dispose(false); }
        public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }
        private void Dispose(bool Disposing)
        {
            if (_this != IntPtr.Zero)
            {
                vtbl.Release(_this);
                _this = IntPtr.Zero;
            }
            vtbl = null;
        }

        public void Init(IntPtr SysHandle)
        {
            Log.Global.WriteLine(MessageType.Info, "AsioObject.Init");
            if (vtbl.init(_this, SysHandle) == ASIOBool.False)
                throw new AsioException("init failed", ASIOError.NotPresent);
        }

        public string DriverName
        {
            get
            {
                StringBuilder name = new StringBuilder(256);
                vtbl.getDriverName(_this, name);
                return name.ToString();
            }
        }

        public string ErrorMessage
        {
            get
            {
                StringBuilder message = new StringBuilder(256);
                vtbl.getErrorMessage(_this, message);
                return message.ToString();
            }
        }

        public int DriverVersion { get { return vtbl.getDriverVersion(_this); } }

        public void Start()
        {
            Log.Global.WriteLine(MessageType.Info, "AsioObject.Start");
            Try(vtbl.start(_this));
        }
        public void Stop()
        {
            Log.Global.WriteLine(MessageType.Info, "AsioObject.Stop");
            Try(vtbl.stop(_this));
        }

        private ASIOChannelInfo[] GetChannels(int Count, bool Input)
        {
            ASIOChannelInfo[] channels = new ASIOChannelInfo[Count];
            for (int i = 0; i < channels.Length; ++i)
            {
                channels[i] = new ASIOChannelInfo() { channel = i, isInput = Input ? ASIOBool.True : ASIOBool.False };
                Try(vtbl.getChannelInfo(_this, ref channels[i]));
            }
            return channels;
        }

        public ASIOChannelInfo[] InputChannels
        {
            get
            {
                int input, output;
                Try(vtbl.getChannels(_this, out input, out output));
                return GetChannels(input, true);
            }
        }

        public ASIOChannelInfo[] OutputChannels
        {
            get
            {
                int input, output;
                Try(vtbl.getChannels(_this, out input, out output));
                return GetChannels(output, false);
            }
        }

        public int InputLatency
        {
            get
            {
                int input, output;
                Try(vtbl.getLatencies(_this, out input, out output));
                return input;
            }
        }

        public int OutputLatency
        {
            get
            {
                int input, output;
                Try(vtbl.getLatencies(_this, out input, out output));
                return output;
            }
        }

        public bool IsSampleRateSupported(double SampleRate) { return vtbl.canSampleRate(_this, SampleRate) == ASIOError.OK; }
        public double SampleRate
        {
            get { double rate; Try(vtbl.getSampleRate(_this, out rate)); return rate; }
            set { Try(vtbl.setSampleRate(_this, value)); }
        }

        public void ShowControlPanel() { Try(vtbl.controlPanel(_this)); }

        public class BufferSizeInfo
        {
            private int min, max, preferred, granularity;

            public BufferSizeInfo(int Min, int Max, int Preferred, int Granularity)
            {
                min = Min;
                max = Max;
                preferred = Preferred;
                granularity = Granularity;
            }

            public int Min { get { return min; } }
            public int Max { get { return max; } }
            public int Preferred { get { return preferred; } }
            public int Granularity { get { return granularity; } }
        };

        public BufferSizeInfo BufferSize
        {
            get
            {
                int min, max, preferred, granularity;
                Try(vtbl.getBufferSize(_this, out min, out max, out preferred, out granularity));
                return new BufferSizeInfo(min, max, preferred, granularity);
            }
        }

        private IntPtr callbacks = IntPtr.Zero;
        private GCHandle[] pins = null;

        public void CreateBuffers(ASIOBufferInfo[] Infos, int Size, ASIOCallbacks Callbacks)
        {
            pins = new GCHandle[]
            {
                GCHandle.Alloc(Callbacks.bufferSwitch),
                GCHandle.Alloc(Callbacks.sampleRateDidChange),
                GCHandle.Alloc(Callbacks.asioMessage),
                GCHandle.Alloc(Callbacks.bufferSwitchTimeInfo)
            };
            callbacks = Marshal.AllocHGlobal(Marshal.SizeOf(Callbacks));

            Marshal.StructureToPtr(Callbacks, callbacks, false);
            Log.Global.WriteLine(MessageType.Info, "AsioObject.CreateBuffers(Size={0})", Size);
            Try(vtbl.createBuffers(_this, Infos, Infos.Length, Size, callbacks));
        }
        public void DisposeBuffers()
        {
            Log.Global.WriteLine(MessageType.Info, "AsioObject.DisposeBuffers");
            Try(vtbl.disposeBuffers(_this));

            Marshal.FreeHGlobal(callbacks);
            callbacks = IntPtr.Zero;

            foreach (GCHandle i in pins)
                i.Free();
            pins = null;
        }

        public void OutputReady() { Try(vtbl.outputReady(_this)); }

        private static void Try(ASIOError Result)
        {
            if (Result != ASIOError.OK && Result != ASIOError.SUCCESS)
                throw new AsioException(Result);
        }

        [StructLayout(LayoutKind.Sequential)]
        class VTable
        {
            // IUnknown
            [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate int _AddRef(IntPtr _this);
            [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate int _QueryInterface(IntPtr _this, Guid riid, ref IntPtr ppvObject);
            [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate int _Release(IntPtr _this);

            // IASIO
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)] public delegate ASIOBool _init(IntPtr _this, IntPtr sysHandle);
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)] public delegate void _getDriverName(IntPtr _this, StringBuilder name);
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)] public delegate int _getDriverVersion(IntPtr _this);
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)] public delegate void _getErrorMessage(IntPtr _this, StringBuilder name);
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)] public delegate ASIOError _start(IntPtr _this);
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)] public delegate ASIOError _stop(IntPtr _this);
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)] public delegate ASIOError _getChannels(IntPtr _this, out int numInputChannels, out int numOutputChannels);
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)] public delegate ASIOError _getLatencies(IntPtr _this, out int inputLatency, out int outputLatency);
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)] public delegate ASIOError _getBufferSize(IntPtr _this, out int minSize, out int maxSize, out int preferredSize, out int granularity);
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)] public delegate ASIOError _canSampleRate(IntPtr _this, double sampleRate);
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)] public delegate ASIOError _getSampleRate(IntPtr _this, out double sampleRate);
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)] public delegate ASIOError _setSampleRate(IntPtr _this, double sampleRate);
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)] public delegate ASIOError _getClockSources(IntPtr _this, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] ASIOClockSource[] clocks, out int numSources);
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)] public delegate ASIOError _setClockSource(IntPtr _this, int reference);
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)] public delegate ASIOError _getSamplePosition(IntPtr _this, out long sPos, out long tStamp);
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)] public delegate ASIOError _getChannelInfo(IntPtr _this, ref ASIOChannelInfo info);
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)] public delegate ASIOError _createBuffers(IntPtr _this, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] ASIOBufferInfo[] bufferInfos, int numChannels, int bufferSize, IntPtr callbacks);
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)] public delegate ASIOError _disposeBuffers(IntPtr _this);
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)] public delegate ASIOError _controlPanel(IntPtr _this);
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)] public delegate ASIOError _future(IntPtr _this, int selector, IntPtr opt);
            [UnmanagedFunctionPointer(CallingConvention.ThisCall)] public delegate ASIOError _outputReady(IntPtr _this);

            // IUnknown
            public _AddRef AddRef = null;
            public _QueryInterface QueryInterface = null;
            public _Release Release = null;

            // IASIO
            public _init init = null;
            public _getDriverName getDriverName = null;
            public _getDriverVersion getDriverVersion = null;
            public _getErrorMessage getErrorMessage = null;
            public _start start = null;
            public _stop stop = null;
            public _getChannels getChannels = null;
            public _getLatencies getLatencies = null;
            public _getBufferSize getBufferSize = null;
            public _canSampleRate canSampleRate = null;
            public _getSampleRate getSampleRate = null;
            public _setSampleRate setSampleRate = null;
            public _getClockSources getClockSources = null;
            public _setClockSource setClockSource = null;
            public _getSamplePosition getSamplePosition = null;
            public _getChannelInfo getChannelInfo = null;
            public _createBuffers createBuffers = null;
            public _disposeBuffers disposeBuffers = null;
            public _controlPanel controlPanel = null;
            public _future future = null;
            public _outputReady outputReady = null;

            public VTable(IntPtr pvtbl)
            {
                FieldInfo[] fields = GetType().GetFields();
                for (int i = 0; i < fields.Length; ++i)
                {
                    IntPtr pi = Marshal.ReadIntPtr(pvtbl, i * IntPtr.Size);
                    fields[i].SetValue(this, Marshal.GetDelegateForFunctionPointer(pi, fields[i].FieldType));
                }
            }
        }

        [DllImport("ole32.Dll")]
        private static extern int CoCreateInstance(ref Guid clsid,
           [MarshalAs(UnmanagedType.IUnknown)] object inner,
           uint context,
           ref Guid uuid,
           out IntPtr rReturnedComObject);
    }
}
