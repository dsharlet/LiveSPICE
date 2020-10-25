using System;

namespace Circuit
{
    public class Matrix
    {
        // Columns of the matrix.
        private double[,] m = new double[2, 3];

        public Matrix(double m00, double m01, double m02,
                      double m10, double m11, double m12)
        {
            m[0, 0] = m00; m[0, 1] = m01; m[0, 2] = m02;
            m[1, 0] = m10; m[1, 1] = m11; m[1, 2] = m12;
        }

        public double this[int i, int j]
        {
            get { return m[i, j]; }
            set { m[i, j] = value; }
        }

        public static Matrix Scale(double x, double y)
        {
            return new Matrix(
                x, 0.0, 0.0,
                0.0, y, 0.0);
        }

        public static Matrix Scale(Point x) { return Scale(x.x, x.y); }
        public static Matrix Scale(double x) { return Scale(x, x); }

        public static Matrix Translate(Point x)
        {
            return new Matrix(
                1.0, 0.0, x.x,
                0.0, 1.0, x.y);
        }

        public static Matrix Rotate(double t)
        {
            double c = Math.Cos(t);
            double s = Math.Sin(t);
            return new Matrix(
                c, s, 0.0,
                -s, c, 0.0);
        }

        public static Point operator *(Matrix l, Point r)
        {
            return new Point(
                l[0, 0] * r.x + l[0, 1] * r.y + l[0, 2],
                l[1, 0] * r.x + l[1, 1] * r.y + l[1, 2]);
        }

        public static Matrix operator *(Matrix l, double r)
        {
            return new Matrix(
                l[0, 0] * r, l[0, 1] * r, l[0, 2] * r,
                l[1, 0] * r, l[1, 1] * r, l[1, 2] * r);
        }

        public static Matrix operator /(Matrix l, double r) { return l * (1 / r); }

        public static Matrix operator *(Matrix l, Matrix r)
        {
            return new Matrix(
                l[0, 0] * r[0, 0] + l[0, 1] * r[1, 0],
                l[0, 0] * r[0, 1] + l[0, 1] * r[1, 1],
                l[0, 0] * r[0, 2] + l[0, 1] * r[1, 2] + l[0, 2],
                l[1, 0] * r[0, 0] + l[1, 1] * r[1, 0],
                l[1, 0] * r[0, 1] + l[1, 1] * r[1, 1],
                l[1, 0] * r[0, 2] + l[1, 1] * r[1, 2] + l[1, 2]);
        }

        public static Matrix operator ^(Matrix l, int r)
        {
            if (r == -1)
            {
                double det = l[0, 0] * l[1, 1] - l[0, 1] * l[1, 0];

                return new Matrix(
                    l[1, 1], -l[0, 1], l[0, 1] * l[1, 2] - l[0, 2] * l[1, 1],
                    -l[1, 0], l[0, 0], l[1, 0] * l[0, 2] - l[0, 0] * l[1, 2]) / det;
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
