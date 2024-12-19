using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Plotting
{
    public class SeriesEventArgs : EventArgs
    {
        private Series s;
        public Series Series { get { return s; } }

        public SeriesEventArgs(Series S) { s = S; }
    }

    /// <summary>
    /// Collection of Series.
    /// </summary>
    public class SeriesCollection : ICollection<Series>, IEnumerable<Series>
    {
        protected List<Series> x = new List<Series>();
        protected List<Color> colors = new List<Color>()
        {
            Color.Red,
            Color.Blue,
            Color.Green,
            Color.DarkRed,
            Color.DarkGreen,
            Color.DarkBlue,
        };

        /// <summary>
        /// Default colors used for series if none is specified.
        /// </summary>
        public IList<Color> Colors { get { return colors; } }

        /// <summary>
        /// Get the series at the specified index.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public Series this[int i] { get { return x[i]; } }

        /// <summary>
        /// Estimate useful bounds for displaying the signals in this collection.
        /// </summary>
        /// <param name="x0"></param>
        /// <param name="x1"></param>
        /// <param name="y0"></param>
        /// <param name="y1"></param>
        public void AutoBounds(double x0, double x1, out double y0, out double y1)
        {
            lock (x)
            {
                int N = 0;

                // Compute the mean.
                float mean = 0.0f;
                x.ForEach(i =>
                {
                    List<PointF[]> xy = i.Evaluate(x0, x1);
                    mean += xy.Sum(j => j.Sum(k => k.Y));
                    N += xy.Sum(j => j.Count());
                });
                mean /= N;

                // Compute standard deviation.
                float stddev = 0.0f;
                float max = 0.0f;
                //series.ForEach(i => stddev = Math.Max(stddev, i.Evaluate(_x0, _x1).Max(j => j.Max(k => Math.Abs(mean - k.Y))) * 1.25 + 1e-6));
                x.ForEach(i =>
                {
                    List<PointF[]> xy = i.Evaluate(x0, x1);
                    stddev += xy.Sum(j => j.Sum(k => (mean - k.Y) * (mean - k.Y)));
                    max = xy.Max(j => j.Max(k => Math.Abs(mean - k.Y)), max);
                });
                stddev = (float)Math.Sqrt(stddev / N) * 4.0f;
                float y = Math.Min(stddev, max) * 1.25f + 1e-6f;

                y0 = mean - y;
                y1 = mean + y;
            }
        }

        public delegate void SeriesEventHandler(object sender, SeriesEventArgs e);

        private List<SeriesEventHandler> itemAdded = new List<SeriesEventHandler>();
        protected void OnItemAdded(SeriesEventArgs e) { foreach (SeriesEventHandler i in itemAdded) i(this, e); }
        /// <summary>
        /// Called when a series is added to the collection.
        /// </summary>
        public event SeriesEventHandler ItemAdded
        {
            add { itemAdded.Add(value); }
            remove { itemAdded.Remove(value); }
        }

        private List<SeriesEventHandler> itemRemoved = new List<SeriesEventHandler>();
        protected void OnItemRemoved(SeriesEventArgs e) { foreach (SeriesEventHandler i in itemRemoved) i(this, e); }
        /// <summary>
        /// Called when a series is removed from the collection.
        /// </summary>
        public event SeriesEventHandler ItemRemoved
        {
            add { itemRemoved.Add(value); }
            remove { itemRemoved.Remove(value); }
        }

        // ICollection<Series>
        public int Count { get { lock (x) return x.Count; } }
        public bool IsReadOnly { get { return false; } }
        public void Add(Series item)
        {
            lock (x)
            {

                if (item.Pen == Pens.Transparent)
                    item.Pen = new Pen(colors.ArgMin(j => x.Count(k => k.Pen != null && k.Pen.Color == j)), 0.5f);
                x.Add(item);
            }
            OnItemAdded(new SeriesEventArgs(item));
        }
        public void AddRange(IEnumerable<Series> items)
        {
            lock (x) foreach (Series i in items)
                    Add(i);
        }
        public void Clear()
        {
            Series[] removed;
            lock (x)
            {
                removed = x.ToArray();
                x.Clear();
            }

            foreach (Series i in removed)
                OnItemRemoved(new SeriesEventArgs(i));
        }
        public bool Contains(Series item) { lock (x) return x.Contains(item); }
        public void CopyTo(Series[] array, int arrayIndex) { lock (x) x.CopyTo(array, arrayIndex); }
        public bool Remove(Series item)
        {
            bool ret;
            lock (x) ret = x.Remove(item);
            if (ret)
                OnItemRemoved(new SeriesEventArgs(item));
            return ret;
        }
        public void RemoveRange(IEnumerable<Series> items)
        {
            foreach (Series i in items)
                Remove(i);
        }

        public void ForEach(Action<Series> f)
        {
            lock (x) foreach (Series i in x)
                    f(i);
        }

        // IEnumerable<Series>
        public IEnumerator<Series> GetEnumerator() { return x.GetEnumerator(); }

        IEnumerator IEnumerable.GetEnumerator() { return this.GetEnumerator(); }
    }
}
