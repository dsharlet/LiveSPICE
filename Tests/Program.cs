using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Circuit;
using Tests.Genetic;
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
            var log = new ConsoleLog() { Verbosity = MessageType.Error };
            var tester = new Test();
            string fmt = "{0,-40}{1,12:G4}{2,12:G4}{3,12:G4}{4,12:G4}";
            System.Console.WriteLine(fmt, "Circuit", "Analysis (ms)", "Solve (ms)", "Sim (kHz)", "Realtime x");
            foreach (var circuit in GetCircuits(pattern, log))
            {
                double[] result = tester.Benchmark(circuit, t => Harmonics(t, 0.5, 82, 2), sampleRate, oversample, iterations, log: log);
                double analyzeTime = result[0];
                double solveTime = result[1];
                double simRate = result[2];
                string name = circuit.Name;
                if (name.Length > 39)
                    name = name.Substring(0, 39);
                System.Console.WriteLine(fmt, name, analyzeTime * 1000, solveTime * 1000, simRate / 1000, simRate / sampleRate);
            }

        }

        private static void Optimize(Test tester, Circuit.Circuit C, Schematic schematic, bool optimize)
        {
            (int[] permutation, double score)[] ranking =
            {
                (Enumerable.Range(0, C.Components.Count).ToArray(), 0),
                (Enumerable.Range(0, C.Components.Count).ToArray(), 0),
                //(new []{0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 62, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 24, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79}, 0)
                //(new []{0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 51, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 45, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 54, 46, 47, 48, 49, 50, 17, 52, 53, 31, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79}, 0),
                //(new []{0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 51, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 45, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 54, 46, 47, 48, 49, 50, 17, 52, 53, 31, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79}, 0)
                //(new []{ 1, 0, 2, 3, 4 }, 0),
                //(new []{ 1, 2, 3, 0, 4 }, 0)
            };

            while (true)
            {

                var candidates = ranking
                    .Select(i => i.permutation)
                    .Concat(Enumerable.Range(1, 3).Select(i => ranking[0].permutation.Crossover(ranking[1].permutation).Permutate(i)))
                    .Append(Enumerable.Range(0, C.Components.Count).ToArray().Permutate(C.Components.Count));

                var results = candidates.Select(permutation => (permutation, score: tester.Benchmark(C, t => Harmonics(t, .5, 82, 2), SampleRate, Oversample, Iterations, permutation, optimize: optimize, log: log)));

                ranking = results.OrderByDescending(r => r.score).Take(2).ToArray();
                Console.WriteLine($"Best: {ranking[0].score}x");

                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    switch (key.Key)
                    {
                        case ConsoleKey.X:
                            {
                                var best = ranking[0].permutation;
                                var elements = schematic.Elements.ToArray();
                                var symbols = elements.OfType<Symbol>().ToArray();
                                var other = elements.Except(symbols);
                                schematic.Elements.Clear();
                                schematic.Elements.AddRange(best.Select(i => symbols[i]).Concat(other));
                                schematic.Save($"./{C.Name}_optimized.schx");
                                break;
                            }
                        default:
                            break;
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
