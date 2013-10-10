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
        
        protected static Pen SelectedWirePen = new Pen(Brushes.Blue, Symbol.EdgeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        protected static Pen HighlightedWirePen = new Pen(Brushes.Gray, Symbol.EdgeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
    }
}
