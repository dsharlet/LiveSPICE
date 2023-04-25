using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Joins;
using System.Text;
using System.Threading.Tasks;
using LiveSPICE.Cli.Utils;
using LiveSPICE.CLI.Utils;
using Tests.Genetic;
using Util;

namespace LiveSPICE.CLI.Commands
{
    internal class OptimizeCommand : Command
    {
        public OptimizeCommand() : base("optimize", "Optimize circuit using genetic algorithm")
        {
            var filename = new Argument<string>("filename", "Circuit to optimize");
            AddArgument(filename);

            this.SetHandler(
                RunOptimize,
                filename,
                GlobalOptions.SampleRate,
                GlobalOptions.Oversample,
                GlobalOptions.Iterations,
                Bind.FromServiceProvider<ILog>(),
                Bind.FromServiceProvider<SchematicReader>(),
                Bind.FromServiceProvider<BenchmarkRunner>());
        }

        private static void RunOptimize(string filename, int sampleRate, int oversample, int iterations, ILog log, SchematicReader reader, BenchmarkRunner runner)
        {

            var schematic = reader.GetSchematic(filename);
            var circuit = schematic.Build();

            (int[] permutation, double score)[] ranking =
            {
                (Enumerable.Range(0, circuit.Components.Count).ToArray(), 0),
                (Enumerable.Range(0, circuit.Components.Count).ToArray(), 0)
            };

            while (true)
            {

                var candidates = ranking
                    .Select(i => i.permutation)
                    .Concat(Enumerable.Range(1, 3).Select(i => ranking[0].permutation.Crossover(ranking[1].permutation).Permutate(i)))
                    .Append(Enumerable.Range(0, circuit.Components.Count).ToArray().Permutate(circuit.Components.Count));

                var results = candidates.Select(permutation => (permutation, score: runner.Benchmark(circuit, t => FunctionGenerator.Harmonics(t, .5, 82, 2), sampleRate, oversample, iterations, permutation, optimize: true)));

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
                                var symbols = elements.OfType<Circuit.Symbol>().ToArray();
                                var other = elements.Except(symbols);
                                schematic.Elements.Clear();
                                schematic.Elements.AddRange(best.Select(i => symbols[i]).Concat(other));
                                schematic.Save($"./{circuit.Name}_optimized.schx");
                                break;
                            }
                        default:
                            break;
                    }
                }
            }
        }
    }
}
