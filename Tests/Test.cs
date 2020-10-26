using Circuit;
using ComputerAlgebra;
using ComputerAlgebra.Plotting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Util;

namespace Tests
{
    internal class Test
    {
        private static readonly Variable t = Component.t;

        public int SampleRate = 44100;
        public int Samples = 100000;
        public int Oversample = 8;
        public int Iterations = 8;

        public Log Log = new ConsoleLog() { Verbosity = MessageType.Info };

        public void Run(IEnumerable<string> Tests, Func<double, double> Vin)
        {
            // This test generates the signal for the LiveSPICE 'logo'.
            //Run("Subcircuit Trivial.schx", Vin, "V1[t]", new Expression[] { "_v15[t]", "_v11[t]" });
            //return;

            foreach (string File in Tests)
                Run(File, Vin);
        }

        public void Run(string FileName, Func<double, double> Vin)
        {
            Circuit.Circuit C = Schematic.Load(FileName, Log).Build();
            C.Name = Path.GetFileNameWithoutExtension(FileName);
            Expression input = C.Components.OfType<Input>().Select(i => Expression.Parse(i.Name + "[t]")).DefaultIfEmpty("V[t]").SingleOrDefault();
            IEnumerable<Expression> outputs = C.Nodes.Select(i => i.V);
            Run(C, Vin, input, outputs);
        }

        public void Run(Circuit.Circuit C, Func<double, double> Vin, Expression Input, IEnumerable<Expression> Plots)
        {
            Analysis analysis = C.Analyze();
            TransientSolution TS = TransientSolution.Solve(analysis, (Real)1 / (SampleRate * Oversample), Log);

            Simulation S = new Simulation(TS)
            {
                Oversample = Oversample,
                Iterations = Iterations,
                Log = Log,
                Input = new[] { Input },
                Output = Plots,
            };

            if (Samples > 0)
                RunTest(S, Vin, Samples, C.Name);
        }

        public void RunTest(Simulation S, Func<double, double> Vin, int Samples, string Name)
        {
            double T = S.TimeStep;
            int t0 = 0;
            int t1 = SampleRate / 10;

            List<List<double>> output = S.Output.Select(i => new List<double>(Samples)).ToList();

            double t = 0;
            Random rng = new Random();
            while (Samples > 0)
            {
                int N = Math.Min(Samples, rng.Next(1000, 10000));
                double[] input = new double[N];
                List<double[]> buffers = S.Output.Select(i => new double[N]).ToList();
                for (int n = 0; n < N; ++n, t += T)
                    input[n] = Vin(t);

                S.Run(input, buffers);

                for (int i = 0; i < S.Output.Count(); ++i)
                    output[i].AddRange(buffers[i]);

                Samples -= N;
            }

            Plot p = new Plot()
            {
                Title = Name,
                Width = 800,
                Height = 400,
                x0 = T * t0,
                x1 = T * t1,
                xLabel = "Time (s)",
                yLabel = "Voltage (V)",
            };

            p.Series.AddRange(output.Select((i, j) => new Scatter(
                i.Take(t1)
                .Select((k, n) => new KeyValuePair<double, double>(n * T, k)).ToArray())
            { Name = S.Output.ElementAt(j).ToString() }));
        }
    }
}