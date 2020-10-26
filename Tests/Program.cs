using System;
using System.IO;

namespace Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            //WriteDocs();

            Test test = new Test();
            test.Run(Directory.EnumerateFiles(@".", "*.schx"), t => Harmonics(t, 0.5, 82, 2));
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
