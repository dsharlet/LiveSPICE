using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace CircuitTests
{
    public enum PointStyle
    {
        None,
        Square,
        Circle,
        Cross,
    }
    
    public class Plot
    {
        public abstract class Series
        {
            protected Color color = Color.Transparent;
            public Color Color { get { return color; } set { color = value; } }

            public abstract void Paint(System.Drawing.Drawing2D.Matrix T, double x0, double x1, Graphics G);

            public virtual double MinY { get { return 0.0; } }
            public virtual double MaxY { get { return 0.0; } }
        }

        public class Function : Series
        {
            protected SyMath.Function f;
            public Function(SyMath.Function f)
            {
                this.f = f;
            }

            public override void Paint(System.Drawing.Drawing2D.Matrix T, double x0, double x1, Graphics G)
            {
                PointF[] dx_ = new PointF[] { new PointF(3.0f, 3.0f) };
                PointF dx = dx_[0];

                Pen pen = new Pen(color, 1.0f);

                int N = 2048;
                PointF[] points = new PointF[N + 1];
                for (int i = 0; i <= N; ++i)
                {
                    double x = ((x1 - x0) * i) / N;
                    points[i].X = (float)x;
                    points[i].Y = (float)f.Call(x);
                }

                System.Drawing.PointF[] Tpoints = new System.Drawing.PointF[points.Length];
                points.CopyTo(Tpoints, 0);
                T.TransformPoints(Tpoints);
                G.DrawLines(pen, Tpoints);
            }
        }

        public class Scatter : Series
        {
            protected PointStyle pointStyle = PointStyle.Square;
            public PointStyle PointStyle { get { return pointStyle; } set { pointStyle = value; } }

            protected PointF[] points;
            public Scatter(IEnumerable<SyMath.Arrow> Points)
            {
                points = Points.Select(i => new PointF((float)(double)i.Left, (float)(double)i.Right)).ToArray();
            }

            public override void Paint(System.Drawing.Drawing2D.Matrix T, double x0, double x1, Graphics G)
            {
                PointF dx = new PointF(3.0f, 3.0f);

                Pen pen = new Pen(color, 1.0f);
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                Brush brush = new SolidBrush(color);

                System.Drawing.PointF[] Tpoints = new System.Drawing.PointF[points.Length];
                points.CopyTo(Tpoints, 0);
                T.TransformPoints(Tpoints);
                G.DrawLines(pen, Tpoints);
            }

            public override double MinY { get { return points.Min(i => i.Y); } }
            public override double MaxY { get { return points.Max(i => i.Y); } }
        }

        protected Thread thread;
        protected Form form = new Form();
        
        protected PointF x0;
        protected PointF x1;
        
        protected Dictionary<string, Series> series = new Dictionary<string,Series>();

        public Plot(string Name, int Width, int Height, double x0, double y0, double x1, double y1, Dictionary<string, Series> Series)
        {
            if (y1 - y0 <= 0)
            {
                y0 = Series.Min(i => i.Value.MinY) - 1e-6;
                y1 = Series.Max(i => i.Value.MaxY) + 1e-6;
                double y = (y0 + y1) / 2.0;

                y0 = (y0 - y) * 1.25 + y;
                y1 = (y1 - y) * 1.25 + y;
            }

            this.x0 = new PointF((float)x0, (float)y0);
            this.x1 = new PointF((float)x1, (float)y1);
            series = Series;

            form.Size = new Size(Width, Height);
            form.Text = Name;
            form.Paint += Plot_Paint;
            form.SizeChanged += Plot_SizeChanged;

            thread = new Thread(() => Application.Run(form));
            thread.Start();
        }

        public Plot(string Name, int Width, int Height, double x0, double y0, double x1, double y1, Series S) : this(Name, Width, Height, x0, y0, x1, y1, new Dictionary<string, Series> { { "", S } }) { }

        private List<Color> colors = new List<Color>() { Color.Red, Color.Blue, Color.Green, Color.DarkRed, Color.DarkGreen, Color.DarkBlue };

        void Plot_Paint(object sender, PaintEventArgs e)
        {
            Graphics G = e.Graphics;

            System.Drawing.Drawing2D.Matrix T = new System.Drawing.Drawing2D.Matrix(
                new RectangleF(new PointF(0.0f, 0.0f), (SizeF)e.ClipRectangle.Size), 
                new PointF[] { new PointF(x0.X, x1.Y), new PointF(x1.X, x1.Y), new PointF(x0.X, x0.Y) });
            T.Invert();
            G.SmoothingMode = SmoothingMode.AntiAlias;
            G.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            foreach (KeyValuePair<string, Series> i in series)
            {
                if (i.Value.Color == Color.Transparent)
                    i.Value.Color = colors.ArgMin(j => series.Count(k => k.Value.Color == j));
                i.Value.Paint(T, x0.X, x1.X, G);
            }
        }

        void Plot_SizeChanged(object sender, EventArgs e)
        {
            form.Invalidate();
        }
    }
}
