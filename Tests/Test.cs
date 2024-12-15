﻿using Circuit;
using ComputerAlgebra;
using Plotting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Util;

namespace Tests
{
    internal class Test
    {
        private static double Benchmark(double t, Action fn)
        {
            DateTime begin = DateTime.Now;
            int iterations = 0;
            do
            {
                fn();
                iterations++;
            } while ((DateTime.Now - begin).TotalSeconds < t);
            return (DateTime.Now - begin).TotalSeconds / iterations;
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

        public Dictionary<Expression, List<double>> Run(
            Circuit.Circuit C,
            Func<double, double> Vin,
            int SampleRate,
            int Samples,
            int Oversample,
            int Iterations,
            Expression? Input = null,
            IEnumerable<Expression>? Outputs = null)
        {
            Analysis analysis = C.Analyze();
            TransientSolution TS = TransientSolution.Solve(analysis, (Real)1 / (SampleRate * Oversample));

            // By default, pass Vin to each input of the circuit.
            if (Input == null)
                Input = C.Components.Where(i => i is Input)
                    .Select(i => Component.DependentVariable(i.Name, Component.t))
                    // If there are no inputs, just make a dummy.
                    .DefaultIfEmpty("V[t]")
                    // Require exactly one input.
                    .Single();

            // By default, produce every node of the circuit as output.
            if (Outputs == null)
                Outputs = C.Nodes.Select(i => i.V);

            Simulation S = new Simulation(TS)
            {
                Oversample = Oversample,
                Iterations = Iterations,
                Input = new[] { Input },
                Output = Outputs,
            };

            Dictionary<Expression, List<double>> outputs = 
                S.Output.ToDictionary(i => i, i => new List<double>(Samples));

            double T = S.TimeStep;
            double t = 0;
            Random rng = new Random();
            int remaining = Samples;
            while (remaining > 0)
            {
                // Using a varying number of samples on each call to S.Run
                int N = Math.Min(remaining, rng.Next(1000, 10000));
                double[] inputBuffer = new double[N];
                List<double[]> outputBuffers = S.Output.Select(i => new double[N]).ToList();
                for (int n = 0; n < N; ++n, t += T)
                    inputBuffer[n] = Vin(t);

                S.Run(inputBuffer, outputBuffers);

                for (int i = 0; i < S.Output.Count(); ++i)
                    outputs[S.Output.ElementAt(i)].AddRange(outputBuffers[i]);

                remaining -= N;
            }

            return outputs;
        }

        /// <summary>
        /// Benchmark a circuit simulation.
        /// By default, benchmarks producing the sum of all output components.
        /// </summary>
        /// <returns>{analyze time, solve time, simulate rate} in seconds or Hz</returns>
        public double[] Benchmark(
            Circuit.Circuit C,
            Func<double, double> Vin,
            int SampleRate,
            int Oversample,
            int Iterations,
            Expression? Input = null,
            IEnumerable<Expression>? Outputs = null,
            ILog? log = null)
        {
            Analysis? analysis = null;
            double analyzeTime = Benchmark(1, () => analysis = C.Analyze());

            TransientSolution? TS = null;
            double solveTime = Benchmark(1, () => TS = TransientSolution.Solve(analysis, (Real)1 / (SampleRate * Oversample), log));

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

            Simulation S = new Simulation(TS)
            {
                Oversample = Oversample,
                Iterations = Iterations,
                Input = new[] { Input },
                Output = Outputs,
            };

            int N = 1000;
            double[] inputBuffer = new double[N];
            List<double[]> outputBuffers = Outputs.Select(i => new double[N]).ToList();

            double T = 1.0 / SampleRate;
            double t = 0;
            double runTime = Benchmark(3, () =>
            {
                // This is counting the cost of evaluating Vin during benchmarking...
                for (int n = 0; n < N; ++n, t += T)
                    inputBuffer[n] = Vin(t);

                S.Run(inputBuffer, outputBuffers);
            });
            double rate = N / runTime;
            return new double[] { analyzeTime, solveTime, rate };
        }

        public void PlotAll(string Title, Dictionary<Expression, List<double>> Outputs)
        {
            Plot p = new Plot()
            {
                Title = Title,
                Width = 1200,
                Height = 800,
                x0 = 0,
                x1 = Outputs.Max(i => i.Value.Count),
                xLabel = "Time (s)",
                yLabel = "Voltage (V)",
            };

            p.Series.AddRange(Outputs.Select(i => new Scatter(
                i.Value.Select((k, n) => new KeyValuePair<double, double>(n, k)).ToArray())
            { Name = i.Key.ToString() }));

            System.IO.Directory.CreateDirectory("Plots");
            p.Save("Plots\\" + Title + ".bmp");
        }

        private static Dictionary<Expression, double[]> ComputeStatistics(Dictionary<Expression, List<double>> Outputs, int Warmup)
        {
            Dictionary<Expression, double[]> stats = new Dictionary<Expression, double[]>();
            foreach (var i in Outputs)
            {
                double[] steadyState = i.Value.Skip(Warmup).ToArray();
                double mean = steadyState.Sum() / steadyState.Count();
                double min = steadyState.Min();
                double max = steadyState.Max();
                stats.Add(i.Key, new double[] { mean, min, max });
            }
            return stats;
        }

        private string StatsToString(string Title, Dictionary<Expression, List<double>> Outputs, int Warmup)
        {
            Dictionary<Expression, double[]> stats = ComputeStatistics(Outputs, Warmup);

            string cols = "{0}, {1:G5}, {2:G5}, {3:G5}";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Format(cols, "var", "mean", "min", "max"));
            foreach (var i in stats)
                sb.AppendLine(string.Format(cols, i.Key, i.Value[0], i.Value[1], i.Value[2]));

            return sb.ToString();
        }

        public void CheckStatistics(string Title, Dictionary<Expression, List<double>> Outputs, int Warmup, bool Update)
        {
            Dictionary<Expression, double[]> stats = ComputeStatistics(Outputs, Warmup);

            string cols = "{0}, {1:G5}, {2:G5}, {3:G5}";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Format(cols, "var", "mean", "min", "max"));
            foreach (var i in stats)
                sb.AppendLine(string.Format(cols, i.Key, i.Value[0], i.Value[1], i.Value[2]));

            string path = "Stats\\" + Title + ".csv";
            if (Update)
            {
                System.IO.Directory.CreateDirectory("Stats");
                File.WriteAllText(path, sb.ToString());
            }
            else
            {
                string golden = File.ReadAllText(path);
                if (golden != sb.ToString())
                    throw new Exception(String.Format("Statistics mismatch in {0}: got:\n{1}\nexpected:\n{2}", Title, sb.ToString(), golden));
            }
        }
    }
}