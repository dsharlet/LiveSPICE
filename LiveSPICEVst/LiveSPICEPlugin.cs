using SharpSoundDevice;
using System;
using System.Reflection;
using System.Runtime;
using System.Runtime.InteropServices;

namespace LiveSPICEVst
{
    /// <summary>
    /// Managed VST class to be loaded by SharpSoundDevice
    /// </summary>
    public unsafe class LiveSPICEPlugin : IAudioDevice
    {
        private DeviceInfo DevInfo;

        public int CurrentProgram { get; private set; }
        public DeviceInfo DeviceInfo { get { return DevInfo; } }
        public IHostInfo HostInfo { get; set; }
        public Parameter[] ParameterInfo { get; private set; }
        public SharpSoundDevice.Port[] PortInfo { get; private set; }
        public int DeviceId { get; set; }

        public SimulationProcessor SimulationProcessor { get; private set; }

        EditorView view;
        System.Windows.Window window;

        double[] inputBuffer = null;
        double[] outputBuffer = null;
        int currentBufferSize = 0;

        public LiveSPICEPlugin()
        {
            DevInfo = new DeviceInfo();
            DevInfo.Developer = "";
            DevInfo.DeviceID = "LiveSPICEVst";
            DevInfo.EditorHeight = 200;
            DevInfo.EditorWidth = 350;
            DevInfo.HasEditor = true;
            DevInfo.Name = "LiveSPICEVst";
            DevInfo.ProgramCount = 1;
            DevInfo.Type = DeviceType.Effect;
            DevInfo.UnsafeProcessing = true;
            DevInfo.Version = 1000;
            DevInfo.VstId = DeviceUtilities.GenerateIntegerId(DevInfo.DeviceID);

            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);

            // Not sure if this helps, but...
            GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;

            ParameterInfo = new Parameter[0]
            {
            };

            PortInfo = new SharpSoundDevice.Port[2]
            {
                new SharpSoundDevice.Port() { Direction = PortDirection.Input, Name = "Mono Input", NumberOfChannels = 1 },
                new SharpSoundDevice.Port() { Direction = PortDirection.Output, Name = "Mono Output", NumberOfChannels = 1 }
            };

            SimulationProcessor = new SimulationProcessor();
        }

        public void Debug(String debugStr)
        {
            Logging.Log(debugStr);
        }

        public void InitializeDevice()
        {
        }

        public void DisposeDevice()
        {

        }

        public void Start()
        {
            Logging.Log("Starting");
            Logging.Log("Sample rate: " + HostInfo.SampleRate + " Buffer Size: " + HostInfo.BlockSize);

            SimulationProcessor.SampleRate = HostInfo.SampleRate;
        }
        public void Stop() { }

        public void OpenEditor(IntPtr parentWindow)
        {
            Logging.Log("Open editor");

            if (view == null)
            {
                view = new EditorView(this) { Width = DevInfo.EditorWidth, Height = DevInfo.EditorHeight };
            }

            DevInfo.EditorWidth = (int)view.Width;
            DevInfo.EditorHeight = (int)view.Height;
            HostInfo.SendEvent(DeviceId, new Event { Data = null, EventIndex = 0, Type = EventType.WindowSize });
            window = new System.Windows.Window() { Content = view };
            window.Width = view.Width;
            window.Height = view.Height;
            DeviceUtilities.DockWpfWindow(window, parentWindow);
            window.Show();
        }

        public void CloseEditor()
        {
        }

        public void HostChanged() { }

        // We handle unsafe buffer data, so this is unused
        public void ProcessSample(double[][] input, double[][] output, uint bufferSize)
        {
            throw new NotImplementedException();
        }

        public bool SendEvent(Event ev)
        {
            return true;
        }

        public void SetParam(int index, double value)
        {
        }

        public Program GetProgramData(int index)
        {
            Logging.Log("Save program: " + index);

            var program = new Program();
            program.Name = null; // "Program 1";

            //program.Data = GetCurrentProgramData();

            return program;
        }


        public void SetProgramData(Program program, int index)
        {
            Logging.Log("Load program: " + index);

        }

        /// <summary>
        /// Process a buffer of samples from the VST host
        /// </summary>
        /// <param name="input">IntPtr to a double** of input samples</param>
        /// <param name="output">IntPtr to a double** of output samples</param>
        /// <param name="inChannelCount">The number of input channels</param>
        /// <param name="outChannelCount">The number of output channels</param>
        /// <param name="bufferSize">The buffer size in samples</param>
        public void ProcessSample(IntPtr input, IntPtr output, uint inChannelCount, uint outChannelCount, uint bufferSize)
        {
            if (currentBufferSize < bufferSize)
            {
                currentBufferSize = (int)bufferSize;

                Array.Resize<double>(ref inputBuffer, currentBufferSize);
                Array.Resize<double>(ref outputBuffer, currentBufferSize);
            }

            // We are doing mono processing, so just grab the left input and output channel
            IntPtr leftIn = (IntPtr)((double**)input)[0];
            IntPtr leftOut = (IntPtr)((double**)output)[0];

            Marshal.Copy(leftIn, inputBuffer, 0, currentBufferSize);

            SimulationProcessor.RunSimulation(inputBuffer, outputBuffer);

            Marshal.Copy(outputBuffer, 0, leftOut, currentBufferSize);
        }
    }
}
