using Circuit;
using LiveSPICE.Avalonia;
using Xunit;

namespace LiveSPICE.Avalonia.Tests;

public sealed class SimulationAndAudioTests
{
    [Fact]
    public void AudioSimulationFactoryRunsPassiveHighpass()
    {
        Circuit.Circuit circuit = Schematic.Load(FindFixture("Tests/Circuits/Passive 1stOrder Highpass RC.schx")).Build();
        Simulation simulation = AudioSimulationFactory.Create(circuit, 48000, 4, 8);
        double[] input = Enumerable.Range(0, 512).Select(i => Math.Sin(2 * Math.PI * 440 * i / 48000)).ToArray();
        double[] output = new double[input.Length];

        simulation.Run(input.Length, new[] { input }, new[] { output });

        Assert.Contains(output, i => Math.Abs(i) > 1e-9);
        Assert.DoesNotContain(output, double.IsNaN);
    }

    [Fact]
    public void AudioSimulationFactoryRejectsCircuitWithoutInput()
    {
        Circuit.Circuit circuit = new Circuit.Circuit();

        Assert.Throws<NotSupportedException>(() => AudioSimulationFactory.Create(circuit, 48000, 4, 8));
    }

    [Fact]
    public void VirtualAudioDriverProvidesLoopbackDeviceAndChannels()
    {
        VirtualAudioDriver driver = new VirtualAudioDriver();
        Audio.Device device = driver.Devices.Single();

        Assert.Equal("LiveSPICE Virtual Audio", driver.Name);
        Assert.Equal("LiveSPICE Virtual Audio", driver.ToString());
        Assert.Equal("Managed loopback", device.Name);
        Assert.Equal("Managed loopback", device.ToString());
        Assert.Single(device.InputChannels);
        Assert.Single(device.OutputChannels);
    }

    [Fact]
    public void LinuxAudioDiscoveryFiltersNonAudioPorts()
    {
        LinuxAudioDriver driver = new LinuxAudioDriver();

        Assert.Equal("PipeWire/JACK", driver.Name);
        Assert.Equal("PipeWire/JACK", driver.ToString());
        foreach (Audio.Device device in driver.Devices)
        {
            Assert.Equal("PipeWire/JACK port graph", device.Name);
            Assert.Equal("PipeWire/JACK port graph", device.ToString());
            Assert.DoesNotContain(device.InputChannels.Concat(device.OutputChannels), i => i.Name.StartsWith("Midi-Bridge:", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void AvaloniaAudioDriversListsLinuxAndVirtualDriversOnce()
    {
        Audio.Driver[] drivers = AvaloniaAudioDrivers.Available().ToArray();
        string[] names = drivers.Select(i => i.Name).ToArray();

        Assert.Contains("PipeWire/JACK", names);
        Assert.Contains("LiveSPICE Virtual Audio", names);
        Assert.Equal(names.Length, names.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void LinuxAudioDiscoveryPrefersJackPortNamesOverRawPipeWireNodes()
    {
        string[] ports = LinuxAudioDiscovery.PreferredPorts(
            new[] { "Built-in Audio Analog Stereo:capture_FL", "Midi-Bridge:Midi Through:(capture_0) Midi Through Port-0" },
            new[] { "alsa_input.pci-0000_00_1f.3.analog-stereo:capture_FL" }).ToArray();

        Assert.Equal(new[] { "Built-in Audio Analog Stereo:capture_FL" }, ports);
    }

    [Fact]
    public void WaveformWindowDiscoversNodeProbeCandidates()
    {
        Circuit.Circuit circuit = Schematic.Load(FindFixture("Tests/Circuits/Passive 1stOrder Highpass RC.schx")).Build();

        ProbeCandidate[] candidates = WaveformWindow.ProbeCandidates(circuit).ToArray();

        Assert.NotEmpty(candidates);
        Assert.DoesNotContain(candidates, i => string.IsNullOrWhiteSpace(i.Name) || i.Name == "0");
        Assert.DoesNotContain(candidates, i => i.Expression.EqualsZero());
    }

    [Fact]
    public void WaveformWindowGeneratesFallbackSineInput()
    {
        double[] input = new double[32];

        WaveformWindow.FillInputBuffer(input, 48000, 440, 1, null);

        Assert.Contains(input, i => Math.Abs(i) > 1e-9);
    }

    [Fact]
    public void WaveformWindowGeneratesExpressionInput()
    {
        double[] input = new double[8];

        WaveformWindow.FillInputBuffer(input, 48000, 440, 2, "0.125");

        Assert.All(input, i => Assert.Equal(0.25, i, 12));
    }

    [Fact]
    public void VirtualAudioStreamInvokesCallbackUntilStopped()
    {
        VirtualAudioDriver driver = new VirtualAudioDriver();
        Audio.Device device = driver.Devices.Single();
        using ManualResetEventSlim called = new ManualResetEventSlim(false);
        int callbackCount = 0;

        Audio.Stream stream = device.Open((count, input, output, rate) =>
        {
            callbackCount++;
            Assert.Equal(256, count);
            Assert.Equal(48000, rate);
            Assert.Single(input);
            Assert.Single(output);
            output[0][0] = 0.25;
            called.Set();
        }, device.InputChannels, device.OutputChannels);

        Assert.True(called.Wait(TimeSpan.FromSeconds(2)));
        stream.Stop();
        Assert.True(callbackCount > 0);
    }

    [Fact]
    public async Task LiveAudioProcessorCanRunOffUiThread()
    {
        Schematic schematic = Schematic.Load(FindFixture("Tests/Circuits/Passive 1stOrder Highpass RC.schx"));
        LiveAudioProcessor processor = new LiveAudioProcessor(schematic);
        processor.Start(48000, 8, 8);
        using Audio.SampleBuffer input = new Audio.SampleBuffer(128);
        using Audio.SampleBuffer output = new Audio.SampleBuffer(128);
        for (int i = 0; i < input.Samples.Length; i++)
            input[i] = Math.Sin(2 * Math.PI * 440 * i / 48000);

        await Task.Run(() => processor.Process(128, new[] { input }, new[] { output }, 48000));

        Assert.Contains(output.Samples, i => Math.Abs(i) > 1e-12);
    }

    [Fact]
    public void LinuxJackLiveModeProcessesBuiltInMicrophoneThroughRcFilter()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("LIVESPICE_RUN_JACK_HARDWARE_TEST"), "1", StringComparison.Ordinal))
            return;

        LinuxAudioDriver driver = new LinuxAudioDriver();
        Audio.Device? device = driver.Devices.SingleOrDefault();
        Assert.NotNull(device);

        Audio.Channel? microphone = BuiltIn(device!.InputChannels, ":capture_FL") ?? BuiltIn(device.InputChannels, ":capture_");
        Audio.Channel? playback = BuiltIn(device.OutputChannels, ":playback_FL") ?? BuiltIn(device.OutputChannels, ":playback_");
        Assert.NotNull(microphone);
        Assert.NotNull(playback);

        Schematic schematic = Schematic.Load(FindFixture("Tests/Circuits/Passive 1stOrder Highpass RC.schx"));
        LiveAudioProcessor liveProcessor = new LiveAudioProcessor(schematic);
        List<double[]> capturedInputs = new List<double[]>();
        List<double[]> capturedOutputs = new List<double[]>();
        using ManualResetEventSlim completed = new ManualResetEventSlim(false);
        int callbackCount = 0;

        Audio.Stream stream = device.Open((count, input, output, rate) =>
        {
            if (callbackCount == 0)
                liveProcessor.Start(rate, 4, 8);

            double[] inputCopy = input.Length == 0 ? new double[count] : input[0].Samples.Take(count).ToArray();
            liveProcessor.Process(count, input, output, rate);
            double[] outputCopy = output.Length == 0 ? new double[count] : output[0].Samples.Take(count).ToArray();

            lock (capturedInputs)
            {
                capturedInputs.Add(inputCopy);
                capturedOutputs.Add(outputCopy);
                callbackCount++;
                if (callbackCount >= 8)
                    completed.Set();
            }
        }, new[] { microphone! }, new[] { playback! });

        try
        {
            Assert.True(completed.Wait(TimeSpan.FromSeconds(4)), "Timed out waiting for PipeWire/JACK live audio callbacks.");
        }
        finally
        {
            stream.Stop();
            liveProcessor.Stop();
        }

        double inputRms = Rms(capturedInputs.SelectMany(i => i));
        Assert.True(inputRms > 1e-5, $"Captured built-in microphone input is too close to silence. RMS={inputRms:R}.");

        LiveAudioProcessor referenceProcessor = new LiveAudioProcessor(schematic);
        referenceProcessor.Start(stream.SampleRate, 4, 8);
        List<double> expectedSamples = new List<double>();
        List<double> actualSamples = new List<double>();
        List<double> inputSamples = new List<double>();
        for (int block = 0; block < capturedInputs.Count; block++)
        {
            using Audio.SampleBuffer input = Buffer(capturedInputs[block]);
            using Audio.SampleBuffer output = new Audio.SampleBuffer(capturedInputs[block].Length);
            double[] expected = referenceProcessor.Process(capturedInputs[block].Length, new[] { input }, new[] { output }, stream.SampleRate);

            Assert.Equal(expected.Length, capturedOutputs[block].Length);
            for (int sample = 0; sample < expected.Length; sample++)
                Assert.Equal(expected[sample], capturedOutputs[block][sample], 12);

            expectedSamples.AddRange(expected);
            actualSamples.AddRange(capturedOutputs[block]);
            inputSamples.AddRange(capturedInputs[block]);
        }
        referenceProcessor.Stop();

        double expectedRms = Rms(expectedSamples);
        double actualRms = Rms(actualSamples);
        double errorRms = Rms(expectedSamples.Zip(actualSamples, (expected, actual) => expected - actual));
        double copyErrorRms = Rms(inputSamples.Zip(actualSamples, (input, actual) => input - actual));

        Assert.True(expectedRms > 1e-8, $"Expected RC output is too close to silence. RMS={expectedRms:R}.");
        Assert.True(actualRms > 1e-8, $"Live RC output is too close to silence. RMS={actualRms:R}.");
        Assert.True(errorRms <= Math.Max(1e-10, expectedRms * 1e-8), $"Live output does not match the RC prediction. Error RMS={errorRms:R}, expected RMS={expectedRms:R}.");
        Assert.True(copyErrorRms > inputRms * 1e-3, $"Live output looks like an unfiltered copy of the input. Copy error RMS={copyErrorRms:R}, input RMS={inputRms:R}.");
    }

    private static Audio.Channel? BuiltIn(IEnumerable<Audio.Channel> channels, string suffix)
    {
        return channels.FirstOrDefault(i => i.Name.Contains("Built-in Audio", StringComparison.OrdinalIgnoreCase) && i.Name.Contains(suffix, StringComparison.OrdinalIgnoreCase));
    }

    private static Audio.SampleBuffer Buffer(double[] samples)
    {
        Audio.SampleBuffer buffer = new Audio.SampleBuffer(samples.Length);
        Array.Copy(samples, buffer.Samples, samples.Length);
        return buffer;
    }

    private static double Rms(IEnumerable<double> samples)
    {
        double sum = 0;
        int count = 0;
        foreach (double sample in samples)
        {
            sum += sample * sample;
            count++;
        }
        return count == 0 ? 0 : Math.Sqrt(sum / count);
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