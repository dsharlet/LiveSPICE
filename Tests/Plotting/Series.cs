using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Matrix2D = System.Drawing.Drawing2D.Matrix;

namespace Plotting
{
    public enum PointStyle
    {
        None,
        Square,
        Circle,
        Cross,
    }

    /// <summary>
    /// A data series.
    /// </summary>
    public abstract class Series
    {
        private string name = "y[x]";
        public string Name { get { return name; } set { name = value; } }

        private Pen pen = null;
        public Pen Pen { get { return pen != null ? pen : Pens.Transparent; } set { pen = value; } }

        protected PointStyle pointStyle = PointStyle.Square;
        public PointStyle PointStyle { get { return pointStyle; } set { pointStyle = value; } }

        public abstract List<PointF[]> Evaluate(double x0, double x1);

        public void Paint(Matrix2D T, double x0, double x1, Graphics G)
        {
            Pen pen = Pen;
            List<PointF[]> points = Evaluate(x0, x1);
            foreach (PointF[] i in points)
            {
                T.TransformPoints(i);
                G.DrawLines(pen, i);
            }
        }

        protected static float ToFloat(double x)
        {
            if (x > 1e6)
                return 1e6f;
            else if (x < -1e6)
                return -1e6f;
            else
                return (float)x;
        }
    }

    /// <summary>
    /// Data series derived from a lambda function.
    /// </summary>
    public class Function : Series
    {
        protected Func<double, double> f;
        public Function(Func<double, double> f) { this.f = f; }

        public override List<PointF[]> Evaluate(double x0, double x1)
        {
            int N = 2048;

            List<PointF[]> points = new List<PointF[]>();

            List<PointF> run = new List<PointF>();
            for (int i = 0; i <= N; ++i)
            {
                double x = ((x1 - x0) * i) / N + x0;
                float fx = ToFloat(f(x));

                if (double.IsNaN(fx) || float.IsInfinity(fx))
                {
                    if (run.Count > 1)
                        points.Add(run.ToArray());
                    run.Clear();
                }
                else
                {
                    run.Add(new PointF((float)x, fx));
                }
            }
            if (run.Count > 1)
                points.Add(run.ToArray());

            return points;
        }
    }

    /// <summary>
    /// Explicit point list.
    /// </summary>
    public class Scatter : Series
    {
        protected PointF[] points;
        public Scatter(KeyValuePair<double, double>[] Points)
        {
            points = Points.Select(i => new PointF((float)i.Key, ToFloat(i.Value))).ToArray();
        }

        public override List<PointF[]> Evaluate(double x0, double x1)
        {
            return new List<PointF[]>() { points.ToArray() };
        }
    }
}
