using System;
using System.IO;
using Circuit;
using System.Collections.Generic;
using Util;
using System.Linq;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace Tests
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand().WithCommand("test", "Run tests", c => c
                                                    .WithArgument<string>("pattern", "Glob pattern for files to test")
                                                    .WithOption<bool>(new[] { "--plot" }, "Plot results")
                                                    .WithOption(new[] { "--samples" }, () => 4800, "Samples")
                                                    .WithHandler(CommandHandler.Create<string, bool, int, int, int, int>(Test)))
                                               .WithCommand("benchmark", "Run benchmarks", c => c
                                                    .WithArgument<string>("pattern", "Glob pattern for files to benchmark")
                                                    .WithHandler(CommandHandler.Create<string, int, int, int>(Benchmark)))
                                               .WithGlobalOption(new Option<int>("--sampleRate", () => 48000, "Sample Rate"))
                                               .WithGlobalOption(new Option<int>("--oversample", () => 8, "Oversample"))
                                               .WithGlobalOption(new Option<int>("--iterations", () => 8, "Iterations"));

            return await rootCommand.InvokeAsync(args);
        }

        public static void Test(string pattern, bool plot, int sampleRate, int samples, int oversample, int iterations)
        {
            var log = new ConsoleLog() { Verbosity = MessageType.Info };
            var tester = new Test();

            foreach (var circuit in GetCircuits(pattern, log))
            {
                var outputs = tester.Run(circuit, t => Harmonics(t, 0.5, 82, 2), sampleRate, samples, oversample, iterations);
                if (plot)
                {
                    tester.PlotAll(circuit.Name, outputs);
                }
            }
        }

        public static void Benchmark(string pattern, int sampleRate, int oversample, int iterations)
        {
            var log = new ConsoleLog() { Verbosity = MessageType.Warning };
            var tester = new Test();
            foreach (var circuit in GetCircuits(pattern, log))
            {
                System.Console.WriteLine("Benchmarking {0}...", circuit.Name);
                tester.Benchmark(circuit, t => Harmonics(t, 0.5, 82, 2), sampleRate, oversample, iterations, log: log);
                System.Console.WriteLine("");
            }
        }

        private static IEnumerable<Circuit.Circuit> GetCircuits(string glob, ILog log) => Globber.Glob(glob).Select(filename =>
        {
            log.WriteLine(MessageType.Info, filename);
            var circuit = Schematic.Load(filename, log).Build();
            circuit.Name = Path.GetFileNameWithoutExtension(filename);
            return circuit;
        });

        // Generate a function with the first N harmonics of f0.
        private static double Harmonics(double t, double A, double f0, int N)
        {
            double s = 0;
            for (int i = 1; i <= N; ++i)
                s += Math.Sin(t * f0 * 2 * 3.1415 * i) / N;
            return A * s;
        }
    }
}
