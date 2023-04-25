using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveSPICE.CLI.Utils
{
    internal static class FunctionGenerator
    {
        /// <summary>
        /// Generate a function with the first <paramref name="n"/> harmonics of <paramref name="f0"/>.
        /// </summary>
        /// <param name="time">Time.</param>
        /// <param name="amplitude">Amplitude.</param>
        /// <param name="f0">Base frequency.</param>
        /// <param name="n">Number of harmonics.</param>
        /// <returns></returns>
        public static double Harmonics(double time, double amplitude, double f0, int n)
        {
            double s = 0;
            for (int i = 1; i <= n; ++i)
                s += Math.Sin(time * f0 * 2 * Math.PI * i) / n;
            return amplitude * s;
        }
    }
}
