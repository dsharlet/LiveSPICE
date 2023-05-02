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
            Circuit.Circuit circuit,
            Func<double, double> Vin,
            int sampleRate,
            int oversample,
            int iterations,
            bool legacy = false,
            int[]? permutation = null,
            Expression input = null,
            IEnumerable<Expression> outputs = null,
            bool optimize = false)
        {
            double t = 0;
            permutation?.Print();
            try
            {
                Analysis? analysis = null;
                var analyzeTime = Benchmark(.01, () => analysis = permutation != null ? circuit.Analyze(permutation) : circuit.Analyze());
                log.WriteLine(MessageType.Info, $"{nameof(Circuit.Circuit.Analyze)} time: [yellow]{analyzeTime}[/yellow]");

                // By default, pass Vin to each input of the circuit.
                if (input == null)
                    input = FindInput(circuit);

                // By default, produce every node of the circuit as output.
                if (outputs == null)
                {
                    Expression sum = 0;
                    foreach (Speaker i in circuit.Components.OfType<Speaker>())
                        sum += i.Out;
                    outputs = new[] { sum };
                }

                ISimulation? simulation = null;

                if (legacy)
                {
                    TransientSolution? TS = null;
                    var solveTime = Benchmark(1, () => TS = TransientSolution.Solve(analysis, (Real)1 / (sampleRate * oversample), log));
                   log.WriteLine(MessageType.Info,$"Solve time: [yellow]{solveTime}[/yellow]");

                    var s = new LegacySimulation(TS)
                    {
                        Oversample = oversample,
                        Iterations = iterations,
                        Input = new[] { input },
                        Output = outputs,
                    };

                    var buildTime = Benchmark(1, () => s.Build());
                    log.WriteLine(MessageType.Info, $"Build time: [yellow]{buildTime}[/yellow]");

                    simulation = s;
                } else
                {
                    var builder = new NewtonSimulationBuilder(log);

                    var settings = new NewtonSimulationSettings(sampleRate, oversample, iterations, Optimize: true);

                    TransientSolution? solution = null;

                    var solveTime = Benchmark(.01, () => solution = builder.Solve(analysis, settings));
                    log.WriteLine(MessageType.Info, $"{nameof(NewtonSimulationBuilder.Solve)} time: [yellow]{solveTime}[/yellow]");

                    var buildTime = Benchmark(.01, () => simulation = builder.Build(solution, settings, new[] { input }, outputs, CancellationStrategy.TimeoutAfter(TimeSpan.FromSeconds(30))));
                    log.WriteLine(MessageType.Info, $"{nameof(NewtonSimulationBuilder.Build)} time: [yellow]{buildTime}[/yellow]");

                }

                int N = 1000;
                double[] inputBuffer = new double[N];
                List<double[]> outputBuffers = outputs.Select(i => new double[N]).ToList();

                double T = 1.0 / sampleRate;

                var runTime = Benchmark(1, () =>
                {
                    // This is counting the cost of evaluating Vin during benchmarking...
                    for (int n = 0; n < N; ++n, t += T)
                        inputBuffer[n] = Vin(t);
                    simulation!.Run(inputBuffer, outputBuffers);
                });
                double rate = N / runTime.TotalMilliseconds * 1000; // samples per second
                log.WriteLine(MessageType.Info, "[yellow]{0:G3}[/yellow] kHz, [green]{1:G3}x[/green] real time", rate / 1000, rate / sampleRate);
                return rate / sampleRate;
            }
            catch (SimulationDivergedException ex)
            {
                log.WriteLine(MessageType.Error, "[red]Simulation diverged[/red]");
            }
            catch (Exception e)
            {
                log.WriteLine(MessageType.Error, $"[red]{e}[/red]");
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
