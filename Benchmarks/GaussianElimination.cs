using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using Circuit;
using MathNet.Numerics.LinearAlgebra;
using System;
using System.Linq;

namespace Benchmarks
{
    [MemoryDiagnoser]
    [HardwareCounters(
        HardwareCounter.BranchMispredictions,
        HardwareCounter.BranchInstructions,
        HardwareCounter.CacheMisses)]
    public class GaussianElimination
    {
        [Params(12)]
        public int M { get; set; }
        public int N => M;
        public int Size => 100000;

        private const int seed = 12345;


        private (double[] RowMajorArray, double[] ColumnMajorArray, double[][] JaggedArray, Matrix<double> A, Vector<double> b)[] _data;

        [IterationSetup]
        public void Setup()
        {
            var rnd = new Random(seed);
            _data = Enumerable.Range(0, Size).Select(_ =>
            {
                var A = Matrix<double>.Build.Random(M, N, rnd.Next());
                var x = Vector<double>.Build.Random(M, rnd.Next());
                var b = A * x;

                var ab = A.InsertColumn(N, b);

                return (RowMajorArray: ab.ToRowMajorArray(),
                        ColumnMajorArray: ab.ToColumnMajorArray(),
                        JaggedArray: ab.ToRowArrays().Select(a => a.Concat(Enumerable.Repeat(0d, System.Numerics.Vector<double>.Count)).ToArray()).ToArray(),
                        A: A,
                        b: b);

            }).ToArray();
        }

        [Benchmark]
        public void CompactColumnMajor()
        {
            for (int iteration = 0; iteration < Size; iteration++)
            {
                var Ab = _data[iteration].ColumnMajorArray;

                // Solve for dx.
                // For each variable in the system...
                for (int j = 0; j + 1 < N; ++j)
                {
                    int pi = j;
                    double max = Math.Abs(Ab[j * M + j]);

                    // Find a pivot row for this variable.
                    for (int i = j + 1; i < M; ++i)
                    {
                        // if(|JxF[i][j]| > max) { pi = i, max = |JxF[i][j]| }
                        double maxj = Math.Abs(Ab[i + M * j]);
                        if (maxj > max)
                        {
                            pi = i;
                            max = maxj;
                        }
                    }

                    // Swap pivot row with the current row.
                    if (pi != j)
                    {
                        for (int n = 0; n <= N; n++)
                        {
                            var tmp = Ab[n * M + pi];
                            Ab[n * M + pi] = Ab[n * M + j];
                            Ab[n * M + j] = tmp;
                        }
                    }

                    // Eliminate the rows after the pivot.
                    double p = Ab[j * M + j];
                    for (int i = j + 1; i < M; ++i)
                    {
                        double s = Ab[i + M * j] / p;
                        if (s != 0.0)
                            for (int ij = j + 1; ij <= N; ++ij)
                                Ab[i + M * ij] -= Ab[j + M * ij] * s;
                    }
                }
            }
        }


        [Benchmark]
        public void CompactRowMajor()
        {

            var eN = N + 1;
            for (int iteration = 0; iteration < Size; iteration++)
            {
                var Ab = _data[iteration].RowMajorArray;

                // Solve for dx.
                // For each variable in the system...
                for (int j = 0; j + 1 < N; ++j)
                {
                    int pi = j;
                    double max = Math.Abs(Ab[j * eN + j]);

                    // Find a pivot row for this variable.
                    for (int i = j + 1; i < M; ++i)
                    {
                        // if(|JxF[i][j]| > max) { pi = i, max = |JxF[i][j]| }
                        double maxj = Math.Abs(Ab[i * eN + j]);
                        if (maxj > max)
                        {
                            pi = i;
                            max = maxj;
                        }
                    }

                    // Swap pivot row with the current row.
                    if (pi != j)
                    {
                        for (int n = 0; n <= N; n++)
                        {
                            var tmp = Ab[n + pi * eN];
                            Ab[n + pi * eN] = Ab[n + j * eN];
                            Ab[n + j * eN] = tmp;
                        }
                    }

                    // Eliminate the rows after the pivot.
                    double p = Ab[j * eN + j];
                    for (int i = j + 1; i < M; ++i)
                    {
                        double s = Ab[i * eN + j] / p;
                        if (s != 0.0)
                            for (int ij = j + 1; ij <= N; ++ij)
                                Ab[i * eN + ij] -= Ab[j * eN + ij] * s;
                    }
                }
            }
        }

        [Benchmark(Baseline = true)]
        public void Current()
        {
            for (int iteration = 0; iteration < Size; iteration++)
            {
                var Ab = _data[iteration].JaggedArray;

                Simulation.Solve(Ab, M, N);
            }
        }

        [Benchmark]
        public void ArrayOfArrays()
        {
            for (int iteration = 0; iteration < Size; iteration++)
            {
                var Ab = _data[iteration].JaggedArray;

                // Solve for dx.
                // For each variAb2le in the system...
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

                    // Eliminate the rows after the pivot.
                    double p = Ab[j][j];
                    for (int i = j + 1; i < M; ++i)
                    {
                        double s = Ab[i][j] / p;
                        if (s != 0.0d)
                        {
                            for (int jj = j + 1; jj <= N; ++jj)
                            {
                                Ab[i][jj] -= Ab[j][jj] * s;
                            }
                        }
                    }
                }
            }
        }

        [Benchmark]
        public void ArrayOfArraysVectorized()
        {
            for (int iteration = 0; iteration < Size; iteration++)
            {
                var Ab = _data[iteration].JaggedArray;

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

                    var vectorLength = System.Numerics.Vector<double>.Count;
                    // Eliminate the rows after the pivot.
                    double p = Ab[j][j];
                    for (int i = j + 1; i < M; ++i)
                    {
                        double s = Ab[i][j] / p;
                        if (s != 0.0)
                        {
                            int jj;
                            for (jj = j + 1; jj <= (N - vectorLength); jj += vectorLength)
                            {
                                var source = new System.Numerics.Vector<double>(Ab[j], jj);
                                var target = new System.Numerics.Vector<double>(Ab[i], jj);
                                var res = target - (source * s);
                                res.CopyTo(Ab[i], jj);
                            }
                            for (; jj <= N; ++jj)
                            {
                                Ab[i][jj] -= Ab[j][jj] * s;
                            }
                        }
                    }
                }
            }
        }

        [Benchmark]
        public void ArrayOfArraysVectorizedNoTailLoop()
        {
            for (int iteration = 0; iteration < Size; iteration++)
            {
                var Ab = _data[iteration].JaggedArray;

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

                    var vectorLength = System.Numerics.Vector<double>.Count;
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
                                var source = new System.Numerics.Vector<double>(Ab[j], jj);
                                var target = new System.Numerics.Vector<double>(Ab[i], jj);
                                var res = target - (source * s);
                                res.CopyTo(Ab[i], jj);
                            }
                        }
                    }
                }
            }
        }

        [Benchmark]
        public void MathNetSolve()
        {
            for (int iteration = 0; iteration < Size; iteration++)
            {
                _data[iteration].A.Solve(_data[iteration].b);
            }
        }

    }
}
