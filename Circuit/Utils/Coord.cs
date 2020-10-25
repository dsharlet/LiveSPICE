using System;

namespace Circuit
{
    /// <summary>
    /// 2D integer coordinate.
    /// </summary>
    public struct Coord : IEquatable<Coord>
    {
        public int x;
        public int y;

        public Coord(int x, int y) { this.x = x; this.y = y; }

        public override int GetHashCode() { return x.GetHashCode() ^ y.GetHashCode(); }
        public bool Equals(Coord p) { return x == p.x && y == p.y; }
        public override bool Equals(object obj)
        {
            if (obj is Coord)
                return Equals((Coord)obj);
            else
                return false;
        }
        public override string ToString() { return x.ToString() + "," + y.ToString(); }

        public static Coord Parse(string s)
        {
            string[] x = s.Split(',');
            return new Coord(int.Parse(x[0]), int.Parse(x[1]));
        }

        public static explicit operator Coord(Point x) { return new Coord((int)x.x, (int)x.y); }

        public static int operator *(Coord l, Coord r) { return l.x * r.x + l.y * r.y; }
        public static Coord operator +(Coord l, Coord r) { return new Coord(l.x + r.x, l.y + r.y); }
        public static Coord operator -(Coord l, Coord r) { return new Coord(l.x - r.x, l.y - r.y); }
        public static Coord operator +(Coord l, int r) { return new Coord(l.x + r, l.y + r); }
        public static Coord operator -(Coord l, int r) { return new Coord(l.x - r, l.y - r); }
        public static Coord operator *(Coord l, int r) { return new Coord(l.x * r, l.y * r); }
        public static Coord operator /(Coord l, int r) { return new Coord(l.x / r, l.y / r); }
        public static Coord operator *(int l, Coord r) { return new Coord(l * r.x, l * r.y); }
        public static Coord operator /(int l, Coord r) { return new Coord(l / r.x, l / r.y); }
        public static Coord operator -(Coord x) { return new Coord(-x.x, -x.y); }

        public static bool operator ==(Coord l, Coord r) { return l.x == r.x && l.y == r.y; }
        public static bool operator !=(Coord l, Coord r) { return l.x != r.x || l.y != r.y; }

        public static double Abs(Coord x) { return Math.Sqrt(x.x * x.x + x.y * x.y); }
    };
}
