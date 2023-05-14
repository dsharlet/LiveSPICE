using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using Circuit;
using ComputerAlgebra;
using LiveSPICE.Cli;
using LiveSPICE.Cli.Utils;
using LiveSPICE.CLI.Utils;
using Tests;
using Tests.Genetic;
using Util;
using Util.Cancellation;

namespace LiveSPICE.CLI.Commands
{
    internal class BenchmarkCommand : Command
    {
        public BenchmarkCommand() : base("benchmark", "Benchmark a circuit simulation.")
        {
            var pattern = new Argument<string>("pattern", "\"Glob pattern for files to benchmark");
            AddArgument(pattern);

            var legacy = new Option<bool>("--legacy", "Use legacy simulation process.");
            AddOption(legacy);

            var dynamic = new Option<bool>("--dynamic", () => true, "Enable dynamic components.");
            AddOption(dynamic);

            AddOption(CommonOptions.Amplitude);
            AddOption(CommonOptions.SampleRate);
            AddOption(CommonOptions.Oversample);
            AddOption(CommonOptions.Iterations);

            this.SetHandler(
                RunBenchmark,
                pattern, 
                CommonOptions.SampleRate, 
                CommonOptions.Oversample,
                CommonOptions.Iterations,
                legacy,
                dynamic,
                CommonOptions.Amplitude,
                Bind.FromServiceProvider<ILog>(),
                Bind.FromServiceProvider<SchematicReader>(),
                Bind.FromServiceProvider<BenchmarkRunner>());
        }

        private void RunBenchmark(
            string pattern,
            int sampleRate,
            int oversample,
            int iterations,
            bool legacy,
            bool dynamic,
            Quantity amplitude,
            ILog log,
            SchematicReader reader,
            BenchmarkRunner runner)
        {
            foreach (var circuit in reader.GetSchematics(pattern).Select(s => s.Build(log)))
            {
                if (legacy) { log.WriteLine(MessageType.Info, "[darkyellow]Legacy simulation process[/darkyellow]"); }

                if (!dynamic)
                {
                    foreach (var pot in circuit.Components.OfType<Potentiometer>())
                    {
                        pot.Dynamic = false;
                    }
                }

                var (analyzeTime, solveTime, buildTime, simRate) = runner.Benchmark(circuit, t => FunctionGenerator.Harmonics(t, 0.5, 82, 2), sampleRate, oversample, iterations);

                string name = circuit.Name;
                log.Info($"{name,-40}{analyzeTime,12}{solveTime,12}{buildTime,12}{simRate,12:G4}");
            }
        }
    }
}
