using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Xml.Serialization;
using AudioPlugSharp;
using AudioPlugSharpWPF;

namespace LiveSPICEVst
{
    /// <summary>
    /// Managed VST class to be loaded by SharpSoundDevice
    /// </summary>
    public class LiveSPICEPlugin : AudioPluginBase
    {
        public SimulationProcessor SimulationProcessor { get; private set; }
        public EditorView EditorView { get; set; }
        public string SchematicPath { get { return SimulationProcessor.SchematicPath; } }

        System.Windows.Window window;

        AudioIOPort monoInput;
        AudioIOPort monoOutput;

        bool haveSimulationError = false;

        public LiveSPICEPlugin()
        {
            Company = "";
            Website = "livespice.org";
            Contact = "";
            PluginName = "LiveSPICEVst";
            PluginCategory = "Fx";
            PluginVersion = "1.1.0";

            // Unique 64bit ID for the plugin
            PluginID = 0xDC8558DC41A44872;

            HasUserInterface = true;
            EditorWidth = 350;
            EditorHeight = 200;

            GCSettings.LatencyMode = GCLatencyMode.LowLatency;

            SimulationProcessor = new SimulationProcessor();

            //AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            Logger.Log("****assembly resolve " + args.Name);

            AppDomain domain = (AppDomain)sender;
            foreach (Assembly asm in domain.GetAssemblies())
            {
                if (asm.FullName == args.Name)
                {

                    Logger.Log("** got it");
                    return asm;
                }
            }

            return null;
        }

        public override void Initialize()
        {
            base.Initialize();

            InputPorts = new AudioIOPort[] { monoInput = new AudioIOPort("Mono Input", EAudioChannelConfiguration.Mono) };
            OutputPorts = new AudioIOPort[] { monoOutput = new AudioIOPort("Mono Output", EAudioChannelConfiguration.Mono) };
        }

        public override void InitializeProcessing()
        {
            base.InitializeProcessing();

            Logger.Log("Initialize Processing");
            Logger.Log("Sample rate: " + Host.SampleRate + " Max Buffer Size: " + Host.MaxAudioBufferSize);

            SimulationProcessor.SampleRate = Host.SampleRate;
        }

        public override bool ShowEditor(IntPtr parentWindow)
        {
            Logger.Log("Open editor");

            if (EditorView == null)
            {
                EditorView = new EditorView(this)
                {
                    Width = EditorWidth,
                    Height = EditorHeight
                };
            }

            EditorWindow window = new EditorWindow(this, EditorView)
            {
                Width = EditorWidth,
                Height = EditorHeight
            };

            window.Show(parentWindow);

            return true;
        }

        /// <summary>
        /// Save our current plugin state
        /// </summary>
        /// <returns>A byte array of data</returns>
        public override byte[] SaveState()
        {
            byte[] stateData = null;

            VstProgramParameters programParameters = new VstProgramParameters
            {
                SchematicPath = SimulationProcessor.SchematicPath,
                OverSample = SimulationProcessor.Oversample,
                Iterations = SimulationProcessor.Iterations
            };

            foreach (ComponentWrapper wrapper in SimulationProcessor.InteractiveComponents)
            {
                if (wrapper is PotWrapper)
                {
                    programParameters.ControlParameters.Add(new VSTProgramControlParameter { Name = wrapper.Name, Value = (wrapper as PotWrapper).PotValue });
                }
                else if (wrapper is ButtonWrapper)
                {
                    programParameters.ControlParameters.Add(new VSTProgramControlParameter { Name = wrapper.Name, Value = (wrapper as ButtonWrapper).Engaged ? 1 : 0 });
                }
            }

            XmlSerializer serializer = new XmlSerializer(typeof(VstProgramParameters));

            using (MemoryStream memoryStream = new MemoryStream())
            {
                serializer.Serialize(memoryStream, programParameters);

                stateData = memoryStream.ToArray();
            }

            return stateData;
        }

        /// <summary>
        /// Restore plugin state
        /// </summary>
        /// <param name="stateData">Byte array of data to restore</param>
        public override void RestoreState(byte[] stateData)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(VstProgramParameters));

            try
            {
                using (MemoryStream memoryStream = new MemoryStream(stateData))
                {
                    VstProgramParameters programParameters = serializer.Deserialize(memoryStream) as VstProgramParameters;

                    if (string.IsNullOrEmpty(programParameters.SchematicPath))
                    {
                        haveSimulationError = false;

                        SimulationProcessor.ClearSchematic();
                    }
                    else
                    {
                        LoadSchematic(programParameters.SchematicPath);
                    }

                    SimulationProcessor.Oversample = programParameters.OverSample;
                    SimulationProcessor.Iterations = programParameters.Iterations;

                    foreach (VSTProgramControlParameter controlParameter in programParameters.ControlParameters)
                    {
                        ComponentWrapper wrapper = SimulationProcessor.InteractiveComponents.Where(i => i.Name == controlParameter.Name).SingleOrDefault();

                        if (wrapper != null)
                        {
                            if (wrapper.Name == controlParameter.Name)
                            {
                                if (wrapper is PotWrapper)
                                {
                                    (wrapper as PotWrapper).PotValue = controlParameter.Value;
                                }
                                else if (wrapper is ButtonWrapper)
                                {
                                    (wrapper as ButtonWrapper).Engaged = (controlParameter.Value == 1);
                                }
                            }
                        }
                    }

                    if (EditorView != null)
                        EditorView.UpdateSchematic();
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Load state failed: " + ex.Message);
            }
        }

        public void LoadSchematic(string path)
        {
            haveSimulationError = false;

            try
            {
                SimulationProcessor.LoadSchematic(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format("Error loading schematic from: {0}\n\n{1}", path, ex.ToString()), "Schematic Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public override void Process()
        {
            if (haveSimulationError)
            {
                monoInput.PassThroughTo(monoOutput);
            }
            else
            {
                double[][] inBuffers = monoInput.GetAudioBuffers();
                double[][] outBuffers = monoOutput.GetAudioBuffers();

                // Read input samples from unmanaged memory
                monoInput.ReadData();

                try
                {
                    SimulationProcessor.RunSimulation(inBuffers, outBuffers, inBuffers[0].Length);
                }
                catch (Exception ex)
                {
                    haveSimulationError = true;

                    new Thread(() =>
                    {
                        MessageBox.Show("Error running circuit simulation.\n\n" + ex.Message, "Simulation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }).Start();
                }

                // Write outout samples to unmanaged memory
                monoOutput.WriteData();
            }
        }
    }
}
