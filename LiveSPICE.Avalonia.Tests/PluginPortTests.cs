using AudioPlugSharp;
using System.IO;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using LiveSPICE.Avalonia;
using LiveSPICE.PluginCore;
using LiveSPICE.PluginLinux;
using Xunit;

namespace LiveSPICE.Avalonia.Tests;

public class PluginPortTests
{
    [Fact]
    public void LinuxPluginInitializesMonoPorts()
    {
        LiveSPICELinuxPlugin plugin = new LiveSPICELinuxPlugin();

        plugin.Initialize();

        Assert.Single(plugin.InputPorts);
        Assert.Single(plugin.OutputPorts);
        Assert.Equal(EAudioChannelConfiguration.Mono, plugin.InputPorts[0].ChannelConfiguration);
        Assert.Equal(EAudioChannelConfiguration.Mono, plugin.OutputPorts[0].ChannelConfiguration);
        Assert.True(plugin.HasUserInterface);
        Assert.Equal(700u, plugin.EditorWidth);
        Assert.Equal(420u, plugin.EditorHeight);
    }

    [Fact]
    public void LinuxPluginStartsWithoutHardcodedSchematicByDefault()
    {
        LiveSPICELinuxPlugin plugin = new LiveSPICELinuxPlugin();

        Assert.Null(plugin.SimulationProcessor.Schematic);
        Assert.Empty(plugin.SchematicPath);
        Assert.DoesNotContain("LiveSPICE.PluginLinux.DefaultSchematic.schx", typeof(LiveSPICELinuxPlugin).Assembly.GetManifestResourceNames());
    }

    [AvaloniaFact]
    public void LinuxPluginCreatesAvaloniaEditorBoundToProcessor()
    {
        LiveSPICELinuxPlugin plugin = new LiveSPICELinuxPlugin();
        PluginEditorWindow editor = RunOnUiThread(plugin.CreateEditorWindow);
        try
        {
            Assert.Equal("Load Schematic", editor.TestLoadedText);
            Assert.Equal(0, editor.TestControlPanelCount);
            Assert.Equal(0, editor.TestOverlayControlCount);

            plugin.LoadSchematic(FindFixture("Tests/Circuits/59 Bassman Preamp.schx"));
            editor.LoadSchematic(plugin.SchematicPath);

            Assert.Equal(plugin.SimulationProcessor.SchematicName, editor.TestLoadedText);
            Assert.Equal(plugin.SimulationProcessor.InteractiveComponents.Count, editor.TestControlPanelCount);
            Assert.Equal(plugin.SimulationProcessor.InteractiveComponents.Count, editor.TestOverlayControlCount);
        }
        finally
        {
            CloseOnUiThread(editor);
        }
    }

    [AvaloniaFact]
    public void PluginEditorSettingsUpdateSharedProcessor()
    {
        SimulationProcessor processor = new SimulationProcessor();
        PluginEditorWindow editor = RunOnUiThread(() => new PluginEditorWindow(processor));
        try
        {
            editor.TestSelectedOversample = 8;
            editor.TestSelectedIterations = 32;

            Assert.Equal(8, processor.Oversample);
            Assert.Equal(32, processor.Iterations);
        }
        finally
        {
            CloseOnUiThread(editor);
        }
    }

    [Fact]
    public void PluginProgramParametersRoundTripProcessorSettings()
    {
        SimulationProcessor processor = new SimulationProcessor
        {
            Oversample = 4,
            Iterations = 16,
        };

        PluginProgramParameters parameters = PluginProgramParameters.FromProcessor(processor);
        SimulationProcessor restored = new SimulationProcessor();
        parameters.ApplyTo(restored);

        Assert.Equal(4, restored.Oversample);
        Assert.Equal(16, restored.Iterations);
    }

    [Fact]
    public void LinuxPluginStateRoundTripsProcessorSettings()
    {
        LiveSPICELinuxPlugin plugin = new LiveSPICELinuxPlugin();
        string schematicPath = FindFixture("Tests/Circuits/59 Bassman Preamp.schx");
        plugin.LoadSchematic(schematicPath);
        plugin.SimulationProcessor.Oversample = 8;
        plugin.SimulationProcessor.Iterations = 32;
        SetDistinctControlValues(plugin.SimulationProcessor);

        byte[] state = plugin.SaveState();
        LiveSPICELinuxPlugin restored = new LiveSPICELinuxPlugin();
        restored.RestoreState(state);

        Assert.Equal(schematicPath, restored.SchematicPath);
        Assert.Equal(8, restored.SimulationProcessor.Oversample);
        Assert.Equal(32, restored.SimulationProcessor.Iterations);
        AssertControlValuesEqual(plugin.SimulationProcessor, restored.SimulationProcessor);
    }

    [Fact]
    public void PluginEditorCreatesOverlayControlsForInteractiveSchematic()
    {
        SimulationProcessor processor = new SimulationProcessor();
        string schematicPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Tests/Circuits/59 Bassman Preamp.schx"));
        processor.LoadSchematic(schematicPath);

        Assert.NotEmpty(processor.InteractiveComponents);
        Assert.All(processor.InteractiveComponents, wrapper =>
            Assert.Contains(processor.Schematic!.Symbols, symbol => PluginEditorWindow.WrapperMatchesSymbol(wrapper, symbol)));
    }

    [Fact]
    public void SimulationProcessorPassesThroughWhenNoSchematicIsLoaded()
    {
        SimulationProcessor processor = new SimulationProcessor();
        double[][] input = new[] { Enumerable.Range(0, 64).Select(i => Math.Sin(i / 8.0)).ToArray() };
        double[][] output = new[] { new double[64] };

        processor.RunSimulation(input, output, output[0].Length);

        Assert.Equal(input[0], output[0]);
    }

    [Fact]
    public void SimulationProcessorProcessesLoadedRcSchematic()
    {
        SimulationProcessor processor = new SimulationProcessor
        {
            SampleRate = 48000,
            Oversample = 4,
            Iterations = 8,
        };
        processor.LoadSchematic(FindFixture("Tests/Circuits/Passive 1stOrder Highpass RC.schx"));
        double[][] input = new[] { Enumerable.Range(0, 512).Select(i => Math.Sin(2 * Math.PI * 440 * i / 48000)).ToArray() };
        double[][] output = new[] { new double[512] };

        RunUntilReady(processor, input, output, output[0].Length);

        Assert.Contains(output[0], i => Math.Abs(i) > 1e-9);
        Assert.NotEqual(input[0], output[0]);
        Assert.DoesNotContain(output[0], double.IsNaN);
    }

    [AvaloniaFact]
    public void PluginLoadsMxrPhase90WithControlsAndProducesStableAudio()
    {
        LiveSPICELinuxPlugin plugin = new LiveSPICELinuxPlugin();
        string schematicPath = FindFixture("Tests/Examples/MXR Phase 90.schx");
        plugin.LoadSchematic(schematicPath);

        PotWrapper[] pots = plugin.SimulationProcessor.InteractiveComponents.OfType<PotWrapper>().ToArray();
        Assert.Equal(new[] { "Speed", "Trimmer" }, pots.Select(i => i.Name).OrderBy(i => i).ToArray());

        PluginEditorWindow editor = RunOnUiThread(plugin.CreateEditorWindow);
        try
        {
            editor.LoadSchematic(schematicPath);
            Assert.Equal("MXR Phase 90", editor.TestLoadedText);
            Assert.Equal(2, editor.TestControlPanelCount);
            Assert.Equal(2, editor.TestOverlayControlCount);
        }
        finally
        {
            CloseOnUiThread(editor);
        }

        double[][] input = new[] { Enumerable.Range(0, 1024).Select(i => 0.1 * Math.Sin(2 * Math.PI * 220 * i / 48000)).ToArray() };
        double[][] output = new[] { new double[1024] };
        plugin.SimulationProcessor.SampleRate = 48000;
        plugin.SimulationProcessor.Oversample = 4;
        plugin.SimulationProcessor.Iterations = 8;

        RunUntilReady(plugin.SimulationProcessor, input, output, output[0].Length);

        Assert.Contains(output[0], i => Math.Abs(i) > 1e-9);
        Assert.DoesNotContain(output[0], double.IsNaN);
    }

    private static void RunUntilReady(SimulationProcessor processor, double[][] input, double[][] output, int length)
    {
        Exception? lastException = null;
        for (int attempt = 0; attempt < 20; attempt++)
        {
            Array.Clear(output[0]);
            try
            {
                processor.RunSimulation(input, output, length);
                if (output[0].Any(i => Math.Abs(i) > 1e-12) && !output[0].SequenceEqual(input[0]))
                    return;
            }
            catch (NullReferenceException ex)
            {
                lastException = ex;
            }
            Thread.Sleep(50);
        }

        if (lastException != null)
            throw lastException;
    }

    private static void SetDistinctControlValues(SimulationProcessor processor)
    {
        int position = 0;
        foreach (IComponentWrapper wrapper in processor.InteractiveComponents)
        {
            switch (wrapper)
            {
                case PotWrapper potWrapper:
                    potWrapper.PotValue = 0.1 + position * 0.03;
                    break;
                case DoubleThrowWrapper doubleThrowWrapper:
                    doubleThrowWrapper.Engaged = position % 2 == 0;
                    break;
                case MultiThrowWrapper multiThrowWrapper:
                    multiThrowWrapper.Position = position % 3;
                    break;
            }
            position++;
        }
    }

    private static void AssertControlValuesEqual(SimulationProcessor expected, SimulationProcessor actual)
    {
        Assert.Equal(expected.InteractiveComponents.Count, actual.InteractiveComponents.Count);
        for (int index = 0; index < expected.InteractiveComponents.Count; index++)
        {
            IComponentWrapper expectedWrapper = expected.InteractiveComponents[index];
            IComponentWrapper actualWrapper = actual.InteractiveComponents[index];
            Assert.Equal(expectedWrapper.Name, actualWrapper.Name);
            switch (expectedWrapper)
            {
                case PotWrapper expectedPot:
                    PotWrapper actualPot = Assert.IsType<PotWrapper>(actualWrapper);
                    Assert.Equal(expectedPot.PotValue, actualPot.PotValue, 12);
                    break;
                case DoubleThrowWrapper expectedSwitch:
                    DoubleThrowWrapper actualDoubleThrow = Assert.IsType<DoubleThrowWrapper>(actualWrapper);
                    Assert.Equal(expectedSwitch.Engaged, actualDoubleThrow.Engaged);
                    break;
                case MultiThrowWrapper expectedSwitch:
                    MultiThrowWrapper actualMultiThrow = Assert.IsType<MultiThrowWrapper>(actualWrapper);
                    Assert.Equal(expectedSwitch.Position, actualMultiThrow.Position);
                    break;
            }
        }
    }

    private static T RunOnUiThread<T>(Func<T> action)
    {
        return Dispatcher.UIThread.CheckAccess()
            ? action()
            : Dispatcher.UIThread.Invoke(action);
    }

    private static void CloseOnUiThread(PluginEditorWindow editor)
    {
        if (Dispatcher.UIThread.CheckAccess())
            editor.Close();
        else
            Dispatcher.UIThread.Invoke(editor.Close);
    }

    private static string FindFixture(string relativePath)
    {
        DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            string candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
                return candidate;
            directory = directory.Parent;
        }
        throw new FileNotFoundException("Could not locate test fixture.", relativePath);
    }
}
