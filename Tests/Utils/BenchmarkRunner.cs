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

namespace LiveSPICE.Cli.Utils
{
    internal class BenchmarkRunner
    {
        private readonly ILog log;

        public BenchmarkRunner(ILog log)
        {
            this.log = log;
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
            int[]? permutation = null,
            Expression Input = null,
            IEnumerable<Expression> Outputs = null,
            bool optimize = false)
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

                //TransientSolution TS = null;
                //var solveTime = Benchmark(1, () => TS = TransientSolution.Solve(analysis, (Real)1 / (SampleRate * Oversample)));
                //System.Console.WriteLine("TransientSolution.Solve time: {0} ms", solveTime);

                //var simulation = new OldSimulation(TS)
                //{
                //    Oversample = Oversample,
                //    Iterations = Iterations,
                //    Input = new[] { Input },
                //    Output = Outputs,
                //};

                var builder = new NewtonSimulationBuilder(log);

                var settings = new NewtonSimulationSettings(SampleRate, Oversample, Iterations, Optimize: true);

                Simulation simulation = null;
                var solveTime = Benchmark(.01, () => simulation = builder.Build(analysis, settings, new[] { Input }, Outputs, CancellationStrategy.TimeoutAfter(TimeSpan.FromSeconds(30))));
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
            catch (SimulationDivergedException ex)
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

        private TimeSpan Benchmark(double t, Action fn)
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

        private Expression FindInput(Circuit.Circuit C)
        {
            return C.Components.OfType<Input>()
                .Select(i => i.In)
                // If there are no inputs, just make a dummy.
                .DefaultIfEmpty("V[t]")
                // Require exactly one input.
                .Single();
        }
    }
}
