using System.Collections;
using System.Collections.Generic;
using System.Windows.Media;

namespace LiveSPICE
{
    /// <summary>
    /// Properties and methods associated with a particular signal.
    /// </summary>
    public class Signal : IEnumerable<double>
    {
        private List<double> samples = new List<double>();

        private string name;
        /// <summary>
        /// Name of this signal.
        /// </summary>
        public string Name { get { return name; } set { name = value; } }
        public override string ToString() { return name; }

        private Pen pen = new Pen(Brushes.White, 1.0);
        /// <summary>
        /// Pen to draw this signal with.
        /// </summary>
        public Pen Pen { get { return pen; } set { pen = value; } }

        private object tag;
        public object Tag { get { return tag; } set { tag = value; } }

        private long clock = 0;
        public long Clock { get { return clock; } }

        /// <summary>
        /// Add new samples to this signal.
        /// </summary>
        /// <param name="Clock"></param>
        /// <param name="Samples"></param>
        public void AddSamples(long Clock, double[] Samples)
        {
            lock (Lock)
            {
                samples.AddRange(Samples);
                clock = Clock;
            }
        }

        /// <summary>
        /// Truncate samples older than NewCount.
        /// </summary>
        /// <param name="Truncate"></param>
        public void Truncate(int NewCount)
        {
            lock (Lock)
            {
                if (samples.Count > NewCount)
                {
                    int remove = samples.Count - NewCount;
                    samples.RemoveRange(0, remove);
                }
            }
        }

        public void Clear() { lock (Lock) { samples.Clear(); clock = 0; } }

        public int Count { get { return samples.Count; } }
        public double this[int i]
        {
            get
            {
                if (0 <= i && i < samples.Count)
                    return samples[(int)i];
                else
                    return double.NaN;
            }
        }

        public object Lock { get { return samples; } }

        // IEnumerable<double> interface
        IEnumerator<double> IEnumerable<double>.GetEnumerator() { return samples.GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return samples.GetEnumerator(); }
    }
}