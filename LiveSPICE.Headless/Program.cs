using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Circuit.Headless
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                Options options = Options.Parse(args);
                WaveData inputWave = options.InputWavPath != null ? WaveFile.ReadMono(options.InputWavPath) : null;

                if (inputWave != null)
                {
                    if (!options.HasExplicitSampleRate)
                        options.SetSampleRate(inputWave.SampleRate);
                    if (!options.HasExplicitSampleCount)
                        options.SetSampleCount(inputWave.Samples.Length);
                }

                Schematic schematic = Schematic.Load(options.SchematicPath);
                Circuit circuit = schematic.Build();
                Simulation simulation = AudioSimulationFactory.Create(circuit, options.SampleRate, options.Oversample, options.Iterations);

                double[] input = new double[options.BatchSize];
                double[] output = new double[options.BatchSize];
                double[] renderedOutput = options.OutputWavPath != null ? new double[options.SampleCount] : null;

                double min = double.PositiveInfinity;
                double max = double.NegativeInfinity;
                double sumSquares = 0;
                double last = 0;

                int remaining = options.SampleCount;
                int generated = 0;
                while (remaining > 0)
                {
                    int count = Math.Min(remaining, input.Length);
                    FillInput(input, count, generated, options, inputWave);
                    Array.Clear(output, 0, count);

                    simulation.Run(count, new[] { input }, new[] { output });

                    for (int i = 0; i < count; i++)
                    {
                        double sample = output[i];
                        min = Math.Min(min, sample);
                        max = Math.Max(max, sample);
                        sumSquares += sample * sample;
                        last = sample;
                    }

                    if (renderedOutput != null)
                        Array.Copy(output, 0, renderedOutput, generated, count);

                    remaining -= count;
                    generated += count;
                }

                double rms = Math.Sqrt(sumSquares / options.SampleCount);

                if (renderedOutput != null)
                    WaveFile.WriteMono16(options.OutputWavPath, renderedOutput, (int)Math.Round(options.SampleRate));

                Console.WriteLine("Headless simulation completed.");
                Console.WriteLine($"Schematic: {Path.GetFileName(options.SchematicPath)}");
                Console.WriteLine($"Samples: {options.SampleCount} at {options.SampleRate.ToString(CultureInfo.InvariantCulture)} Hz");
                if (options.InputWavPath != null)
                    Console.WriteLine($"Input WAV: {options.InputWavPath}");
                if (options.OutputWavPath != null)
                    Console.WriteLine($"Output WAV: {options.OutputWavPath}");
                Console.WriteLine($"Output min={min.ToString("R", CultureInfo.InvariantCulture)} max={max.ToString("R", CultureInfo.InvariantCulture)} rms={rms.ToString("R", CultureInfo.InvariantCulture)} last={last.ToString("R", CultureInfo.InvariantCulture)}");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return 1;
            }
        }

        private static void FillInput(double[] input, int count, int offset, Options options, WaveData inputWave)
        {
            if (inputWave != null)
            {
                Array.Clear(input, 0, input.Length);

                int available = Math.Max(0, Math.Min(count, inputWave.Samples.Length - offset));
                if (available > 0)
                    Array.Copy(inputWave.Samples, offset, input, 0, available);
                return;
            }

            double radiansPerSample = 2 * Math.PI * options.Frequency / options.SampleRate;
            for (int i = 0; i < count; i++)
                input[i] = options.Amplitude * Math.Sin((offset + i) * radiansPerSample);
        }

        private sealed class Options
        {
            public string SchematicPath { get; private set; }
            public double SampleRate { get; private set; } = 48000;
            public int Oversample { get; private set; } = 1;
            public int Iterations { get; private set; } = 16;
            public int SampleCount { get; private set; } = 48000;
            public int BatchSize { get; private set; } = 256;
            public double Frequency { get; private set; } = 440;
            public double Amplitude { get; private set; } = 0.25;
            public string InputWavPath { get; private set; }
            public string OutputWavPath { get; private set; }
            public bool HasExplicitSampleRate { get; private set; }
            public bool HasExplicitSampleCount { get; private set; }

            public static Options Parse(string[] args)
            {
                if (args.Length == 0)
                    throw new ArgumentException("Usage: LiveSPICE.Headless <schematic> [--input-wav path] [--output-wav path] [--sample-rate hz] [--oversample n] [--iterations n] [--samples n] [--batch-size n] [--frequency hz] [--amplitude value]");

                Options options = new Options
                {
                    SchematicPath = Path.GetFullPath(args[0])
                };

                for (int i = 1; i < args.Length; i += 2)
                {
                    if (i + 1 >= args.Length)
                        throw new ArgumentException($"Missing value for '{args[i]}'.");

                    string value = args[i + 1];
                    switch (args[i])
                    {
                        case "--input-wav":
                            options.InputWavPath = Path.GetFullPath(value);
                            break;
                        case "--output-wav":
                            options.OutputWavPath = Path.GetFullPath(value);
                            break;
                        case "--sample-rate":
                            options.SampleRate = ParseDouble(args[i], value);
                            options.HasExplicitSampleRate = true;
                            break;
                        case "--oversample":
                            options.Oversample = ParseInt(args[i], value);
                            break;
                        case "--iterations":
                            options.Iterations = ParseInt(args[i], value);
                            break;
                        case "--samples":
                            options.SampleCount = ParseInt(args[i], value);
                            options.HasExplicitSampleCount = true;
                            break;
                        case "--batch-size":
                            options.BatchSize = ParseInt(args[i], value);
                            break;
                        case "--frequency":
                            options.Frequency = ParseDouble(args[i], value);
                            break;
                        case "--amplitude":
                            options.Amplitude = ParseDouble(args[i], value);
                            break;
                        default:
                            throw new ArgumentException($"Unknown option '{args[i]}'.");
                    }
                }

                if (!File.Exists(options.SchematicPath))
                    throw new FileNotFoundException("Schematic file not found.", options.SchematicPath);
                if (options.InputWavPath != null && !File.Exists(options.InputWavPath))
                    throw new FileNotFoundException("Input WAV file not found.", options.InputWavPath);
                if (options.SampleRate <= 0)
                    throw new ArgumentOutOfRangeException(nameof(options.SampleRate));
                if (options.Oversample <= 0)
                    throw new ArgumentOutOfRangeException(nameof(options.Oversample));
                if (options.Iterations <= 0)
                    throw new ArgumentOutOfRangeException(nameof(options.Iterations));
                if (options.SampleCount <= 0)
                    throw new ArgumentOutOfRangeException(nameof(options.SampleCount));
                if (options.BatchSize <= 0)
                    throw new ArgumentOutOfRangeException(nameof(options.BatchSize));

                return options;
            }

            private static int ParseInt(string option, string value)
            {
                return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                    ? parsed
                    : throw new ArgumentException($"Invalid integer value '{value}' for '{option}'.");
            }

            private static double ParseDouble(string option, string value)
            {
                return double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double parsed)
                    ? parsed
                    : throw new ArgumentException($"Invalid numeric value '{value}' for '{option}'.");
            }

            public void SetSampleRate(double sampleRate)
            {
                SampleRate = sampleRate;
            }

            public void SetSampleCount(int sampleCount)
            {
                SampleCount = sampleCount;
            }
        }
    }
}