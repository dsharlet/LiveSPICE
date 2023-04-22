using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Circuit;
using ComputerAlgebra;
using ComputerAlgebra.Plotting;
using Tests.Genetic;
using Util;
using Util.Cancellation;

namespace Tests
{
    internal class Test
    {
        private static TimeSpan Benchmark(double t, Action fn)
        {
            var sw = Stopwatch.StartNew();
            var ms = t * 1000;
            int iterations = 0;
            do
            {
                fn();
                iterations++;
            } while (sw.ElapsedMilliseconds < ms);
            return TimeSpan.FromMilliseconds(sw.Elapsed.TotalMilliseconds / iterations);
        }

        private static Expression FindInput(Circuit.Circuit C)
        {
            return C.Components.OfType<Input>()
                .Select(i => i.In)
                // If there are no inputs, just make a dummy.
                .DefaultIfEmpty("V[t]")
                // Require exactly one input.
                .Single();
        }

        public Dictionary<Expression, double[]> Run(
            Circuit.Circuit C,
            Func<double, double> Vin,
            int SampleRate,
            int Samples,
            int Oversample,
            int Iterations,
            Expression? Input = null,
            IEnumerable<Expression>? Outputs = null,
            ILog? log = null)
        {
            var analysis = C.Analyze();

            // By default, pass Vin to each input of the circuit.
            if (Input == null)
                Input = C.Components.Where(i => i is Input)
                    .Select(i => Component.DependentVariable(i.Name, Component.t))
                    // If there are no inputs, just make a dummy.
                    .DefaultIfEmpty("V[t]")
                    // Require exactly one input.
                    .Single();

            // By default, produce every node of the circuit as output.
            var outputs = (Outputs ?? C.Nodes.Select(i => i.V)).ToArray();

            var builder = new NewtonSimulationBuilder(log);

            var settings = new NewtonSimulationSettings(SampleRate, Oversample, Iterations, Optimize: true);

            var simulation = builder.Build(analysis, settings, new[] { Input }, outputs, CancellationStrategy.None);

            var outputBuffers = outputs.ToDictionary(i => i, i => new double[Samples]);

            double T = simulation.TimeStep;
            double t = 0;
            Random rng = new Random();
            int remaining = Samples;
            while (remaining > 0)
            {
                // Using a varying number of samples on each call to S.Run
                int N = Math.Min(remaining, rng.Next(1000, 10000));
                double[] inputBuffer = new double[N];
                List<double[]> buffers = outputs.Select(i => new double[N]).ToList();
                for (int n = 0; n < N; ++n, t += T)
                    inputBuffer[n] = Vin(t);

                simulation.Run(inputBuffer, buffers);

                for (int i = 0; i < outputs.Count(); ++i)
                    Array.Copy(buffers[i], 0, outputBuffers[outputs[i]], Samples - remaining, N);

                remaining -= N;
            }

            return outputBuffers;
        }

        /// <summary>
        /// Benchmark a circuit simulation.
        /// By default, benchmarks producing the sum of all output components.
        /// </summary>
        /// <returns>{analyze time, solve time, simulate rate} in seconds or Hz</returns>
        public double Benchmark(
            Circuit.Circuit C,
            Func<double, double> Vin,
            int SampleRate,
            int Oversample,
            int Iterations,
            int[] permutation,
            Expression? Input = null,
            IEnumerable<Expression>? Outputs = null,
            bool optimize = false,
            ILog? log = null)
        {
            double t = 0;
            permutation.Print();
            try
            {
                Analysis? analysis = null;
                var analyzeTime = Benchmark(.01, () => analysis = C.Analyze(permutation));
                Console.WriteLine($"{nameof(Circuit.Circuit.Analyze)} time: {analyzeTime}");

                // By default, pass Vin to each input of the circuit.
                if (Input == null)
                    Input = FindInput(C);

                // By default, produce every node of the circuit as output.
                if (Outputs == null)
                {
                    Expression sum = 0;
                    foreach (Speaker i in C.Components.OfType<Speaker>())
                        sum += i.Out;
                    Outputs = new[] { sum };
                }

                var builder = new NewtonSimulationBuilder(log);

                var settings = new NewtonSimulationSettings(SampleRate, Oversample, Iterations, Optimize: true);

                Simulation simulation = null;
                var solveTime = Benchmark(.01, () => simulation = builder.Build(analysis, settings, new[] { Input }, Outputs, CancellationStrategy.None));
                Console.WriteLine($"{nameof(NewtonSimulationBuilder.Build)} time: {solveTime}");

                int N = 1000;
                double[] inputBuffer = new double[N];
                List<double[]> outputBuffers = Outputs.Select(i => new double[N]).ToList();

                double T = 1.0 / SampleRate;

                var runTime = Benchmark(1, () =>
                {
                    // This is counting the cost of evaluating Vin during benchmarking...
                    for (int n = 0; n < N; ++n, t += T)
                        inputBuffer[n] = Vin(t);

                    simulation.Run(inputBuffer, outputBuffers);
                });
                double rate = N / runTime.TotalMilliseconds * 1000; // samples per second
                Console.WriteLine("{0:G3} kHz, {1:G3}x real time", rate / 1000, rate / SampleRate);
                return rate / SampleRate;
            }
            catch (SimulationDiverged ex)
            {
                Console.WriteLine("Simulation diverged");
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.ToString());
                Console.ResetColor();
            }
            return 0;
        }

        public void PlotAll(string Title, Dictionary<Expression, double[]> Outputs)
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