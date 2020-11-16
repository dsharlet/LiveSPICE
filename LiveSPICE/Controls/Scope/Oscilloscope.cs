using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace LiveSPICE
{
    public class Oscilloscope : SignalDisplay
    {
        protected const double MinFrequency = 20.0;
        protected const double MaxPeriod = 1.0 / MinFrequency;

        static Oscilloscope() { DefaultStyleKeyProperty.OverrideMetadata(typeof(Oscilloscope), new FrameworkPropertyMetadata(typeof(Oscilloscope))); }

        private Circuit.Quantity a4 = new Circuit.Quantity(440, Circuit.Units.Hz);
        public Circuit.Quantity A4
        {
            get { return a4; }
            set
            {
                a4.Set(value);
                InvalidateVisual();
                NotifyChanged(nameof(A4));
            }
        }

        protected bool showNotes = true;
        public bool ShowNotes
        {
            get { return showNotes; }
            set
            {
                showNotes = value;
                InvalidateVisual();
                NotifyChanged(nameof(ShowNotes));
            }
        }

        protected double zoom = 10.0f / 440.0f;
        public double Zoom
        {
            get { return zoom; }
            set
            {
                zoom = Math.Min(Math.Max(value, 16.0 / 48000.0), 2 * MaxPeriod);
                InvalidateVisual();
                NotifyChanged(nameof(Zoom));
            }
        }

        protected ScopeMode mode;
        public ScopeMode Mode
        {
            get { return mode; }
            set
            {
                mode = value;
                InvalidateVisual();
                NotifyChanged(nameof(Mode));
            }
        }

        public Pen GridPen = new Pen(Brushes.Gray, 0.25);
        public Pen AxisPen = new Pen(Brushes.Gray, 0.5);
        public Pen TracePen = new Pen(Brushes.White, 0.5);

        protected double Vmax, Vmean;
        protected Point? tracePoint;

        public Oscilloscope()
        {
            Background = Brushes.DimGray;
            Cursor = Cursors.Cross;
            FontFamily = new FontFamily("Courier New");

            Signals = new SignalCollection();
            Signals.ItemAdded += (o, e) => InvalidateVisual();
            Signals.ItemRemoved += (o, e) => InvalidateVisual();

            CommandBindings.Add(new CommandBinding(NavigationCommands.Zoom, (o, e) => Zoom *= 0.5));
            CommandBindings.Add(new CommandBinding(NavigationCommands.DecreaseZoom, (o, e) => Zoom *= 2.0));

            MouseWheel += (o, e) => { Zoom /= (float)Math.Pow(2.0f, (float)e.Delta / (120.0f * 4.0f)); InvalidateVisual(); };
            MouseMove += (o, e) => { tracePoint = e.GetPosition(this); InvalidateVisual(); };
            MouseLeave += (o, e) => { tracePoint = null; InvalidateVisual(); };
            MouseDown += (o, e) => { Focus(); InvalidateVisual(); };
        }

        protected override void OnRender(DrawingContext DC)
        {
            DC.DrawRectangle(BorderBrush, null, new Rect(0, 0, ActualWidth, ActualHeight));
            Rect bounds = new Rect(
                BorderThickness.Left,
                BorderThickness.Top,
                ActualWidth - (BorderThickness.Right + BorderThickness.Left),
                ActualHeight - (BorderThickness.Top + BorderThickness.Bottom));
            DC.PushClip(new RectangleGeometry(bounds));
            DC.DrawRectangle(Background, null, bounds);

            DrawTimeAxis(DC, bounds);

            if (Signals.Empty())
            {
                DC.Pop();
                return;
            }

            Signal stats = SelectedSignal;
            Signal stabilize = stats;
            if (stabilize == null)
                stabilize = Signals.First();

            double sampleRate = Signals.SampleRate;

            double f0 = 0.0;

            // Remembe the clock for when we analyzed the signal to keep the signals in sync even if new data gets added in the background.
            long sync = stabilize.Clock;

            lock (stabilize.Lock)
            {
                int Decimate = 1 << (int)Math.Floor(Math.Log(sampleRate / 22000, 2));
                int BlockSize = 8192;
                if (stabilize.Count >= BlockSize)
                {
                    double[] data = stabilize.Skip(stabilize.Count - BlockSize).ToArray();

                    // Estimate the fundamental frequency of the signal.
                    double phase;
                    double f = Frequency.Estimate(data, Decimate, out phase);

                    // Convert phase from (-pi, pi] to (0, 1]
                    phase = ((phase + Math.PI) / (2 * Math.PI));

                    // Shift all the signals by the phase in samples to align the signal between frames.
                    if (f > 1.0)
                        sync -= (int)Math.Round(phase * BlockSize / f);

                    // Compute fundamental frequency in Hz.
                    f0 = sampleRate * f / BlockSize;
                }
            }

            double mean = 0.0;
            double peak = 0.0;
            double rms = 0.0;
            if (stats != null)
            {
                lock (stats.Lock)
                {
                    // Compute statistics of the clock signal.
                    mean = stats.Sum() / stats.Count;
                    peak = stats.Max(i => Math.Abs(i - mean), 0.0);
                    rms = Math.Sqrt(stats.Sum(i => (i - mean) * (i - mean)) / stats.Count);
                }
            }
            else
            {
                foreach (Signal i in signals)
                    lock (i.Lock) peak = Math.Max(peak, i.Max(j => Math.Abs(j), 0.0));
            }

            // Compute the target min/max
            double bound = peak;
            double gamma = 0.1;
            double window = Math.Max(Math.Pow(2.0, Math.Ceiling(Math.Log(bound + 1e-9, 2.0))), 5e-3);
            Vmax = Math.Max(TimeFilter(Vmax, window, gamma), Math.Abs(bound + (Vmean - mean)));
            Vmean = TimeFilter(Vmean, mean, gamma);
            if (Math.Abs(mean) * 1e2 < Vmax)
                Vmean = 0.0;

            DrawSignalAxis(DC, bounds);

            foreach (Signal i in signals.Except(stabilize).Append(stabilize))
                lock (i.Lock) DrawSignal(DC, bounds, i, (int)(sync - i.Clock));

            if (stats != null)
                DrawStatistics(DC, bounds, stats.Pen.Brush, peak, mean, rms, f0);

            if (tracePoint.HasValue)
                DrawTrace(DC, bounds, tracePoint.Value);

            DC.Pop();
        }

        protected void DrawTimeAxis(DrawingContext DC, Rect Bounds)
        {
            double y = (Bounds.Bottom + Bounds.Top) / 2;

            // Draw axis.
            DC.DrawLine(AxisPen, new Point(Bounds.Left, y), new Point(Bounds.Right, y));

            Typeface typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);

            double tmin = MapToTime(Bounds, 0);
            double dt = Partition(MapToTime(Bounds, 80) - MapToTime(Bounds, 0));
            double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            for (double t = -dt; t > tmin; t -= dt)
            {
                double x = MapFromTime(Bounds, t);

                DC.DrawLine(GridPen, new Point(x, Bounds.Top), new Point(x, Bounds.Bottom));

                DC.DrawLine(AxisPen, new Point(x, y - 3), new Point(x, y + 3));

                FormattedText time = new FormattedText(
                    Circuit.Quantity.ToString(-t, Circuit.Units.s),
                    System.Globalization.CultureInfo.CurrentCulture,
                    System.Windows.FlowDirection.LeftToRight,
                    typeface, FontSize,
                    AxisPen.Brush, pixelsPerDip);
                DC.DrawText(time, new Point(x - time.Width / 2, y - time.Height - 2));

                FormattedText freq = new FormattedText(
                    FrequencyToString(1.0 / Math.Abs(t)),
                    System.Globalization.CultureInfo.CurrentCulture,
                    System.Windows.FlowDirection.LeftToRight,
                    typeface, FontSize,
                    AxisPen.Brush, pixelsPerDip);
                DC.DrawText(freq, new Point(x - freq.Width / 2, y + 3));
            }
        }

        protected void DrawSignalAxis(DrawingContext DC, Rect Bounds)
        {
            // Draw axis on the left.
            double x = Bounds.Right - 1;

            double dv = Partition(MapToSignal(Bounds, 0) - MapToSignal(Bounds, 50));

            int labels = (int)((2 * Vmax) / dv);
            labels = (labels / 2) * 2;

            Typeface typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
            double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            for (double v = -Vmax + (2 * Vmax - (labels * dv)) / 2 + Vmean; v < Vmax + Vmean; v += dv)
            {
                double y = MapFromSignal(Bounds, v);
                if (y + 10 < (Bounds.Top + Bounds.Bottom) / 2 ||
                    y - 10 > (Bounds.Top + Bounds.Bottom) / 2)
                {
                    DC.DrawLine(GridPen, new Point(Bounds.Left, y), new Point(Bounds.Right, y));

                    DC.DrawLine(AxisPen, new Point(x - 3, y), new Point(x + 3, y));

                    FormattedText volts = new FormattedText(
                        Circuit.Quantity.ToString(v, Circuit.Units.V, "+G3"),
                        System.Globalization.CultureInfo.CurrentCulture,
                        System.Windows.FlowDirection.LeftToRight,
                        typeface, FontSize,
                        AxisPen.Brush, pixelsPerDip);
                    DC.DrawText(volts, new Point(x - volts.Width - 3, y - volts.Height / 2));
                }
            }
        }

        // Keep this here to avoid generating garbage for the GC.
        readonly List<Point> points = new List<Point>();
        protected void DrawSignal(DrawingContext DC, Rect Bounds, Signal S, int shift)
        {
            shift += S.Count;

            // Rate of pixels to sample.
            const double rate = 1.0;

            // How many pixels map to one sample.
            double margin = Bounds.Width / (double)(zoom * Signals.SampleRate);
            points.Clear();
            Point p = new Point();
            for (double i = -margin; i <= Bounds.Right + margin; i += rate)
            {
                int s0 = MapToSample(Bounds, i) + shift;
                int s1 = MapToSample(Bounds, i + rate) + shift;

                if (s1 > s0)
                {
                    // Anti-aliasing.
                    // TODO: Better antialiasing (this is just a box filter).
                    double v = 0.0;
                    for (int j = s0; j < s1; ++j)
                        v += S[j];

                    if (!double.IsNaN(v))
                    {
                        p.X = i;
                        p.Y = MapFromSignal(Bounds, v / (s1 - s0));
                        points.Add(p);
                    }
                }
            }

            DouglasPeuckerReduction.Reduce(points, 0.5);
            for (int i = 0; i + 1 < points.Count; ++i)
                DC.DrawLine(S.Pen, points[i], points[i + 1]);
        }

        protected void DrawTrace(DrawingContext DC, Rect Bounds, Point At)
        {
            DC.DrawLine(TracePen, new Point(Bounds.Left, At.Y), new Point(Bounds.Right, At.Y));
            DC.DrawLine(TracePen, new Point(At.X, Bounds.Top), new Point(At.X, Bounds.Bottom));

            Typeface typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
            double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;

            double t = MapToTime(Bounds, At.X);
            FormattedText time = new FormattedText(
                Circuit.Quantity.ToString(-t, Circuit.Units.s),
                System.Globalization.CultureInfo.CurrentCulture,
                System.Windows.FlowDirection.LeftToRight,
                typeface, FontSize,
                TracePen.Brush, pixelsPerDip);
            DC.DrawText(time, new Point(At.X - time.Width - 2, (Bounds.Top + Bounds.Bottom) / 2 - time.Height - 2));
            FormattedText freq = new FormattedText(
                FrequencyToString(1.0 / Math.Abs(t)),
                System.Globalization.CultureInfo.CurrentCulture,
                System.Windows.FlowDirection.LeftToRight,
                typeface, FontSize,
                TracePen.Brush, pixelsPerDip);
            DC.DrawText(freq, new Point(At.X - freq.Width - 2, (Bounds.Top + Bounds.Bottom) / 2 + 3));

            double v = MapToSignal(Bounds, At.Y);
            FormattedText volts = new FormattedText(
                Circuit.Quantity.ToString(v, Circuit.Units.V, "+G3"),
                System.Globalization.CultureInfo.CurrentCulture,
                System.Windows.FlowDirection.LeftToRight,
                typeface, FontSize,
                TracePen.Brush, pixelsPerDip);
            DC.DrawText(volts, new Point(Bounds.Right - volts.Width - 4, At.Y));
        }

        protected void DrawStatistics(DrawingContext DC, Rect Bounds, Brush Brush, double Peak, double Mean, double Rms, double Freq)
        {
            FormattedText stats = new FormattedText(
                String.Format(
                    "\u0192\u2080:   {0}\nPeak: {1}\nMean: {2}\nRms:  {3}",
                    FrequencyToString(Freq),
                    Circuit.Quantity.ToString(Peak, Circuit.Units.V, "+G3"),
                    Circuit.Quantity.ToString(Mean, Circuit.Units.V, "+G3"),
                    Circuit.Quantity.ToString(Rms, Circuit.Units.V, "+G3")),
                System.Globalization.CultureInfo.CurrentCulture,
                System.Windows.FlowDirection.LeftToRight,
                new Typeface(FontFamily, FontStyle, FontWeight, FontStretch), FontSize,
                Brush, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            DC.DrawText(stats, Bounds.TopLeft);
        }

        private double MapFromTime(Rect Bounds, double t) { return Bounds.Right + (t * Bounds.Width / zoom); }
        private double MapToTime(Rect Bounds, double x) { return (x - Bounds.Right) * zoom / Bounds.Width; }

        private int MapToSample(Rect Bounds, double x) { return (int)Math.Round(((x - Bounds.Right) * zoom * Signals.SampleRate) / Bounds.Width); }

        private double MapToSignal(Rect Bounds, double y) { return Vmax - (y - Bounds.Top) * 2 * Vmax / Bounds.Height + Vmean; }
        private double MapFromSignal(Rect Bounds, double v) { return Bounds.Top + ((Vmax - (v - Vmean)) / (2 * Vmax) * Bounds.Height); }


        private static double Partition(double P)
        {
            double[] Partitions = { 10.0, 4.0, 2.0 };

            double p = Math.Pow(10.0, Math.Ceiling(Math.Log10(P)));
            foreach (double i in Partitions)
                if (p / i > P)
                    return p / i;
            return p;
        }

        private string FrequencyToString(double f)
        {
            if (ShowNotes)
                return Frequency.ToNote(f, (double)A4);
            else
                return Circuit.Quantity.ToString(f, Circuit.Units.Hz);
        }

        private static double TimeFilter(double Prev, double Cur, double t)
        {
            if (double.IsNaN(Prev) || double.IsNaN(Cur))
                return Cur;

            return Prev + (Cur - Prev) * t;
        }
    }
}