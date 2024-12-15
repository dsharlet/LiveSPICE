using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Windows.Forms;
using Matrix2D = System.Drawing.Drawing2D.Matrix;

namespace Plotting
{
    /// <summary>
    /// A single plot window.
    /// </summary>
    public class Plot
    {
        protected SeriesCollection series;
        /// <summary>
        /// The series displayed in this plot.
        /// </summary>
        public SeriesCollection Series { get { return series; } }

        /// <summary>
        /// Width of the plot window.
        /// </summary>
        public int Width { get { return form.Width; } set { Invoke(() => form.Width = value); } }
        /// <summary>
        /// Height of the plot window.
        /// </summary>
        public int Height { get { return form.Height; } set { Invoke(() => form.Height = value); } }

        private double _x0 = -10.0, _x1 = 10.0;
        private double _y0 = double.NaN, _y1 = double.NaN;
        /// <summary>
        /// Plot area bounds.
        /// </summary>
        public double x0 { get { return _x0; } set { Invoke(() => { _x0 = value; Invalidate(); }); } }
        public double y0 { get { return _y0; } set { Invoke(() => { _y0 = value; Invalidate(); }); } }
        public double x1 { get { return _x1; } set { Invoke(() => { _x1 = value; Invalidate(); }); } }
        public double y1 { get { return _y1; } set { Invoke(() => { _y1 = value; Invalidate(); }); } }

        private string xlabel = null, ylabel = null;
        /// <summary>
        /// Axis labels.
        /// </summary>
        public string xLabel { get { return xlabel; } set { Invoke(() => { xlabel = value; Invalidate(); }); } }
        public string yLabel { get { return ylabel; } set { Invoke(() => { ylabel = value; Invalidate(); }); } }

        string title = null;
        /// <summary>
        /// Title of the plot window.
        /// </summary>
        public string Title { get { return title; } set { Invoke(() => { form.Text = title = value; Invalidate(); }); } }

        private bool showLegend = true;
        /// <summary>
        /// Show or hide the legend.
        /// </summary>
        public bool ShowLegend { get { return showLegend; } set { Invoke(() => { showLegend = value; Invalidate(); }); } }

        protected Thread thread;
        protected Form form = new Form();
        protected bool shown = false;
        private void Invoke(Action action)
        {
            while (!shown)
                Thread.Sleep(0);
            form.Invoke((Delegate)action);
        }

        public Plot()
        {
            series = new SeriesCollection();
            series.ItemAdded += (o, e) => Invalidate();
            series.ItemRemoved += (o, e) => Invalidate();

            form = new Form()
            {
                Text = "Plot",
                Width = 300,
                Height = 300,
            };
            form.Paint += Plot_Paint;
            form.SizeChanged += Plot_SizeChanged;
            form.Shown += (o, e) => shown = true;
            form.KeyDown += (o, e) => { if (e.KeyCode == Keys.Escape) form.Close(); };

            thread = new Thread(() => Application.Run(form));
            thread.Start();
        }

        public void Save(string Filename)
        {
            Rectangle bounds = new Rectangle(0, 0, Width, Height);
            Bitmap bitmap = new Bitmap(Width, Height);
            Invoke(() => form.DrawToBitmap(bitmap, bounds));
            bitmap.Save(Filename);
        }

        private RectangleF PaintTitle(Graphics G, RectangleF Area)
        {
            if (title == null)
                return Area;

            Font font = new Font(form.Font.FontFamily, form.Font.Size * 1.5f, FontStyle.Bold);

            // Draw title.
            SizeF sz = G.MeasureString(title, font);
            G.DrawString(title, font, Brushes.Black, new PointF(Area.Left + (Area.Width - sz.Width) / 2, Area.Top));
            Area.Y += sz.Height + 10.0f;
            Area.Height -= sz.Height + 10.0f;

            return Area;
        }

        private RectangleF PaintAxisLabels(Graphics G, RectangleF Area)
        {
            Font font = new Font(form.Font.FontFamily, form.Font.Size * 1.25f);

            // Draw axis labels.
            if (xlabel != null)
            {
                SizeF sz = G.MeasureString(xlabel, font);
                G.DrawString(xlabel, font, Brushes.Black, new PointF(Area.Left + (Area.Width - sz.Width) / 2, Area.Bottom - sz.Height - 5.0f));
                Area.Height -= sz.Height + 10.0f;
            }
            if (ylabel != null)
            {
                SizeF sz = G.MeasureString(ylabel, font);
                G.TranslateTransform(Area.Left + 5.0f, Area.Top + (Area.Height + sz.Width) / 2.0f);
                G.RotateTransform(-90.0f);
                G.DrawString(ylabel, font, Brushes.Black, new PointF(0.0f, 0.0f));
                G.ResetTransform();

                Area.X += sz.Height + 10.0f;
                Area.Width -= sz.Height + 10.0f;
            }

            Area.X += 30.0f;
            Area.Width -= 30.0f;
            Area.Height -= 20.0f;

            return Area;
        }

        private RectangleF PaintLegend(Graphics G, RectangleF Area)
        {
            if (!showLegend)
                return Area;

            Font font = form.Font;

            // Draw legend.
            float legendWidth = 0.0f;
            series.ForEach(i =>
            {
                SizeF sz = G.MeasureString(i.Name, font);
                legendWidth = Math.Max(legendWidth, sz.Width);
            });

            float legendY = Area.Top;
            series.ForEach(i =>
            {
                PointF lx = new PointF(Area.Right - legendWidth, legendY);
                SizeF sz = G.MeasureString(i.Name, font);
                G.DrawString(i.Name, font, Brushes.Black, lx);

                PointF[] points = new PointF[]
                {
                    new PointF(lx.X - 25.0f, legendY + sz.Height / 2.0f),
                    new PointF(lx.X - 5.0f, legendY + sz.Height / 2.0f),
                };
                G.DrawLines(i.Pen, points);
                legendY += sz.Height;
            });

            Area.Width -= legendWidth + 30.0f;

            return Area;
        }

        private void Plot_Paint(object sender, PaintEventArgs e)
        {
            if (series.Count == 0)
                return;

            Graphics G = e.Graphics;
            G.SmoothingMode = SmoothingMode.AntiAlias;
            G.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            Pen axis = Pens.Black;
            Pen grid = Pens.LightGray;
            Font font = form.Font;

            // Compute plot area.
            RectangleF area = e.ClipRectangle;
            area.Inflate(-10.0f, -10.0f);

            area = PaintTitle(G, area);
            area = PaintAxisLabels(G, area);
            area = PaintLegend(G, area);

            // Draw background.
            G.FillRectangle(Brushes.White, area);
            G.DrawRectangle(Pens.Gray, area.Left, area.Top, area.Width, area.Height);

            // Compute plot bounds.
            PointF x0, x1;
            double y0 = _y0, y1 = _y1;
            if (double.IsNaN(y0) || double.IsNaN(y1))
                series.AutoBounds(_x0, _x1, out y0, out y1);
            x0 = new PointF((float)_x0, (float)y0);
            x1 = new PointF((float)_x1, (float)y1);

            Matrix2D T = new Matrix2D(
                area,
                new PointF[] { new PointF(x0.X, x1.Y), new PointF(x1.X, x1.Y), new PointF(x0.X, x0.Y) });
            T.Invert();

            // Draw axes.
            double dx = Partition((x1.X - x0.X) / (Width / 80));
            for (double x = x0.X - x0.X % dx; x <= x1.X; x += dx)
            {
                PointF tx = Tx(T, new PointF((float)x, 0.0f));
                string s = x.ToString("G3");
                SizeF sz = G.MeasureString(s, font);
                G.DrawString(s, font, Brushes.Black, new PointF(tx.X - sz.Width / 2, area.Bottom + 3.0f));
                G.DrawLine(grid, new PointF(tx.X, area.Top), new PointF(tx.X, area.Bottom));
            }

            double dy = Partition((x1.Y - x0.Y) / (Height / 50));
            for (double y = x0.Y - x0.Y % dy; y <= x1.Y; y += dy)
            {
                PointF tx = Tx(T, new PointF(0.0f, (float)y));
                string s = y.ToString("G3");
                SizeF sz = G.MeasureString(s, font);
                G.DrawString(s, font, Brushes.Black, new PointF(area.Left - sz.Width, tx.Y - sz.Height / 2));
                G.DrawLine(grid, new PointF(area.Left, tx.Y), new PointF(area.Right, tx.Y));
            }

            G.DrawLine(axis, Tx(T, new PointF(x0.X, 0.0f)), Tx(T, new PointF(x1.X, 0.0f)));
            G.DrawLine(axis, Tx(T, new PointF(0.0f, x0.Y)), Tx(T, new PointF(0.0f, x1.Y)));
            G.DrawRectangle(Pens.Gray, area.Left, area.Top, area.Width, area.Height);
            G.SetClip(area);

            // Draw series.
            series.ForEach(i => i.Paint(T, x0.X, x1.X, G));
        }

        private static PointF Tx(Matrix2D T, PointF x)
        {
            PointF[] xs = new[] { x };
            T.TransformPoints(xs);
            return xs[0];
        }

        private void Invalidate() { form.Invalidate(); }

        private void Plot_SizeChanged(object sender, EventArgs e) { form.Invalidate(); }

        private static double Partition(double P)
        {
            double[] Partitions = { 10.0, 4.0, 2.0 };

            double p = Math.Pow(10.0, Math.Ceiling(Math.Log10(P)));
            foreach (double i in Partitions)
                if (p / i > P)
                    return p / i;
            return p;
        }
    }
}
