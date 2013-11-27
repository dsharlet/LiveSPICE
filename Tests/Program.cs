using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using ComputerAlgebra;
using ComputerAlgebra.LinqCompiler;
using ComputerAlgebra.Plotting;
using Circuit;
using Util;

// Filter design tool: http://sim.okawa-denshi.jp/en/CRtool.php

namespace Tests
{
    class Program
    {
        static readonly Variable t = Component.t;

        static Quantity SampleRate = new Quantity(44100, Units.Hz);
        static int Samples = 100000;
        static int Oversample = 8;
        static int Iterations = 8;

        static double analysisTime = 0.0;
        static double simulateTime = 0.0;

        static ConsoleLog Log = new ConsoleLog() { Verbosity = MessageType.Info };

        static void Main(string[] args)
        {
            Func<double, double> Vin = t => Harmonics(t, 0.5, 82, 2);

            List<string> errors = new List<string>();
            List<string> performance = new List<string>();

            // This test generates the signal for the LiveSPICE 'logo'.
            //Run("BossSD1NoBuffer.xml", Vin, "V1[t]", new Expression[] { "_v15[t]", "_v11[t]" });
            //return;
            
            foreach (string File in System.IO.Directory.EnumerateFiles(@".", "*.schx"))
            {
                string Name = System.IO.Path.GetFileNameWithoutExtension(File);
                try
                {
                    double perf = Run(File, Vin);
                    performance.Add(Name + ":\t" + Quantity.ToString(perf, Units.Hz) + " (" + (perf / (double)SampleRate).ToString("G3") + "x real time)");
                }
                catch (Exception ex) 
                {
                    errors.Add(Name + ":\t" + ex.Message);
                    Console.WriteLine(ex.Message);
                }
            }

            Console.WriteLine("Analyze/Simulate {0}/{1}", analysisTime, simulateTime);

            Console.WriteLine("{0} succeeded:", performance.Count);
            foreach (string i in performance)
                Console.WriteLine(i);

            Console.WriteLine("{0} failed:", errors.Count);
            foreach (string i in errors)
                Console.WriteLine(i);
        }

        public static double Run(string FileName, Func<double, double> Vin)
        {
            Circuit.Circuit C = Schematic.Load(FileName, Log).Build();
            C.Name = System.IO.Path.GetFileNameWithoutExtension(FileName);
            return Run(
                C, 
                Vin, 
                C.Components.OfType<Input>().Select(i => Expression.Parse(i.Name + "[t]")).DefaultIfEmpty("V[t]").SingleOrDefault(), 
                C.Nodes.Select(i => i.V));
        }

        public static double Run(string FileName, Func<double, double> Vin, Expression Input, IEnumerable<Expression> Plots)
        {
            Circuit.Circuit C = Schematic.Load(FileName, Log).Build();
            C.Name = System.IO.Path.GetFileNameWithoutExtension(FileName);
            return Run(C, Vin, Input, Plots);
        }

        public static double Run(Circuit.Circuit C, Func<double, double> Vin, Expression Input, IEnumerable<Expression> Plots)
        {
            long a = Timer.Counter;

            Analysis analysis = C.Analyze();
            TransientSolution TS = TransientSolution.Solve(analysis, 1 / (SampleRate * Oversample), Log);

            analysisTime += Timer.Delta(a);
            
            Simulation S = new LinqCompiledSimulation(TS, Oversample, Log);
            Console.WriteLine("");
            if (Samples > 0)
                return RunTest(
                    C, S,
                    Input,
                    Plots,
                    Vin,
                    Samples,
                    C.Name);
            else
                return 0.0;
        }

        public static double RunTest(Circuit.Circuit C, Simulation S, Expression Input, IEnumerable<Expression> Outputs, Func<double, double> Vin, int N, string Name)
        {            
            double t0 = (double)S.Time;
            
            Dictionary<Expression, double[]> input = new Dictionary<Expression, double[]>();
            double[] vs = new double[N];
            for (int n = 0; n < vs.Length; ++n)
                vs[n] = Vin(n * S.TimeStep);
            input.Add(Input, vs);

            Dictionary<Expression, double[]> output = Outputs.ToDictionary(i => i, i => new double[vs.Length]);
            
            // Ensure that the simulation is cached before benchmarking.
            S.Run(1, input, output, Iterations);
            S.Reset();

            double time = 0.0;
            try
            {
                long a = Timer.Counter;
                S.Run(vs.Length, input, output, Iterations);
                time = Timer.Delta(a);
                simulateTime += time;
            }
            catch (SimulationDiverged Ex)
            {
                if (Ex.At < 10) throw;
                N = (int)Ex.At;
            }

            int t1 = Math.Min(N, 2000);

            Console.WriteLine("Performance {0}", Quantity.ToString(N / time, Units.Hz));

            Plot p = new Plot()
            {
                Title = Name, 
                Width = 800, Height = 400, 
                x0 = t0,
                x1 = S.TimeStep * t1,
                xLabel = "Time (s)",
                yLabel = "Voltage (V)",
            };

            p.Series.AddRange(output.Select(i => new Scatter(
                i.Value.Take(t1)
                .Select((j, n) => new KeyValuePair<double, double>(n * S.TimeStep, j)).ToArray()) { Name = i.Key.ToString() }));
            return N / time;
        }

        // Generate a function with the first N harmonics of f0.
        static double Harmonics(double t, double A, double f0, int N)
        {
            double s = 0;
            for (int i = 1; i <= N; ++i)
                s += Math.Sin(t * f0 * 2 * 3.1415 * i) / N;
            return A * s;
        }
    }
}
