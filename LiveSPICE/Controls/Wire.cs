using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using SyMath;

namespace LiveSPICE
{
    public class Wire : Element
    {
        static Wire()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Wire), new FrameworkPropertyMetadata(typeof(Wire)));
        }
        
        public Wire() { }

        public void SetWire(Point A, Point B)
        {
            X = (int)Math.Round(Math.Min(A.X, B.X));
            Y = (int)Math.Round(Math.Min(A.Y, B.Y));

            Width = Math.Abs(B.X - A.X);
            Height = Math.Abs(B.Y - A.Y);

            UpdateLayout();
        }

        public Point A { get { return Position; } }
        public Point B { get { return Position + (Vector)Size; } }

        protected Circuit.Node node;
        public Circuit.Node Node { get { return node; } set { node = value; } }

        public override XElement Serialize()
        {
            XElement X = base.Serialize();
            X.SetAttributeValue("Size", Size);
            return X;
        }

        protected override void Deserialize(XElement X)
        {
            Size = Vector.Parse(X.Attribute("Size").Value);
        }

        protected override void OnRender(DrawingContext dc)
        {
            dc.PushGuidelineSet(Guidelines);

            if (Selected)
                dc.DrawLine(SelectedWirePen, new Point(0, 0), new Point(ActualWidth, ActualHeight));
            else if (Highlighted)
                dc.DrawLine(HighlightedWirePen, new Point(0, 0), new Point(ActualWidth, ActualHeight));
            else
                dc.DrawLine(WirePen, new Point(0, 0), new Point(ActualWidth, ActualHeight));

            dc.Pop();
        }

        public override void RotateAround(int Delta, Point Around)
        {
            Point a = A, b = B;

            a = RotateAround(a, Delta, Around);
            b = RotateAround(b, Delta, Around);

            SetWire(a, b);
        }

        public override void FlipOver(double y)
        {
            Point a = A, b = B;

            a.Y += 2 * (y - a.Y);
            b.Y += 2 * (y - b.Y);

            SetWire(a, b);
        }
        
        // http://www.bryceboe.com/2006/10/23/line-segment-intersection-algorithm/
        private static bool Ccw(Point A, Point B, Point C) { return (C.Y - A.Y) * (B.X - A.X) >= (B.Y - A.Y) * (C.X - A.X); }
        public static bool SegmentsIntersect(Point A, Point B, Point C, Point D) { return Ccw(A, C, D) != Ccw(B, C, D) && Ccw(A, B, C) != Ccw(A, B, D); }

        private static bool PointInRect(Point A, Point x1, Point x2)
        {
            return (((x1.X <= A.X && A.X <= x2.X) || (x2.X <= A.X && A.X <= x1.X)) &&
                    ((x1.Y <= A.Y && A.Y <= x2.Y) || (x2.Y <= A.Y && A.Y <= x1.Y)));
        }

        public static bool PointOnLine(Point x, Point x1, Point x2) { return (x2.X - x1.X) * (x.Y - x1.Y) == (x.X - x1.X) * (x2.Y - x1.Y); }
        public static bool PointOnSegment(Point x, Point x1, Point x2) { return PointInRect(x, x1, x2) && PointOnLine(x, x1, x2); }

        public override bool Intersects(Point x1, Point x2)
        {
            Point a = A, b = B;

            if (PointInRect(a, x1, x2)) return true;
            if (PointInRect(b, x1, x2)) return true;

            if (PointOnSegment(x1, a, b)) return true;
            if (PointOnSegment(x2, a, b)) return true;

            Point x2y1 = new Point(x2.X, x1.Y);
            if (SegmentsIntersect(a, b, x1, x2y1)) return true;
            if (SegmentsIntersect(a, b, x2, x2y1)) return true;
            Point x1y2 = new Point(x1.X, x2.Y);
            if (SegmentsIntersect(a, b, x1, x1y2)) return true;
            if (SegmentsIntersect(a, b, x2, x1y2)) return true;

            return false;
        }

        public bool ConnectsTo(Point Terminal) { return PointOnSegment(Terminal, A, B); }
        public bool ConnectsTo(Point WireA, Point WireB)
        {
            Point a = A, b = B;
            return PointOnSegment(WireA, a, b) || PointOnSegment(WireA, a, b) ||
                PointOnSegment(a, WireA, WireB) || PointOnSegment(b, WireA, WireB);
        }

        protected static Pen SelectedWirePen = new Pen(Brushes.Blue, EdgeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        protected static Pen HighlightedWirePen = new Pen(Brushes.Gray, EdgeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
    }
}
