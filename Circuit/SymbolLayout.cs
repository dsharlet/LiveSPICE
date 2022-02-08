using System;
using System.Collections.Generic;
using System.Linq;

namespace Circuit
{
    public enum EdgeType
    {
        Wire,
        Black,
        Gray,
        Red,
        Green,
        Blue,
        Yellow,
        Cyan,
        Magenta,
        Orange,
    };

    public enum Alignment
    {
        Near,
        Center,
        Far,
    }

    public enum Size
    {
        Small,
        Normal,
        Large,
    }

    public enum Direction
    {
        Clockwise,
        Counterclockwise
    }

    /// <summary>
    /// 
    /// </summary>
    public class SymbolLayout
    {
        protected Coord x1 = new Coord(int.MaxValue, int.MaxValue);
        protected Coord x2 = new Coord(int.MinValue, int.MinValue);
        public Coord LowerBound { get { return x1; } }
        public Coord UpperBound { get { return x2; } }

        public Coord Size { get { return UpperBound - LowerBound; } }
        public int Width { get { return x2.x - x1.x; } }
        public int Height { get { return x2.y - x1.y; } }

        // Include the points in x inside the bounds of this symbol.
        public void InBounds(IEnumerable<Coord> x)
        {
            foreach (Coord i in x)
            {
                x1.x = Math.Min(x1.x, i.x); x1.y = Math.Min(x1.y, i.y);
                x2.x = Math.Max(x2.x, i.x); x2.y = Math.Max(x2.y, i.y);
            }
        }
        public void InBounds(params Coord[] x) { InBounds(x.AsEnumerable()); }

        protected Dictionary<Terminal, Coord> terminals = new Dictionary<Terminal, Coord>();
        /// <summary>
        /// The terminals in this layout.
        /// </summary>
        public IEnumerable<Terminal> Terminals { get { return terminals.Keys; } }
        /// <summary>
        /// Get the position of the terminal in this symbol.
        /// </summary>
        /// <param name="T"></param>
        /// <returns></returns>
        public Coord MapTerminal(Terminal T) { return terminals[T]; }
        /// <summary>
        /// Add a terminal followed by a wire to the schematic.
        /// </summary>
        /// <param name="T"></param>
        /// <param name="x"></param>
        public void AddTerminal(Terminal T, params Coord[] x) { terminals[T] = x[0]; AddWire(x); }

        // Add shapes to the schematic.
        public void AddRectangle(EdgeType Type, Coord x1, Coord x2)
        {
            InBounds(x1, x2);
            DrawRectangle(Type, x1, x2);
        }
        public void AddCircle(EdgeType Type, Coord x, int r)
        {
            InBounds(x - r, x + r);
            DrawEllipse(Type, x - r, x + r);
        }
        public void AddLine(EdgeType Type, Coord x1, Coord x2)
        {
            InBounds(x1, x2);
            DrawLine(Type, x1, x2);
        }
        public void AddCurve(EdgeType Type, IEnumerable<Coord> Points)
        {
            InBounds(Points);

            DrawCurve(Type, Points.Select(i => (Point)i));
        }

        internal void DrawArc(EdgeType type, Coord center, double radius, double startAngle, double endAngle, Direction direction = Direction.Clockwise)
        {
            arcs.Add(new Arc(type, center, radius, startAngle, endAngle, direction));
        }

        public void AddCurve(EdgeType Type, params Coord[] Points) { AddCurve(Type, Points.AsEnumerable()); }
        public void AddLoop(EdgeType Type, IEnumerable<Coord> Points)
        {
            AddCurve(Type, Points);
            AddLine(Type, Points.First(), Points.Last());
        }
        public void AddLoop(EdgeType Type, params Coord[] Points) { AddLoop(Type, Points.AsEnumerable()); }


        // Add a wire to the schematic.
        public void AddWire(IEnumerable<Coord> Points)
        {
            foreach (var i in Points.Zip(Points.Skip(1), (a, b) => new[] { a, b }))
                DrawLine(EdgeType.Wire, i[0], i[1]);
            InBounds(Points);
        }
        public void AddWire(params Coord[] Points) { AddWire(Points.AsEnumerable()); }
        public void AddWire(Terminal T, IEnumerable<Coord> x) { AddWire(new Coord[] { terminals[T] }.Concat(x)); }
        public void AddWire(Terminal T, params Coord[] x) { AddWire(T, x.AsEnumerable()); }

        public void AddWire(IEnumerable<Terminal> Terminals) { AddWire(Terminals.Select(i => terminals[i])); }
        public void AddWire(params Terminal[] Terminals) { AddWire(Terminals.Select(i => terminals[i])); }

        public class Shape
        {
            private EdgeType edge;
            private bool fill;
            private Point _x1, _x2;

            public EdgeType Edge { get { return edge; } }
            public bool Fill { get { return fill; } }
            public Point x1 { get { return _x1; } }
            public Point x2 { get { return _x2; } }

            public Shape(EdgeType Edge, Point x1, Point x2, bool Fill)
            {
                edge = Edge;
                fill = Fill;
                _x1 = x1;
                _x2 = x2;
            }
        }

        public class Text
        {
            private Func<string> s;
            private Point _x;
            private Alignment halign, valign;
            private Size size;

            public string String { get { return s(); } }
            public Point x { get { return _x; } }
            public Alignment HorizontalAlign { get { return halign; } }
            public Alignment VerticalAlign { get { return valign; } }
            public Size Size { get { return size; } set { size = value; } }

            public Text(Func<string> String, Point x, Alignment HorizontalAlign, Alignment VerticalAlign, Size Size)
            {
                s = String;
                _x = x;
                halign = HorizontalAlign;
                valign = VerticalAlign;
                size = Size;
            }
        }

        public class Curve
        {
            private EdgeType edge;
            private Point[] _x;

            public EdgeType Edge { get { return edge; } }
            public Point[] x { get { return _x; } }

            public Curve(EdgeType Edge, Point[] x)
            {
                _x = x;
                edge = Edge;
            }
        }


        // Drawings in this layout.
        private List<Shape> lines = new List<Shape>();
        private List<Shape> rectangles = new List<Shape>();
        private List<Shape> ellipses = new List<Shape>();
        private List<Text> texts = new List<Text>();
        private List<Curve> curves = new List<Curve>();
        private readonly List<Arc> arcs = new List<Arc>();

        public IReadOnlyCollection<Shape> Lines => lines;
        public IReadOnlyCollection<Shape> Rectangles => rectangles;
        public IReadOnlyCollection<Shape> Ellipses => ellipses;
        public IReadOnlyCollection<Text> Texts => texts;
        public IReadOnlyCollection<Curve> Curves => curves;
        public IReadOnlyCollection<Arc> Arcs => arcs;

        // Raw drawing functions. These functions don't update the bounds.
        public void DrawLine(EdgeType Type, Point x1, Point x2) { lines.Add(new Shape(Type, x1, x2, false)); }
        public void DrawRectangle(EdgeType Type, Point x1, Point x2, bool Fill) { rectangles.Add(new Shape(Type, x1, x2, Fill)); }
        public void DrawRectangle(EdgeType Type, Point x1, Point x2) { DrawRectangle(Type, x1, x2, false); }
        public void DrawEllipse(EdgeType Type, Point x1, Point x2, bool Fill) { ellipses.Add(new Shape(Type, x1, x2, Fill)); }
        public void DrawEllipse(EdgeType Type, Point x1, Point x2) { DrawEllipse(Type, x1, x2, false); }
        public void DrawText(Func<string> S, Point x, Alignment Horizontal, Alignment Vertical, Size Size) { texts.Add(new Text(S, x, Horizontal, Vertical, Size)); }
        public void DrawText(Func<string> S, Point x, Alignment Horizontal, Alignment Vertical) { DrawText(S, x, Horizontal, Vertical, global::Circuit.Size.Normal); }
        public void DrawText(Func<string> S, Point x) { DrawText(S, x, Alignment.Near, Alignment.Near); }
        public void DrawCurve(EdgeType Type, IEnumerable<Point> x) { curves.Add(new Curve(Type, x.ToArray())); }

        // Add common shapes to the schematic.
        public void DrawArrow(EdgeType Type, Coord x1, Coord x2, double hw, double hl)
        {
            Point dy = new Point((x2.x - x1.x) * hl, (x2.y - x1.y) * hl);
            Point dx = new Point((x1.y - x2.y) * hw, (x2.x - x1.x) * hw);

            DrawLine(Type, x1, x2);
            DrawLine(Type, (Point)x2, new Point(x2.x + dx.x - dy.x, x2.y + dx.y - dy.y));
            DrawLine(Type, (Point)x2, new Point(x2.x - dx.x - dy.x, x2.y - dx.y - dy.y));
        }
        public void DrawArrow(EdgeType Type, Coord x1, Coord x2, double dh) { DrawArrow(Type, x1, x2, dh, dh); }
        public void DrawPositive(EdgeType Type, Coord x)
        {
            DrawLine(Type, new Point(x.x - 1.5, x.y), new Point(x.x + 1.5, x.y));
            DrawLine(Type, new Point(x.x, x.y - 1.5), new Point(x.x, x.y + 1.5));
        }
        public void DrawNegative(EdgeType Type, Coord x)
        {
            DrawLine(Type, new Point(x.x - 1.5, x.y), new Point(x.x + 1.5, x.y));
        }

        // Add a parametric function to the schematic.
        public void DrawFunction(EdgeType Type, Func<double, double> xt, Func<double, double> yt, double t1, double t2, int N)
        {
            Point[] Points = new Point[N + 1];

            double dt = (t2 - t1) / (double)N;
            for (int i = 0; i <= N; ++i)
            {
                double t = t1 + i * dt;
                Points[i] = new Point(xt(t), yt(t));
            }

            DrawCurve(Type, Points);
        }
        public void DrawFunction(EdgeType Type, Func<double, double> xt, Func<double, double> yt, double t1, double t2) { DrawFunction(Type, xt, yt, t1, t2, 16); }

        public struct Arc
        {
            public EdgeType Type { get; set; }
            public Point Center { get; set; }
            public double Radius { get; set; }
            public double StartAngle { get; set; }
            public double EndAngle { get; set; }
            public Direction Direction { get; set; }

            public Arc(EdgeType type, Coord center, double radius, double startAngle, double endAngle, Direction direction)
            {
                Type = type;
                Center = center;
                Radius = radius;
                StartAngle = startAngle;
                EndAngle = endAngle;
                Direction = direction;
            }
        }
    }
}
