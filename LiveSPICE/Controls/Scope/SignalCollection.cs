using System;
using System.Collections;
using System.Collections.Generic;

namespace LiveSPICE
{
    public class SignalEventArgs : EventArgs
    {
        private Signal e;
        public Signal Signal { get { return e; } }

        public SignalEventArgs(Signal E) { e = E; }
    }

    public class TickEventArgs : EventArgs
    {
        private long clock;
        public long Clock { get { return Clock; } }

        public TickEventArgs(long Clock) { clock = Clock; }
    }

    /// <summary>
    /// Collection of Signals.
    /// </summary>
    public class SignalCollection : IEnumerable<Signal>, IEnumerable
    {
        protected List<Signal> x = new List<Signal>();

        public Signal this[int index] { get { lock (x) return x[index]; } }

        public delegate void SignalEventHandler(object sender, SignalEventArgs e);

        private List<SignalEventHandler> itemAdded = new List<SignalEventHandler>();
        protected void OnItemAdded(SignalEventArgs e) { foreach (SignalEventHandler i in itemAdded) i(this, e); }
        public event SignalEventHandler ItemAdded
        {
            add { itemAdded.Add(value); }
            remove { itemAdded.Remove(value); }
        }

        private List<SignalEventHandler> itemRemoved = new List<SignalEventHandler>();
        protected void OnItemRemoved(SignalEventArgs e) { foreach (SignalEventHandler i in itemRemoved) i(this, e); }
        public event SignalEventHandler ItemRemoved
        {
            add { itemRemoved.Add(value); }
            remove { itemRemoved.Remove(value); }
        }

        private long clock = 0;
        public long Clock { get { return clock; } }

        private double sampleRate = 1;
        public double SampleRate { get { return sampleRate; } }

        public void TickClock(int SampleCount, double SampleRate)
        {
            sampleRate = SampleRate;

            int truncate = (int)sampleRate / 4;

            // Remove the signals that we didn't get data for.
            ForEach(i =>
            {
                if (i.Clock < clock)
                    i.Clear();
                else
                    i.Truncate(truncate);
            });

            clock += SampleCount;
        }

        // ICollection<Node>
        public int Count { get { lock (x) return x.Count; } }
        public void Add(Signal item)
        {
            lock (x) x.Add(item);
            OnItemAdded(new SignalEventArgs(item));
        }
        public void AddRange(IEnumerable<Signal> items)
        {
            foreach (Signal i in items)
                Add(i);
        }
        public void Clear()
        {
            Signal[] removed = x.ToArray();
            lock (x) x.Clear();

            foreach (Signal i in removed)
                OnItemRemoved(new SignalEventArgs(i));
        }
        public bool Contains(Signal item) { lock (x) return x.Contains(item); }
        public void CopyTo(Signal[] array, int arrayIndex) { lock (x) x.CopyTo(array, arrayIndex); }
        public bool Remove(Signal item)
        {
            bool ret;
            lock (x) ret = x.Remove(item);
            if (ret)
                OnItemRemoved(new SignalEventArgs(item));
            return ret;
        }
        public void RemoveRange(IEnumerable<Signal> items)
        {
            foreach (Signal i in items)
                Remove(i);
        }

        /// <summary>
        /// This is thread safe.
        /// </summary>
        /// <param name="f"></param>
        public void ForEach(Action<Signal> f)
        {
            lock (x) foreach (Signal i in x)
                    f(i);
        }

        // IEnumerable<Node>
        public IEnumerator<Signal> GetEnumerator() { return x.GetEnumerator(); }

        IEnumerator IEnumerable.GetEnumerator() { return this.GetEnumerator(); }
    }
}
