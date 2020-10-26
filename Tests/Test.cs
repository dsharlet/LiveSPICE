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

        private double analysisTime = 0.0;
        private double simulateTime = 0.0;

        public Log Log = new ConsoleLog() { Verbosity = MessageType.Info };

        public void Run(IEnumerable<string> Tests, Func<double, double> Vin)
        {
            List<string> errors = new List<string>();
            List<string> performance = new List<string>();

            // This test generates the signal for the LiveSPICE 'logo'.
            //Run("Subcircuit Trivial.schx", Vin, "V1[t]", new Expression[] { "_v15[t]", "_v11[t]" });
            //return;

            foreach (string File in Tests)
            {
                string Name = Path.GetFileNameWithoutExtension(File);
                try
                {
                    double perf = Run(File, Vin);
                    performance.Add(Name + ":\t" + Quantity.ToString(perf, Units.Hz) + " (" + (perf / (double)SampleRate).ToString("G3") + "x real time)");
                }
                catch (Exception ex)
                {
                    errors.Add(Name + ":\t" + ex.Message);
                    Log.WriteLine(ex.Message);
                }
            }

            Log.WriteLine("Analyze/Simulate {0}/{1}", analysisTime, simulateTime);

            Log.WriteLine("{0} succeeded:", performance.Count);
            foreach (string i in performance)
                Log.WriteLine(i);

            Log.WriteLine("{0} failed:", errors.Count);
            foreach (string i in errors)
                Log.WriteLine(i);
        }

        public double Run(string FileName, Func<double, double> Vin)
        {
            Circuit.Circuit C = Schematic.Load(FileName, Log).Build();
            C.Name = Path.GetFileNameWithoutExtension(FileName);
            Expression input = C.Components.OfType<Input>().Select(i => Expression.Parse(i.Name + "[t]")).DefaultIfEmpty("V[t]").SingleOrDefault();
            IEnumerable<Expression> outputs = C.Nodes.Select(i => i.V);
            return Run(C, Vin, input, outputs);
        }

        public double Run(Circuit.Circuit C, Func<double, double> Vin, Expression Input, IEnumerable<Expression> Plots)
        {
            long a = Timer.Counter;

            Analysis analysis = C.Analyze();
            TransientSolution TS = TransientSolution.Solve(analysis, (Real)1 / (SampleRate * Oversample), Log);

            analysisTime += Timer.Delta(a);

            Simulation S = new Simulation(TS)
            {
                Oversample = Oversample,
                Iterations = Iterations,
                Log = Log,
                Input = new[] { Input },
                Output = Plots,
            };

            Log.WriteLine("");
            if (Samples > 0)
                return RunTest(S, Vin, Samples, C.Name);
            else
                return 0.0;
        }

        public double RunTest(Simulation S, Func<double, double> Vin, int Samples, string Name)
        {
            double t0 = (double)S.Time;
            double T = S.TimeStep;

            List<List<double>> output = S.Output.Select(i => new List<double>(Samples)).ToList();

            double time = 0.0;
            int samples = 0;
            double t = 0;
            Random rng = new Random();
            while (samples < Samples)
            {
                int N = Math.Min(Samples - samples, rng.Next(1000, 10000));
                double[] input = new double[N];
                List<double[]> buffers = S.Output.Select(i => new double[N]).ToList();
                for (int n = 0; n < N; ++n, t += T)
                    input[n] = Vin(t);

                long a = Timer.Counter;
                S.Run(input, buffers);
                time += Timer.Delta(a);

                for (int i = 0; i < S.Output.Count(); ++i)
                    output[i].AddRange(buffers[i]);

                samples += N;
            }
            simulateTime += time;

            int t1 = Math.Min(samples, 4000);

            Log.WriteLine("Performance {0}", Quantity.ToString(samples / time, Units.Hz));

            Plot p = new Plot()
            {
                Title = Name,
                Width = 800,
                Height = 400,
                x0 = t0,
                x1 = T * t1,
                xLabel = "Time (s)",
                yLabel = "Voltage (V)",
            };

            p.Series.AddRange(output.Select((i, j) => new Scatter(
                i.Take(t1)
                .Select((k, n) => new KeyValuePair<double, double>(n * T, k)).ToArray())
            { Name = S.Output.ElementAt(j).ToString() }));
            return samples / time;
        }
    }
}