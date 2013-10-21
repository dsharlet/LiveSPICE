using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Asio
{
    public enum AsioError : int
    {
	    Ok = 0,
	    Success = 0x3f4847a0,
	    NotPresent = -1000,
        // These probably aren't the right value...
	    HWMalfunction,
	    InvalidParameter,
	    InvalidMode,
	    SPNotAdvancing,
	    NoClock,
	    NoMemory
    };

    public enum SampleType : int 
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
    };

    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Auto)]
    public struct ClockSource
    {
        public int index;
        public int associatedChannel;
        public int associatedGroup;
        public int isCurrentSource;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string name;
    };
    
    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Auto)]
    public struct ChannelInfo
    {
        public int channel;
        public int isInput;
        public int isActive;
        public int channelGroup;
        public SampleType type;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string name;
    };
    
    
    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Auto)]
    public struct BufferInfo
    {
        public int isInput;
        public int channelNum;
        [MarshalAs(UnmanagedType.LPArray, SizeConst=2)]
	    public IntPtr[] buffers;
    };
    

    [Flags]
    public enum TimeInfoFlags : int
    {
	    kSystemTimeValid        = 1, 
	    kSamplePositionValid    = 1 << 1,
	    kSampleRateValid        = 1 << 2,
	    kSpeedValid             = 1 << 3,
	    kSampleRateChanged      = 1 << 4,
	    kClockSourceChanged     = 1 << 5
    };


    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Auto)]
    public struct TimeInfo
    {
	    public double speed;
	    public long systemTime;
	    public long samplePosition;
	    public double sampleRate;
	    public TimeInfoFlags flags;
        [MarshalAs(UnmanagedType.LPArray, SizeConst=12)]
        public char[] reserved;
    };
    
    [Flags]
    public enum TimeCodeFlags : int
    {
	    Valid                = 1,
	    Running              = 1 << 1,
	    Reverse              = 1 << 2,
	    Onspeed              = 1 << 3,
	    Still                = 1 << 4,

	    SpeedValid           = 1 << 8
    };
    
    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Auto)]
    public struct TimeCode
    {       
	    public double speed;
	    public long timeCodeSamples;
	    public TimeCodeFlags flags;
        [MarshalAs(UnmanagedType.LPArray, SizeConst=64)]
	    public char[] future;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Auto)]
    public struct Time
    {
        [MarshalAs(UnmanagedType.LPArray, SizeConst=4)]
        public int[] reserved;
	    public TimeInfo timeInfo;
	    public TimeCode timeCode;
    };
    
    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Auto)]
    public struct Callbacks
    {
	    public delegate void BufferSwitch(int doubleBufferIndex, int directProcess);
	    public delegate void SampleRateDidChange(double sRate);
	    //public delegate long AsioMessage(int selector, int value, void* message, double* opt);
	    //public delegate Time BufferSwitchTimeInfo(Time _params, int doubleBufferIndex, int directProcess);
    };

    public class AsioWrapper
    {
        private object instance;

        public AsioWrapper(object Instance) 
        { 
            instance = Instance; 
        }

        public bool init(IntPtr sysHandle) { return (bool)Invoke("init", sysHandle); }
        public int getDriverVersion() { return (int)Invoke("getDriverVersion"); }
        public void start() { InvokeCheck("start"); }
        public void stop() { InvokeCheck("stop"); }
        public void setClockSource(int reference) { InvokeCheck("setClockSource", reference); }
        public void disposeBuffers() { InvokeCheck("disposeBuffers"); }
        public void controlPanel() { InvokeCheck("controlPanel"); }
        public void outputReady() { InvokeCheck("outputReady"); }
        public bool canSampleRate(double sampleRate) { return (AsioError)Invoke("canSampleRate", sampleRate) == AsioError.Ok; }
        public void setSampleRate(double sampleRate) { InvokeCheck("setSampleRate", sampleRate); }

        public string getDriverName()
        {
            string name = "";
            Invoke("getDriverName", name);
            return name;
        }
        public string getErrorMessage()
        {
            string name = "";
            Invoke("getErrorMessage", name);
            return name;
        }
        public double getSampleRate()
        {
            double sampleRate = 0.0;
            InvokeCheck("getSampleRate", sampleRate);
            return sampleRate;
        }

        public void getChannels(out int numInputChannels, out int numOutputChannels) 
        { 
            numInputChannels = 0;
            numOutputChannels = 0;
            InvokeCheck("getChannels", new object[] { numInputChannels, numOutputChannels });
        }
        //public void getLatencies(int *inputLatency, int *outputLatency) { }
        //public void getBufferSize(int *minSize, int *maxSize, int *preferredSize, int *granularity) { }
        //public void getClockSources(ClockSource *clocks, int *numSources) { }

        //public void getSamplePosition(long *sPos, long *tStamp) { }
        void getChannelInfo(ChannelInfo info) 
        {
            InvokeCheck("getChannelInfo", info);
        }
        void createBuffers(BufferInfo bufferInfos, int numChannels, int bufferSize, Callbacks callbacks) 
        {
            InvokeCheck("createBuffers", bufferInfos, numChannels, bufferSize, callbacks);
        }

        void future(int selector, IntPtr opt) { InvokeCheck("future", selector, opt); }

        private void InvokeCheck(string name, params object[] args)
        {
            switch ((AsioError)Invoke(name, args))
            {
                case AsioError.Ok: return;
                case AsioError.Success: return;
                default: throw new System.Exception(name);
            }
        }

        private object Invoke(string name, params object[] args)
        {
            MethodInfo method = instance.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return method.Invoke(instance, args);
        }
    }
}
