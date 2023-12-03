using System.Windows;
using System.Windows.Media;

namespace SchematicControls
{
    public class WireControl : ElementControl
    {
        static WireControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WireControl), new FrameworkPropertyMetadata(typeof(WireControl)));
        }

        protected Circuit.Wire wire;
        public WireControl(Circuit.Wire W) : base(W)
        {
            wire = W;
            MouseMove += (s, e) => ToolTip = wire.Node != null ? wire.Node.ToString() : null;
        }

        // To avoid glitches, draw wire terminals a little smaller than regular terminals.
        public static double WireTerminalSize = TerminalSize * 0.9;

        protected override void OnRender(DrawingContext dc)
        {
            // This isn't pointless, it makes WPF mouse hit tests succeed near the wire instead of exactly on it.
            dc.DrawRectangle(Brushes.Transparent, null, new Rect(-2, -2, ActualWidth + 4, ActualHeight + 4));

            Pen pen;
            if (Selected)
                pen = SelectedWirePen;
            else if (Highlighted)
                pen = HighlightedWirePen;
            else
                pen = WirePen;
            dc.DrawLine(pen, new Point(0, 0), new Point(ActualWidth, ActualHeight));

            // Don't use the pen to draw the terminals, because the terminals tend to get overdrawn by other components.
            Vector dx = new Vector(WireTerminalSize / 2, WireTerminalSize / 2);
            foreach (Point x in new[] { ToPoint(wire.A - wire.LowerBound), ToPoint(wire.B - wire.LowerBound) })
                dc.DrawRectangle(WirePen.Brush, WirePen, new Rect(x - dx, x + dx));
        }

        protected static Pen SelectedWirePen = new Pen(Brushes.DodgerBlue, EdgeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        protected static Pen HighlightedWirePen = new Pen(Brushes.Gray, EdgeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
    }
}
