using ComputerAlgebra;
using ComputerAlgebra.LinqCompiler;
using ExpressionTreeToString;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using ComputerAlgebra;
using Util;
using ZSpitz.Util;

namespace Circuit
{

    /// <summary>
    /// Simulate a circuit.
    /// </summary>
    public class Simulation
    {
        private Action<int, double, double[][], double[][], double[], double[]> _process;
        private readonly SimulationSettings settings;
        private readonly SortedReadonlyDictionary<Expression, double> _state;
        private long n = 0;

        /// <summary>
        /// Get which sample the simulation is at.
        /// </summary>
        public long At { get { return n; } }

        /// <summary>
        /// Get the simulation time.
        /// </summary>
        public double Time { get { return At * TimeStep; } }

        /// <summary>
        /// Get the timestep for the simulation.
        /// </summary>
        public double TimeStep => (double)settings.TimeStep;

        /// <summary>
        /// The sampling rate of this simulation, the sampling rate of the transient solution divided by the oversampling factor.
        /// </summary>
        public Expression SampleRate => settings.SampleRate;

        /// <summary>
        /// Stores any global state in the simulation (previous state values, mostly).
        /// </summary>
        public IReadOnlyDictionary<Expression, double> State => _state;

        /// <summary>
        /// Parameters
        /// </summary>
        public IEnumerable<Analysis.Parameter> Parameters { get; }

        /// <summary>
        /// Create a simulation using the given solution and the specified inputs/outputs.
        /// </summary>
        /// <param name="Solution">Transient solution to run.</param>
        /// <param name="Input">Expressions in the solution to be defined by input samples.</param>
        /// <param name="Output">Expressions describing outputs to be saved from the simulation.</param>
        public Simulation(Action<int, double, double[][], double[][], double[], double[]> process, IReadOnlyDictionary<Expression, double> state, SimulationSettings settings, IEnumerable<Analysis.Parameter> parameters)
        {
            _process = process;
            this._state = new SortedReadonlyDictionary<Expression, double>(state);
            this.settings = settings;
            Parameters = parameters;

            //Observable.CombineLatest(inputSubject, outputSubject, oversampleSubject, iterationsSubject, (input, output, oversample, iterations) => (input, output, oversample, iterations))
            //    .Select(a => Compile(a.input, a.output, a.oversample, a.iterations))
            //    .Switch()
            //    .Subscribe(newProcess => _process = newProcess);
        }

        /// <summary>
        /// Process some samples with this simulation. The Input and Output buffers must match the enumerations provided
        /// at initialization.
        /// </summary>
        /// <param name="N">Number of samples to process.</param>
        /// <param name="Input">Buffers that describe the input samples.</param>
        /// <param name="Output">Buffers to receive output samples.</param>
        public void Run(int N, IEnumerable<double[]> Input, IEnumerable<double[]> Output)
        {
            try
            {
                var parameters = Parameters.Select(p => p.Value).ToArray();
                _process(N, n * TimeStep, Input.AsArray(), Output.AsArray(), parameters, _state.RawData);
                n += N;
            }
            catch (SimulationDivergedException Ex)
            {
                throw new SimulationDivergedException("Simulation diverged near t = " + Quantity.ToString(Time, Units.s) + " + " + Ex.At, n + Ex.At);
            }
        }
        public void Run(int N, IEnumerable<double[]> Output) { Run(N, new double[][] { }, Output); }
        public void Run(double[] Input, IEnumerable<double[]> Output) { Run(Input.Length, new[] { Input }, Output); }
        public void Run(double[] Input, double[] Output) { Run(Input.Length, new[] { Input }, new[] { Output }); }

        /// <summary>
        /// A human readable implementation of RowReduce.
        /// </summary>
        /// <param name="Ab"></param>
        /// <param name="M"></param>
        /// <param name="N"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void RowReduce(double[][] Ab, int M, int N)
        {
            // Solve for dx.
            // For each variable in the system...
            for (int j = 0; j + 1 < N; ++j)
            {
                int pi = j;
                double max = Math.Abs(Ab[j][j]);

                // Find a pivot row for this variable.
                for (int i = j + 1; i < M; ++i)
                {
                    double[] Abi = Ab[i];
                    // if(|JxF[i][j]| > max) { pi = i, max = |JxF[i][j]| }
                    double maxj = Math.Abs(Abi[j]);
                    if (maxj > max)
                    {
                        pi = i;
                        max = maxj;
                    }
                }

                // Swap pivot row with the current row.
                if (pi != j)
                {
                    var Abpi = Ab[pi];
                    Ab[pi] = Ab[j];
                    Ab[j] = Abpi;
                }

                double[] Abj = Ab[j];

                // Eliminate the rows after the pivot.
                double p = Abj[j];
                for (int i = j + 1; i < M; ++i)
                {
                    double[] Abi = Ab[i];
                    double s = Abi[j] / p;
                    if (s != 0.0)
                        for (int ij = j + 1; ij <= N; ++ij)
                            Abi[ij] -= Abj[ij] * s;
                }
            }
        }

        /// <summary>
        /// This algorith has no tail-loop - it requires arrays to be padded to N + Vector.Count - 1
        /// </summary>
        /// <param name="Ab"></param>
        /// <param name="M"></param>
        /// <param name="N"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void RowReduceVector(double[][] Ab, int M, int N)
        {
            // Solve for dx.
            // For each variable in the system...
            for (int j = 0; j + 1 < N; ++j)
            {
                int pi = j;
                double max = Math.Abs(Ab[j][j]);

                // Find a pivot row for this variable.
                for (int i = j + 1; i < M; ++i)
                {
                    // if(|JxF[i][j]| > max) { pi = i, max = |JxF[i][j]| }
                    double maxj = Math.Abs(Ab[i][j]);
                    if (maxj > max)
                    {
                        pi = i;
                        max = maxj;
                    }
                }

                // Swap pivot row with the current row.
                if (pi != j)
                {
                    var tmp = Ab[pi];
                    Ab[pi] = Ab[j];
                    Ab[j] = tmp;
                }

                var vectorLength = Vector<double>.Count;
                // Eliminate the rows after the pivot.
                double p = Ab[j][j];
                for (int i = j + 1; i < M; ++i)
                {
                    double s = Ab[i][j] / p;
                    if (s != 0.0)
                    {
                        int jj;
                        for (jj = j + 1; jj <= N; jj += vectorLength)
                        {
                            var source = new Vector<double>(Ab[j], jj);
                            var target = new Vector<double>(Ab[i], jj);
                            var res = target - (source * s);
                            res.CopyTo(Ab[i], jj);
                        }
                    }
                }
            }
        }


    }
}
