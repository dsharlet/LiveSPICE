using System;
using System.IO;
using Circuit;
using ComputerAlgebra;
using System.Collections.Generic;
using Util;
using System.Linq;

namespace Tests
{
    class Program
    {
        // TODO: Make these command line arguments.
        static int SampleRate = 48000;
        static int Samples = 4800;
        static int Oversample = 8;
        static int Iterations = 8;

        static void Main(string[] args)
        {
            bool test = args.Contains("--test");
            bool benchmark = args.Contains("--benchmark");
            bool plot = args.Contains("--plot");

            Log log = new ConsoleLog() { Verbosity = MessageType.Info };
            Test tester = new Test();

            foreach (string i in args.Where(i => !i.StartsWith("--")))
            {
                foreach (string File in Globber.Glob(i))
                {
                    System.Console.WriteLine(File);
                    Circuit.Circuit C = Schematic.Load(File, log).Build();
                    C.Name = Path.GetFileNameWithoutExtension(File);
                    if (test)
                    {
                        Dictionary<Expression, List<double>> outputs =
                            tester.Run(C, t => Harmonics(t, 0.5, 82, 2), SampleRate, Samples, Oversample, Iterations);
                        if (plot)
                            tester.PlotAll(C.Name, outputs);
                    }
                    if (benchmark)
                        tester.Benchmark(C, t => Harmonics(t, 0.5, 82, 2), SampleRate, Oversample, Iterations);
                    System.Console.WriteLine("");
                }
            }
        }

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
