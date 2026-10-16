using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using AudioPlugSharp;
using LiveSPICE.Avalonia;
using LiveSPICE.PluginCore;

namespace LiveSPICE.PluginLinux;

public class LiveSPICELinuxPlugin : AudioPluginBase
{
    private AudioIOPortManaged? monoInput;
    private AudioIOPortManaged? monoOutput;
    private bool haveSimulationError;

    public LiveSPICELinuxPlugin()
    {
        Company = string.Empty;
        Website = "livespice.org";
        Contact = string.Empty;
        PluginName = "LiveSPICE Linux";
        PluginCategory = "Fx";
        PluginVersion = "1.1.0";
        PluginID = 0xDC8558DC41A44872;
        HasUserInterface = true;
        EditorWidth = 700;
        EditorHeight = 420;
        SimulationProcessor = new SimulationProcessor();
        LoadDefaultSchematic();
    }

    public SimulationProcessor SimulationProcessor { get; }

    public string SchematicPath => SimulationProcessor.SchematicPath;

    public PluginEditorWindow CreateEditorWindow()
    {
        return new PluginEditorWindow(SimulationProcessor);
    }

    public override void Initialize()
    {
        base.Initialize();
        InputPorts = new AudioIOPort[] { monoInput = new AudioIOPortManaged("Mono Input", EAudioChannelConfiguration.Mono) };
        OutputPorts = new AudioIOPort[] { monoOutput = new AudioIOPortManaged("Mono Output", EAudioChannelConfiguration.Mono) };
    }

    public override void InitializeProcessing()
    {
        base.InitializeProcessing();
        SimulationProcessor.SampleRate = Host.SampleRate;
    }

    public void LoadSchematic(string path)
    {
        haveSimulationError = false;
        SimulationProcessor.LoadSchematic(path);
    }

    private void LoadDefaultSchematic()
    {
        Assembly assembly = typeof(LiveSPICELinuxPlugin).Assembly;
        string? resourceName = assembly.GetManifestResourceNames().SingleOrDefault(i => i == "LiveSPICE.PluginLinux.DefaultSchematic.schx");
        if (resourceName == null)
            return;

        string defaultSchematicPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LiveSPICE",
            "PluginLinux",
            "DefaultSchematic.schx");
        Directory.CreateDirectory(Path.GetDirectoryName(defaultSchematicPath)!);

        using Stream resource = assembly.GetManifestResourceStream(resourceName)!;
        using FileStream file = File.Create(defaultSchematicPath);
        resource.CopyTo(file);
        file.Close();

        LoadSchematic(defaultSchematicPath);
    }

    public override byte[] SaveState()
    {
        PluginProgramParameters parameters = PluginProgramParameters.FromProcessor(SimulationProcessor);
        XmlSerializer serializer = new XmlSerializer(typeof(PluginProgramParameters));
        using MemoryStream memoryStream = new MemoryStream();
        serializer.Serialize(memoryStream, parameters);
        return memoryStream.ToArray();
    }

    public override void RestoreState(byte[] stateData)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(PluginProgramParameters));
        try
        {
            using MemoryStream memoryStream = new MemoryStream(stateData);
            if (serializer.Deserialize(memoryStream) is not PluginProgramParameters parameters)
                return;

            if (string.IsNullOrEmpty(parameters.SchematicPath))
            {
                haveSimulationError = false;
                SimulationProcessor.ClearSchematic();
            }
            else
            {
                LoadSchematic(parameters.SchematicPath);
            }

            parameters.ApplyTo(SimulationProcessor);
        }
        catch (Exception ex)
        {
            Logger.Log("Load state failed: " + ex.Message);
        }
    }

    public override void Process()
    {
        base.Process();

        if (monoInput == null || monoOutput == null)
            return;

        if (haveSimulationError)
        {
            monoInput.PassThroughTo(monoOutput);
            return;
        }

        double[][] inputBuffers = monoInput.GetAudioBuffers();
        double[][] outputBuffers = monoOutput.GetAudioBuffers();

        try
        {
            SimulationProcessor.RunSimulation(inputBuffers, outputBuffers, inputBuffers[0].Length);
        }
        catch (Exception ex)
        {
            haveSimulationError = true;
            Logger.Log("Error running circuit simulation: " + ex.Message);
            monoInput.PassThroughTo(monoOutput);
        }
    }
}
