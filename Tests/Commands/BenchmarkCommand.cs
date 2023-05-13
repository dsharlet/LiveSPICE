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
            string fmt = "{0,-40}{1,12:G4}{2,12:G4}{3,12:G4}{4,12:G4}";

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

                runner.Benchmark(circuit, t => FunctionGenerator.Harmonics(t, .5, 82d, 2), sampleRate, oversample, iterations);
                //TODO: fix
                double[] result = runner.Benchmark(circuit, t => Harmonics(t, 0.5, 82, 2), sampleRate, oversample, iterations);
                double analyzeTime = result[0];
                double solveTime = result[1];
                double simRate = result[2];
                string name = circuit.Name;
                if (name.Length > 39)
                    name = name.Substring(0, 39);
                System.Console.WriteLine(fmt, name, analyzeTime * 1000, solveTime * 1000, simRate / 1000, simRate / sampleRate);
            }
        }
    }
}
