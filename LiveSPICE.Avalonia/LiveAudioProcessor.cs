using System;
using Circuit;

namespace LiveSPICE.Avalonia;

internal sealed class LiveAudioProcessor
{
    private readonly object sync = new object();
    private readonly Schematic schematic;
    private Simulation? simulation;

    public LiveAudioProcessor(Schematic schematic)
    {
        this.schematic = schematic;
    }

    public double InputScale { get; set; } = 1;

    public double OutputScale { get; set; } = 1;

    public void Start(double sampleRate, int oversample, int iterations)
    {
        lock (sync)
        {
            simulation = AudioSimulationFactory.Create(schematic.Build(), sampleRate, oversample, iterations);
        }
    }

    public void Stop()
    {
        lock (sync)
        {
            simulation = null;
        }
    }

    public double[] Process(int count, Audio.SampleBuffer[] input, Audio.SampleBuffer[] output, double rate)
    {
        Simulation? current;
        lock (sync)
            current = simulation;

        if (current == null)
        {
            foreach (Audio.SampleBuffer buffer in output)
                buffer.Clear();
            return Array.Empty<double>();
        }

        double[] inputSamples = new double[count];
        if (input.Length > 0)
            Array.Copy(input[0].Samples, inputSamples, count);
        else
            for (int sample = 0; sample < count; sample++)
                inputSamples[sample] = 0.25 * Math.Sin(2 * Math.PI * 440 * (current.Time + sample / rate));
        for (int sample = 0; sample < count; sample++)
            inputSamples[sample] *= InputScale;

        double[] outputSamples = new double[count];
        lock (sync)
            current.Run(count, new[] { inputSamples }, new[] { outputSamples });
        for (int sample = 0; sample < count; sample++)
            outputSamples[sample] *= OutputScale;
        foreach (Audio.SampleBuffer buffer in output)
            Array.Copy(outputSamples, buffer.Samples, count);
        return outputSamples;
    }
}