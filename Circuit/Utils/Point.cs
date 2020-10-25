using System;

namespace Circuit
{
    /// <summary>
    /// General 2D point.
    /// </summary>
    public struct Point
    {
        public double x, y;

        public Point(double x, double y) { this.x = x; this.y = y; }
        public override int GetHashCode() { return x.GetHashCode() ^ y.GetHashCode(); }
        public bool Equals(Point p) { return x == p.x && y == p.y; }
        public override bool Equals(object obj)
        {
            if (obj is Point)
                return Equals((Point)obj);
            else
                return false;
        }
        public override string ToString() { return x.ToString() + "," + y.ToString(); }

        public static implicit operator Point(Coord x) { return new Point(x.x, x.y); }

        public static Point Round(Point x) { return new Point(Math.Round(x.x), Math.Round(x.y)); }
        public static Point Floor(Point x) { return new Point(Math.Floor(x.x), Math.Floor(x.y)); }
        public static Point Ceiling(Point x) { return new Point(Math.Ceiling(x.x), Math.Ceiling(x.y)); }

        public static double operator *(Point l, Point r) { return l.x * r.x + l.y * r.y; }
        public static Point operator +(Point l, Point r) { return new Point(l.x + r.x, l.y + r.y); }
        public static Point operator -(Point l, Point r) { return new Point(l.x - r.x, l.y - r.y); }
        public static Point operator +(Point l, double r) { return new Point(l.x + r, l.y + r); }
        public static Point operator -(Point l, double r) { return new Point(l.x - r, l.y - r); }
        public static Point operator *(Point l, double r) { return new Point(l.x * r, l.y * r); }
        public static Point operator /(Point l, double r) { return new Point(l.x / r, l.y / r); }
        public static Point operator *(double l, Point r) { return new Point(l * r.x, l * r.y); }
        public static Point operator /(double l, Point r) { return new Point(l / r.x, l / r.y); }
        public static Point operator -(Point x) { return new Point(-x.x, -x.y); }

        public static bool operator ==(Point l, Point r) { return l.x == r.x && l.y == r.y; }
        public static bool operator !=(Point l, Point r) { return l.x != r.x || l.y != r.y; }

        public static double Abs(Point x) { return Math.Sqrt(x.x * x.x + x.y * x.y); }
    }
}
