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

        static Quantity SampleRate = new Quantity(48000, Units.Hz);
        static int Samples = 48000;
        static int Oversample = 4;
        static int Iterations = 8;

        static ConsoleLog Log = new ConsoleLog(MessageType.Info);

        static Expression V1 = Harmonics(t, 0.5, 82, 4);
        
        static void Main(string[] args)
        {
            Func<double, double> Vin = ExprFunction.New(V1, Component.t).Compile<Func<double, double>>();

            List<string> errors = new List<string>();
            List<string> performance = new List<string>();
            
            foreach (string File in System.IO.Directory.EnumerateFiles(@".", "*.xml"))
            {
                string Name = System.IO.Path.GetFileNameWithoutExtension(File);
                try
                {
                    double p = Run(File, Vin);
                    performance.Add(Name + ":\t" + p.ToString());
                }
                catch (Exception ex) 
                {
                    errors.Add(Name + ":\t" + ex.Message);
                    System.Console.WriteLine(ex.Message);
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
            Circuit.Circuit C = Schematic.Load(FileName, Log).Build();
            TransientSolution TS = TransientSolution.SolveCircuit(C, 1 / (SampleRate * Oversample), Log);
            Simulation S = new LinqCompiledSimulation(TS, Oversample, Log);
            System.Console.WriteLine("");

            return RunTest(
                C, S, 
                TS.Parameters.ToDictionary(i => i.Name, i => i.Default), 
                Vin, 
                Samples, 
                System.IO.Path.GetFileNameWithoutExtension(FileName));
        }

        public static double RunTest(Circuit.Circuit C, Simulation S, IEnumerable<KeyValuePair<Expression, double>> Arguments, Func<double, double> Vin, int N, string Name)
        {            
            double t0 = (double)S.Time;
            
            Dictionary<Expression, double[]> input = new Dictionary<Expression, double[]>();
            double[] vs = new double[N];
            for (int n = 0; n < vs.Length; ++n)
                vs[n] = Vin(n * S.TimeStep);
            input.Add("V[t]", vs);

            Dictionary<Expression, double[]> output = S.Solution.Nodes.ToDictionary(i => i, i => new double[vs.Length]);
            
            // Ensure that the simulation is cached before benchmarking.
            S.Run(1, input, output, Arguments, Iterations);
            S.Reset();

            double time = 0.0;
            try
            {
                long a = Timer.Counter;
                S.Run(vs.Length, input, output, Arguments, Iterations);
                time = (double)(Timer.Counter - a) / Timer.Frequency;
            }
            catch (SimulationDiverged Ex)
            {
                if (Ex.At < 10) throw;
                N = (int)Ex.At;
            }

            int t1 = Math.Min(N, 2000);

            Dictionary<Expression, List<Arrow>> plots = new Dictionary<Expression, List<Arrow>>();
            foreach (KeyValuePair<Expression, double[]> i in input.Concat(output))
                plots.Add(i.Key, i.Value.Take(t1).Select((j, n) => Arrow.New(n * S.TimeStep, j)).ToList());

            double max = 0.0;
            for (int i = 0; i < vs.Length; ++i)
                max = Math.Max(max, Math.Abs(vs[i] - output.First().Value[i]));
            System.Console.WriteLine("Max error {0}", max);

            IEnumerable<double[]> series = input.Concat(output).Select(i => i.Value);
            Plot p = new Plot(
                Name, 
                800, 400, 
                t0, 0, 
                S.TimeStep * t1, 0, 
                plots.ToDictionary(i => i.Key.ToString(), i => (Plot.Series)new Plot.Scatter(i.Value)));

            double rate = (N * S.TimeStep) / time;
            System.Console.WriteLine("Ran at {0}x real time", rate);
            return rate;
        }

        // Generate a function with the first N harmonics of f0.
        static Expression Harmonics(Variable t, Expression A, Expression f0, int N)
        {
            Expression s = 0;
            for (int i = 1; i <= N; ++i)
                s += Call.Sin(t * f0 * 2 * 3.1415m * i) / N;
            return A * s;
        }
    }
}
