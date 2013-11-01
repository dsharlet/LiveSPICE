using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Circuit
{
    public class Timer
    {
        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);
        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(out long lpFrequency);

        public static long Counter
        {
            get
            {
                long t;
                QueryPerformanceCounter(out t);
                return t;
            }
        }

        public static double Frequency
        {
            get
            {
                long f;
                QueryPerformanceFrequency(out f);
                return (double)f;
            }
        }

        private long begin;
        public Timer() { begin = Counter; }

        public static implicit operator double(Timer T) { return (Counter - T.begin) / Frequency; }

        public override string ToString() { return Quantity.ToString((Counter - begin) / Frequency, Units.s); }
    }
}
