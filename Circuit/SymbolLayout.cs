using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;

namespace Circuit
{
    public enum ShapeType
    {
        Wire,
        Black,
        Gray,
        Red,
    };

    public enum Alignment
    {
        Near,
        Center,
        Far,
    }
    
    /// <summary>
    /// Target for drawing operations for the symbol.
    /// </summary>
    public interface ISymbolDrawing
    {
        void DrawRectangle(ShapeType Type, Point x1, Point x2);
        void DrawLine(ShapeType Type, Point x1, Point x2);
        void DrawEllipse(ShapeType Type, Point x1, Point x2);
        void DrawText(string S, Point x, Alignment Horizontal, Alignment Vertical);
        void DrawLines(ShapeType Type, IEnumerable<Point> x);
    }
    
    /// <summary>
    /// 
    /// </summary>
    public class SymbolLayout
    {
        protected ISymbolDrawing drawing = null;

        public SymbolLayout() { }
        public SymbolLayout(ISymbolDrawing Drawing) { drawing = Drawing; }
     
        protected Coord x1 = new Coord(int.MaxValue, int.MaxValue);
        protected Coord x2 = new Coord(int.MinValue, int.MinValue);
        public Coord LowerBound { get { return x1; } }
        public Coord UpperBound { get { return x2; } }

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
        // Get the position of the terminal in this symbol.
        public Coord MapTerminal(Terminal T) { return terminals[T]; }
        // Add a terminal to the schematic.
        public void AddTerminal(Terminal T, Coord x) { terminals[T] = x; }
        
        // Add shapes to the schematic.
        public void AddRectangle(ShapeType Type, Coord x1, Coord x2) 
        {
            InBounds(x1, x2);
            DrawRectangle(Type, x1, x2); 
        }
        public void AddCircle(ShapeType Type, Coord x, int r)
        {
            InBounds(x - r, x + r);
            DrawEllipse(Type, x - r, x + r);
        }
        public void AddLine(ShapeType Type, Coord x1, Coord x2)
        {
            InBounds(x1, x2);
            DrawLine(Type, x1, x2);
        }
        public void AddLines(ShapeType Type, IEnumerable<Coord> Points)
        {
            InBounds(Points);

            DrawLines(Type, Points.Select(i => (Point)i));
        }
        public void AddLines(ShapeType Type, params Coord[] Points) { AddLines(Type, Points.AsEnumerable()); }
        public void AddLoop(ShapeType Type, IEnumerable<Coord> Points)
        {
            AddLines(Type, Points);
            AddLine(Type, Points.First(), Points.Last());
        }
        public void AddLoop(ShapeType Type, params Coord[] Points) { AddLoop(Type, Points.AsEnumerable()); }
        

        // Add a wire to the schematic.
        public void AddWire(IEnumerable<Coord> Points) 
        {
            foreach (var i in Points.Zip(Points.Skip(1), (a, b) => new[] { a, b }))
                DrawLine(ShapeType.Wire, i[0], i[1]);
            InBounds(Points);
        }
        public void AddWire(params Coord[] Points) { AddWire(Points.AsEnumerable()); }
        public void AddWire(Terminal T, IEnumerable<Coord> x) { AddWire(new Coord[] { terminals[T] }.Concat(x)); }
        public void AddWire(Terminal T, params Coord[] x) { AddWire(T, x.AsEnumerable()); }

        public void AddWire(IEnumerable<Terminal> Terminals) { AddWire(Terminals.Select(i => terminals[i])); }
        public void AddWire(params Terminal[] Terminals) { AddWire(Terminals.Select(i => terminals[i])); }
        
        // Raw drawing functions. These functions don't update the bounds.
        public void DrawLine(ShapeType Type, Point x1, Point x2) { if (drawing != null) drawing.DrawLine(Type, x1, x2); }
        public void DrawRectangle(ShapeType Type, Point x1, Point x2) { if (drawing != null) drawing.DrawRectangle(Type, x1, x2); }
        public void DrawEllipse(ShapeType Type, Point x1, Coord x2) { if (drawing != null) drawing.DrawEllipse(Type, x1, x2); }
        public void DrawText(string S, Point x, Alignment Horizontal, Alignment Vertical) { if (drawing != null) drawing.DrawText(S, x, Horizontal, Vertical); }
        public void DrawText(string S, Point x) { DrawText(S, x, Alignment.Near, Alignment.Near); }
        public void DrawLines(ShapeType Type, IEnumerable<Point> x) { if (drawing != null) drawing.DrawLines(Type, x); }

        // Add common shapes to the schematic.
        public void DrawArrow(ShapeType Type, Coord x1, Coord x2, double dh)
        {
            Point dy = new Point((x2.x - x1.x) * dh, (x2.y - x1.y) * dh);
            Point dx = new Point(-dy.y, dy.x);

            DrawLine(Type, x1, x2);
            DrawLine(Type, (Point)x2, new Point(x2.x + dx.x - dy.x, x2.y + dx.y - dy.y));
            DrawLine(Type, (Point)x2, new Point(x2.x - dx.x - dy.x, x2.y - dx.y - dy.y));
        }
        public void DrawPositive(ShapeType Type, Coord x)
        {
            DrawLine(Type, new Point(x.x - 1.5, x.y), new Point(x.x + 1.5, x.y));
            DrawLine(Type, new Point(x.x, x.y - 1.5), new Point(x.x, x.y + 1.5));
        }
        public void DrawNegative(ShapeType Type, Coord x)
        {
            DrawLine(Type, new Point(x.x - 1.5, x.y), new Point(x.x + 1.5, x.y));
        }

        // Add a parametric function to the schematic.
        public void DrawFunction(ShapeType Type, Func<double, double> xt, Func<double, double> yt, double t1, double t2, int N)
        {
            Point[] Points = new Point[N + 1];

            double dt = (t2 - t1) / (double)N;
            for (int i = 0; i <= N; ++i)
            {
                double t = t1 + i * dt;
                Points[i] = new Point(xt(t), yt(t));
            }

            DrawLines(Type, Points);
        }
        public void DrawFunction(ShapeType Type, Func<double, double> xt, Func<double, double> yt, double t1, double t2) { DrawFunction(Type, xt, yt, t1, t2, 16); }
    }
}
