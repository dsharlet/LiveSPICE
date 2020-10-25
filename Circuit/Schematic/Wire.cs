using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Circuit
{
    /// <summary>
    /// Element for wires.
    /// </summary>
    public class Wire : Element
    {
        protected Conductor conductor;

        public Terminal Anode { get { return conductor.Anode; } }
        public Terminal Cathode { get { return conductor.Cathode; } }

        private Node node = null;
        public Node Node
        {
            get { return node; }
            set { node = value; }
        }

        protected Coord a, b;

        public Coord A
        {
            get { return a; }
            set
            {
                if (a == value) return;
                a = value;
                OnLayoutChanged();
            }
        }
        public Coord B
        {
            get { return b; }
            set
            {
                if (b == value) return;
                b = value;
                OnLayoutChanged();
            }
        }

        public Wire()
        {
            conductor = new Conductor();
            conductor.Tag = this;
        }
        public Wire(Coord A, Coord B) : this()
        {
            a = A;
            b = B;
        }

        public override IEnumerable<Terminal> Terminals { get { return conductor.Terminals; } }
        public override Coord MapTerminal(Terminal T)
        {
            if (T == conductor.Anode) return a;
            else if (T == conductor.Cathode) return b;
            else throw new ArgumentOutOfRangeException("T");
        }

        public bool IsConnectedTo(Coord x) { return PointOnSegment(x, A, B); }

        public bool IsConnectedTo(Wire Other)
        {
            return PointOnSegment(Other.A, A, B) || PointOnSegment(Other.B, A, B) ||
                PointOnSegment(A, Other.A, Other.B) || PointOnSegment(B, Other.A, Other.B);
        }

        public override bool Intersects(Coord x1, Coord x2)
        {
            if (PointInRect(a, x1, x2)) return true;
            if (PointInRect(b, x1, x2)) return true;

            if (PointOnSegment(x1, a, b)) return true;
            if (PointOnSegment(x2, a, b)) return true;

            Coord x2y1 = new Coord(x2.x, x1.y);
            if (SegmentsIntersect(a, b, x1, x2y1)) return true;
            if (SegmentsIntersect(a, b, x2, x2y1)) return true;
            Coord x1y2 = new Coord(x1.x, x2.y);
            if (SegmentsIntersect(a, b, x1, x1y2)) return true;
            if (SegmentsIntersect(a, b, x2, x1y2)) return true;

            return false;
        }

        public override void Move(Coord dx)
        {
            a += dx;
            b += dx;
            OnLayoutChanged();
        }

        public override Coord LowerBound { get { return new Coord(Math.Min(A.x, B.x), Math.Min(A.y, B.y)); } }
        public override Coord UpperBound { get { return new Coord(Math.Max(A.x, B.x), Math.Max(A.y, B.y)); } }

        public override void RotateAround(int dt, Point at)
        {
            a = (Coord)Point.Round(RotateAround(a, dt, at));
            b = (Coord)Point.Round(RotateAround(b, dt, at));
            OnLayoutChanged();
        }

        public override void FlipOver(double y)
        {
            a.y += (int)Math.Round(2 * (y - a.y));
            b.y += (int)Math.Round(2 * (y - b.y));
            OnLayoutChanged();
        }

        public override XElement Serialize()
        {
            XElement X = base.Serialize();
            X.SetAttributeValue("A", a);
            X.SetAttributeValue("B", b);
            return X;
        }

        public new static Element Deserialize(XElement X)
        {
            Coord a = Coord.Parse(X.Attribute("A").Value);
            Coord b = Coord.Parse(X.Attribute("B").Value);
            if (a == b)
                return new Symbol(new Warning(X, "Zero length wire.")) { Position = a };
            return new Wire(a, b);
        }

        public override string ToString()
        {
            return "Wire from " + A.ToString() + " to " + B.ToString();
        }

        // http://www.bryceboe.com/2006/10/23/line-segment-intersection-algorithm/
        private static bool Ccw(Coord A, Coord B, Coord C) { return (C.y - A.y) * (B.x - A.x) >= (B.y - A.y) * (C.x - A.x); }
        public static bool SegmentsIntersect(Coord A, Coord B, Coord C, Coord D) { return Ccw(A, C, D) != Ccw(B, C, D) && Ccw(A, B, C) != Ccw(A, B, D); }

        private static bool PointInRect(Coord A, Coord x1, Coord x2)
        {
            return (((x1.x <= A.x && A.x <= x2.x) || (x2.x <= A.x && A.x <= x1.x)) &&
                    ((x1.y <= A.y && A.y <= x2.y) || (x2.y <= A.y && A.y <= x1.y)));
        }

        public static bool PointOnLine(Coord x, Coord x1, Coord x2) { return (x2.x - x1.x) * (x.y - x1.y) == (x.x - x1.x) * (x2.y - x1.y); }
        public static bool PointOnSegment(Coord x, Coord x1, Coord x2) { return PointInRect(x, x1, x2) && PointOnLine(x, x1, x2); }
    }
}
