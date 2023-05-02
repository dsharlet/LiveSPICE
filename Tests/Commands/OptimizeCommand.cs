using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Joins;
using System.Text;
using System.Threading.Tasks;
using LiveSPICE.Cli;
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

            var populationSize = new Option<int>("--populationSize", () => 6, "Population size");
            AddOption(populationSize);

            AddOption(CommonOptions.SampleRate);
            AddOption(CommonOptions.Oversample);
            AddOption(CommonOptions.Iterations);

            this.SetHandler(
                RunOptimize,
                filename,
                populationSize,
                CommonOptions.SampleRate,
                CommonOptions.Oversample,
                CommonOptions.Iterations,
                Bind.FromServiceProvider<ILog>(),
                Bind.FromServiceProvider<SchematicReader>(),
                Bind.FromServiceProvider<BenchmarkRunner>());
        }

        private static void RunOptimize(string filename,
                                        int populationSize,
                                        int sampleRate,
                                        int oversample,
                                        int iterations,
                                        ILog log,
                                        SchematicReader reader,
                                        BenchmarkRunner runner)
        {

            var schematic = reader.GetSchematic(filename);
            var circuit = schematic.Build();

            circuit.Name = Path.GetFileNameWithoutExtension(filename);


            (int[] permutation, double fitness)[] selection =
            {
                (Enumerable.Range(0, circuit.Components.Count).ToArray(), 0),
                (Enumerable.Range(0, circuit.Components.Count).ToArray(), 0)
            };

            while (true)
            {

                var population = selection
                    .Select(i => i.permutation)
                    .Concat(Enumerable.Range(1, populationSize - 3).Select(i => selection[0].permutation.Crossover(selection[1].permutation).Permutate(i)))
                    .Append(Enumerable.Range(0, circuit.Components.Count).ToArray().Permutate(circuit.Components.Count));

                var evaluated = population.Select(permutation => (permutation, fitness: runner.Benchmark(circuit, t => FunctionGenerator.Harmonics(t, .5, 82, 2), sampleRate, oversample, iterations, permutation: permutation, optimize: true)));

                selection = evaluated.OrderByDescending(r => r.fitness).Take(2).ToArray();
                Console.WriteLine($"[green]Best: {selection[0].fitness}x[/green]");

                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    switch (key.Key)
                    {
                        case ConsoleKey.X:
                            {
                                var best = selection[0].permutation;
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
