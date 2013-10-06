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

            public abstract void Paint(double x0, double x1, Graphics G);
        }

        public class Function : Series
        {
            protected SyMath.Function f;
            public Function(SyMath.Function f)
            {
                this.f = f;
            }

            public override void Paint(double x0, double x1, Graphics G)
            {
                PointF[] dx_ = new PointF[] { new PointF(3.0f, 3.0f) };
                Matrix T = G.Transform.Clone();
                T.Invert();
                T.TransformVectors(dx_);
                PointF dx = dx_[0];

                Pen pen = new Pen(color, (float)Math.Sqrt((double)(dx.X * dx.X + dx.Y * dx.Y)) / 20.0f);

                int N = 2048;
                PointF[] points = new PointF[N + 1];
                for (int i = 0; i <= N; ++i)
                {
                    double x = ((x1 - x0) * i) / N;
                    points[i].X = (float)x;
                    points[i].Y = (float)f.Call(x);
                }

                G.DrawLines(pen, points);
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

            public override void Paint(double x0, double x1, Graphics G)
            {
                PointF[] dx_ = new PointF[] { new PointF(3.0f, 3.0f) };
                Matrix T = G.Transform.Clone();
                T.Invert();
                T.TransformVectors(dx_);
                PointF dx = dx_[0];

                Pen pen = new Pen(color, (float)Math.Sqrt((double)(dx.X * dx.X + dx.Y * dx.Y)) / 20.0f);
                Brush brush = new SolidBrush(color);

                G.DrawLines(pen, points);
                return;
                switch (pointStyle)
                {
                    case CircuitTests.PointStyle.Square:
                        foreach (PointF i in points)
                            G.FillRectangle(brush, i.X - dx.X, i.Y - dx.Y, dx.X * 2.0f, dx.Y * 2.0f);
                        break;
                    case CircuitTests.PointStyle.Circle:
                        foreach (PointF i in points)
                            G.FillEllipse(brush, i.X - dx.X, i.Y - dx.Y, dx.X * 2.0f, dx.Y * 2.0f);
                        break;
                    case CircuitTests.PointStyle.Cross:
                        foreach (PointF i in points)
                        {
                            G.DrawLine(pen, i.X - dx.X, i.Y - dx.Y, i.X + dx.X, i.Y + dx.Y);
                            G.DrawLine(pen, i.X + dx.X, i.Y - dx.Y, i.X - dx.X, i.Y + dx.Y);
                        }
                        break;
                }
            }
        }

        protected Thread thread;
        protected Form form = new Form();
        
        protected PointF x0;
        protected PointF x1;
        
        protected Dictionary<string, Series> series = new Dictionary<string,Series>();

        public Plot(string Name, int Width, int Height, double x0, double y0, double x1, double y1, Dictionary<string, Series> Series)
        {
            this.x0 = new PointF((float)x0, (float)y0);
            this.x1 = new PointF((float)x1, (float)y1);
            series = Series;

            form.Size = new Size(Width, Height);
            form.Text = Name;
            form.Paint += Plot_Paint;

            thread = new Thread(() => Application.Run(form));
            thread.Start();
        }

        public Plot(string Name, int Width, int Height, double x0, double y0, double x1, double y1, Series S) : this(Name, Width, Height, x0, y0, x1, y1, new Dictionary<string, Series> { { "", S } }) { }

        private List<Color> colors = new List<Color>() { Color.Red, Color.Blue, Color.Green, Color.DarkRed, Color.DarkGreen, Color.DarkBlue, Color.Black };

        void Plot_Paint(object sender, PaintEventArgs e)
        {
            Graphics G = e.Graphics;

            Matrix T = new Matrix(
                new RectangleF(new PointF(0.0f, 0.0f), (SizeF)e.ClipRectangle.Size), 
                new PointF[] { new PointF(x0.X, x1.Y), new PointF(x1.X, x1.Y), new PointF(x0.X, x0.Y) });
            T.Invert();
            G.Transform = T;
            G.SmoothingMode = SmoothingMode.AntiAlias;
            G.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            foreach (KeyValuePair<string, Series> i in series)
            {
                if (i.Value.Color == Color.Transparent)
                {
                    i.Value.Color = colors.First();
                    if (colors.Count > 1)
                        colors.RemoveAt(0);
                }
                i.Value.Paint(x0.X, x1.X, G);
            }
        }
    }
}
