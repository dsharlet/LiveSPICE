using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using Util;

namespace Circuit
{
    public class CircuitSimulation
    {
        public Schematic Schematic { get; private set; }
        public Circuit Circuit { get; private set; }
        public Analysis Analysis { get; private set; }
        public Simulation Simulation { get; private set; } = null;
        public ILog Log { get; private set; }
        public int Oversample { get; set; } = 8;
        public int Iterations { get; set; } = 8;
        public double SampleRate { get; set; } = 48000;
        public bool HaveSimulation { get { return Simulation != null; } }

        Dictionary<ComputerAlgebra.Expression, int> argumentLookup = new Dictionary<ComputerAlgebra.Expression, int>();
        double[] arguments;

        public CircuitSimulation(Schematic schematic, ILog log)
        {
            Schematic = schematic;
            Log = log;

            Circuit = Schematic.Build(Log);
            Analysis = Circuit.Analyze();

            arguments = new double[Analysis.Parameters.Count()];

            int pos = 0;

            foreach (Analysis.Parameter P in Analysis.Parameters)
            {
                arguments[pos] = P.Value;
                argumentLookup[P.Expression] = pos++;
            }
        }

        public void SetParameter(ComputerAlgebra.Expression expression, double value)
        {
            arguments[argumentLookup[expression]] = value;
        }

        public void UpdateSimulation(IEnumerable<ComputerAlgebra.Expression> inputs, IEnumerable<ComputerAlgebra.Expression> outputs)
        {
            ComputerAlgebra.Expression h = (ComputerAlgebra.Expression)1 / (SampleRate * Oversample);
            TransientSolution solution = TransientSolution.Solve(Circuit.Analyze(), h, Log);

            var newSimulation = new Simulation(solution)
            {
                Log = Log,
                Input = inputs,
                Output = outputs,
                Arguments = Analysis.Parameters.Select(p => p.Expression),
                Oversample = Oversample,
                Iterations = Iterations,
            };

            Simulation = newSimulation;
        }

        public void RunSimulation(int numSamples, IEnumerable<double[]> audioInputs, IEnumerable<double[]> audioOutputs)
        {
            Simulation.Run(numSamples, audioInputs, audioOutputs, arguments);
        }
    }
}
