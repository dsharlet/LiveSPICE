using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Numerics;
using SyMath;

namespace LiveSPICE
{
    /// <summary>
    /// Properties and methods associated with a particular signal.
    /// </summary>
    public class Signal : IEnumerable<double>
    {
        private List<double> samples = new List<double>();

        private string name;
        public string Name { get { return name; } set { name = value; } }

        // Display parameters.
        private Pen pen = new Pen(Brushes.White, 1.0);
        public Pen Pen { get { return pen; } set { pen = value; } }

        private object tag;
        public object Tag { get { return tag; } set { tag = value; } }

        // Process new samples for this signal.
        public void AddSamples(long Clock, double[] Samples)
        {
            lock (this)
            {
                samples.AddRange(Samples);
                clock = Clock;
            }
        }

        public void Truncate(int Truncate)
        {
            if (samples.Count > Truncate)
            {
                int remove = samples.Count - Truncate;
                samples.RemoveRange(0, remove);
            }
        }

        private long clock = 0;
        public long Clock { get { return clock; } }

        public void Clear() { samples.Clear(); clock = 0; }

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

        // IEnumerable<double> interface
        IEnumerator<double> IEnumerable<double>.GetEnumerator() { return samples.GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return samples.GetEnumerator(); }
    }
}