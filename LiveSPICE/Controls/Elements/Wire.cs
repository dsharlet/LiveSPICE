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

        protected Circuit.Wire wire;
        public Wire(Circuit.Wire W) : base(W) { wire = W; }

        private static Point ToPoint(Circuit.Coord x) { return new Point(x.x, x.y); }
        protected override void OnRender(DrawingContext dc)
        {
            dc.PushGuidelineSet(Symbol.Guidelines);

            if (Selected)
                dc.DrawLine(SelectedWirePen, new Point(0, 0), new Point(ActualWidth, ActualHeight));
            else if (Highlighted)
                dc.DrawLine(HighlightedWirePen, new Point(0, 0), new Point(ActualWidth, ActualHeight));
            else
                dc.DrawLine(Symbol.WirePen, new Point(0, 0), new Point(ActualWidth, ActualHeight));

            dc.DrawRectangle(Symbol.WireBrush, Symbol.MapToPen(wire.Anode.ConnectedTo != null ? Circuit.EdgeType.Black : Circuit.EdgeType.Red), new Rect(-1, -1, 2, 2));
            dc.DrawRectangle(Symbol.WireBrush, Symbol.MapToPen(wire.Cathode.ConnectedTo != null ? Circuit.EdgeType.Black : Circuit.EdgeType.Red), new Rect(ActualWidth - 1, ActualHeight - 1, 2, 2));

            dc.Pop();
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

        //public override bool Intersects(Point x1, Point x2)
        //{
        //    Point a = A, b = B;

        //    if (PointInRect(a, x1, x2)) return true;
        //    if (PointInRect(b, x1, x2)) return true;

        //    if (PointOnSegment(x1, a, b)) return true;
        //    if (PointOnSegment(x2, a, b)) return true;

        //    Point x2y1 = new Point(x2.X, x1.Y);
        //    if (SegmentsIntersect(a, b, x1, x2y1)) return true;
        //    if (SegmentsIntersect(a, b, x2, x2y1)) return true;
        //    Point x1y2 = new Point(x1.X, x2.Y);
        //    if (SegmentsIntersect(a, b, x1, x1y2)) return true;
        //    if (SegmentsIntersect(a, b, x2, x1y2)) return true;

        //    return false;
        //}

        //public bool ConnectsTo(Point Terminal) { return PointOnSegment(Terminal, A, B); }
        //public bool ConnectsTo(Point WireA, Point WireB)
        //{
        //    Point a = A, b = B;
        //    return PointOnSegment(WireA, a, b) || PointOnSegment(WireA, a, b) ||
        //        PointOnSegment(a, WireA, WireB) || PointOnSegment(b, WireA, WireB);
        //}

        protected static Pen SelectedWirePen = new Pen(Brushes.Blue, Symbol.EdgeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        protected static Pen HighlightedWirePen = new Pen(Brushes.Gray, Symbol.EdgeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
    }
}
