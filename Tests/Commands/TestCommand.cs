using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Circuit;
using ComputerAlgebra;
using ComputerAlgebra.Plotting;
using LiveSPICE.Cli;
using LiveSPICE.CLI.Utils;
using Tests;
using Util;
using Util.Cancellation;

namespace LiveSPICE.CLI.Commands
{
    internal class TestCommand : Command
    {
        public TestCommand() : base("test", "Run tests")
        {
            var pattern = new Argument<string>("pattern", "Glob pattern for files to test");
            AddArgument(pattern);

            var plot = new Option<bool>("--plot", "Plot results");
            AddOption(plot);

            var samples = new Option<int>("--samples", () => 4800, "Samples to process");
            AddOption(samples);

            AddOption(CommonOptions.Amplitude);
            AddOption(CommonOptions.Parameters);
            AddOption(CommonOptions.SampleRate);
            AddOption(CommonOptions.Oversample);
            AddOption(CommonOptions.Iterations);

            this.SetHandler(
                RunTest,
                pattern,
                plot,
                samples,
                CommonOptions.Amplitude,
                CommonOptions.Parameters,
                CommonOptions.SampleRate,
                CommonOptions.Oversample,
                CommonOptions.Iterations,
                Bind.FromServiceProvider<ILog>(),
                Bind.FromServiceProvider<SchematicReader>());
        }

        private void RunTest(string pattern,
                             bool plot,
                             int samples,
                             Quantity amplitude,
                             Dictionary<string, double> parameters,
                             int sampleRate,
                             int oversample,
                             int iterations,
                             ILog log,
                             SchematicReader reader)
        {

            foreach (var circuit in reader.GetSchematics(pattern).Select(s => s.Build(log)))
            {
                var interactive = circuit.Components
                    .OfType<IPotControl>()
                    .GroupBy(p => !string.IsNullOrEmpty(p.Group) ? p.Group : p.Name)
                    .ToDictionary(g => g.Key, p => p.ToArray(), StringComparer.OrdinalIgnoreCase);

                foreach (var (paramName, paramValue) in parameters)
                {
                    if (interactive.TryGetValue(paramName, out var components))
                    {
                        foreach (var component in components)
                        {
                            component.Position = Math.Clamp(paramValue, 0d, 1d);
                        }
                    }
                }

                var paramsString = string.Join(", ", interactive.Select(i => i.Key + '=' + i.Value[0].Position));
                log.WriteLine(MessageType.Info, $"Testing circuit with parameters: {paramsString}");

                var analysis = circuit.Analyze();

                // By default, pass Vin to each input of the circuit.
                var input = circuit.Components.Where(i => i is Input)
                    .Select(i => Component.DependentVariable(i.Name, Component.t))
                    // If there are no inputs, just make a dummy.
                    .DefaultIfEmpty("V[t]")
                    // Require exactly one input.
                    .Single();

                // By default, produce every node of the circuit as output.
                var outputs = circuit.Nodes.Select(i => i.V).ToArray();

                var builder = new NewtonSimulationBuilder(log);

                var settings = new NewtonSimulationSettings(sampleRate, oversample, iterations, Optimize: true);

                var simulation = builder.Build(analysis, settings, new[] { input }, outputs, CancellationStrategy.None);

                var outputBuffers = outputs.ToDictionary(i => i, i => new double[samples]);

                double T = simulation.TimeStep;
                double t = 0;

                Random rng = new Random();

                int remaining = samples;

                while (remaining > 0)
                {
                    // Using a varying number of samples on each call to S.Run
                    int N = Math.Min(remaining, rng.Next(1000, 10000));
                    double[] inputBuffer = new double[N];
                    List<double[]> buffers = outputs.Select(i => new double[N]).ToList();
                    for (int n = 0; n < N; ++n, t += T)
                        inputBuffer[n] = FunctionGenerator.Harmonics(t, (double)amplitude, 82, 2);

                    simulation.Run(inputBuffer, buffers);

                    for (int i = 0; i < outputs.Count(); ++i)
                        Array.Copy(buffers[i], 0, outputBuffers[outputs[i]], samples - remaining, N);

                    remaining -= N;
                }

                if (plot)
                {
                    PlotAll(circuit.Name, outputBuffers);
                }
            }
        }

        private void PlotAll(string Title, Dictionary<Expression, double[]> Outputs)
        {
            Plot p = new Plot()
            {
                Title = Title,
                Width = 1200,
                Height = 800,
                x0 = 0,
                x1 = Outputs.Max(i => i.Value.Length),
                xLabel = "Time (s)",
                yLabel = "Voltage (V)",
            };

            p.Series.AddRange(Outputs.Select(i => new Scatter(
                i.Value.Select((k, n) => new KeyValuePair<double, double>(n, k)).ToArray())
            { Name = i.Key.ToString() }));

            System.IO.Directory.CreateDirectory("Plots");
            p.Save("Plots\\" + Title + ".bmp");
        }
    }
}
