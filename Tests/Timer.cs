using System.Runtime.InteropServices;

namespace Tests
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

        public static double Delta(long t1) { return (double)(Counter - t1) / (double)Frequency; }

        public static implicit operator double(Timer T) { return (Counter - T.begin) / Frequency; }

        public override string ToString() { return ((Counter - begin) / Frequency).ToString("G3"); }
    }
}
