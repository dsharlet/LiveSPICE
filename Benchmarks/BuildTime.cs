using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using Circuit;
using ComputerAlgebra;
using Util;
using Util.Cancellation;

namespace Benchmarks
{
    [EventPipeProfiler(EventPipeProfile.CpuSampling)]
    public class BuildTime
    {
        private Analysis _analysis;
        private Expression _input;
        private Expression[] _outputs;
        private readonly Expression _h = (Real)1 / 48000;

        [Params(true, false)]
        public bool Dynamic { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            var schematic = Schematic.Load(@".\Schematics\Marshall JCM800 2203 preamp.schx");
            var circuit = schematic.Build();

            if (!Dynamic)
            {
                foreach (var pot in circuit.Components.OfType<Potentiometer>())
                {
                    pot.Dynamic = false;
                }
            }

            _analysis = circuit.Analyze();

            _input =  circuit.Components.OfType<Input>()
                .Select(i => i.In)
                .DefaultIfEmpty("V[t]")
                .Single();

            Expression sum = 0;
            foreach (Speaker i in circuit.Components.OfType<Speaker>())
                sum += i.Out;
            _outputs = new[] { sum };

        }

        [Benchmark]
        public LegacySimulation Build_LegacySimulation()
        {
            var solution =  TransientSolution.Solve(_analysis, _h);

            var simulation = new LegacySimulation(solution) 
            { 
                Oversample = 1,
                Iterations = 32,
                Input = new[] {_input},
                Output = _outputs,
            };
            simulation.DefineProcess();
            return simulation;
        }

        [Benchmark]
        public Simulation Build_NewtonSimulationBuilder()
        {
            var builder = new NewtonSimulationBuilder(new NullLog());

            var settings = new NewtonSimulationSettings(48000, 1, 32, true);

            return builder.Build(_analysis, settings, new[] { _input }, _outputs, CancellationStrategy.None);
        }
    }
}
