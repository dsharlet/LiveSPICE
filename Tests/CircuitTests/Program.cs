using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using SyMath;
using Circuit;
using LinqExpressions = System.Linq.Expressions;
using LinqExpression = System.Linq.Expressions.Expression;

// Filter design tool: http://sim.okawa-denshi.jp/en/CRtool.php

namespace CircuitTests
{
    class Program
    {
        static readonly Variable t = Component.t;

        static ConsoleLog Log = new ConsoleLog(MessageType.Info);

        // Generate a function with the first N harmonics of f0.
        static Function Harmonics(Variable t, Expression f0, int N)
        {
            Expression s = 0;
            for (int i = 1; i <= N; ++i)
                s += Call.Sin(t * f0 * 2 * 3.1415m * i) / N;
            return ExprFunction.New(s, t);
        }

        static void Main(string[] args)
        {
            Func<double, double> Vin = Harmonics(t, 82, 4).Compile<Func<double, double>>();

            List<string> errors = new List<string>();
            List<string> performance = new List<string>();

            //Run(@"..\..\..\..\Circuits\SeriesDiodeClipper.xml", Vin);
            //return;
            
            foreach (string File in System.IO.Directory.EnumerateFiles(@"..\..\..\..\Circuits\"))
            {
                try
                {
                    double p = Run(File, Vin);
                    performance.Add(File + ": " + p.ToString());
                }
                catch (Exception Ex) 
                {
                    errors.Add(File);
                    System.Console.WriteLine(Ex.ToString());
                }
            }

            System.Console.WriteLine("{0} succeeded:", performance.Count);
            foreach (string i in performance)
                System.Console.WriteLine(i);

            System.Console.WriteLine("{0} failed:", errors.Count);
            foreach (string i in errors)
                System.Console.WriteLine(i);
        }
                
        public static double Run(string FileName, Func<double, double> Vin)
        {
            Circuit.Circuit C = Schematic.Load(FileName, Log).Circuit;
            Simulation S = new Simulation(C, new Quantity(48000, Units.Hz), 4, 4, Log);
            System.Console.WriteLine("");

            return RunTest(S, Vin, 48000 * 10, System.IO.Path.GetFileNameWithoutExtension(FileName));
        }

        public static double RunTest(Simulation S, Func<double, double> Vin, int N, string Name)
        {            
            double t0 = (double)S.Time;
            
            Dictionary<Expression, double[]> input = new Dictionary<Expression, double[]>();
            double[] vs = new double[N];
            for (int n = 0; n < vs.Length; ++n)
                vs[n] = Vin(n * S.TimeStep);
            input.Add("V1[t]", vs);

            Dictionary<Expression, double[]> output = S.Nodes.ToDictionary(i => i, i => new double[vs.Length]);
            //Dictionary<Expression, double[]> output = new Expression[] { "Vo[t]" }.ToDictionary(i => i, i => new double[vs.Length]);
            
            // Ensure that the simulation is compiled before benchmarking.
            S.Process(1, input, output);
            S.Reset();

            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            S.Process(vs.Length, input, output);
            timer.Stop();

            int t1 = 5000;

            //output.RemoveAll(i => i.Value.Contains(double.NaN) || i.Value.Contains(double.NegativeInfinity) || i.Value.Contains(double.PositiveInfinity));

            Dictionary<Expression, List<Arrow>> plots = new Dictionary<Expression, List<Arrow>>();
            foreach (KeyValuePair<Expression, double[]> i in input.Concat(output))
                plots.Add(i.Key, i.Value.Take(t1).Select((j, n) => Arrow.New(n * S.TimeStep, j)).ToList());

            IEnumerable<double[]> series = input.Concat(output).Select(i => i.Value);
            Plot p = new Plot(
                Name, 
                800, 400, 
                t0, series.Min(i => i.Min()) * 1.25 - 0.1, 
                S.TimeStep * t1, series.Max(i => i.Max()) * 1.25 + 0.1, 
                plots.ToDictionary(i => i.Key.ToString(), i => (Plot.Series)new Plot.Scatter(i.Value)));

            return (N * S.TimeStep) / ((double)timer.ElapsedMilliseconds / 1000.0);
        }
    }
}
