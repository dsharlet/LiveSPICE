using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SchematicControls
{
    /// <summary>
    /// Base class for schematic elements.
    /// </summary>
    public class ElementControl : Control
    {
        static ElementControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ElementControl), new FrameworkPropertyMetadata(typeof(ElementControl)));
        }

        public static Pen HighlightPen = new Pen(Brushes.Gray, 1.0f) { DashStyle = DashStyles.Dash }.GetAsFrozen() as Pen;
        public static Pen SelectedPen = new Pen(Brushes.DodgerBlue, 1.0f) { DashStyle = DashStyles.Dash }.GetAsFrozen() as Pen;

        private Pen pen = null;
        public Pen Pen { get { return pen; } set { pen = value; InvalidateVisual(); } }

        protected bool showTerminals = true;
        public bool ShowTerminals { get { return showTerminals; } set { showTerminals = value; InvalidateVisual(); } }

        private List<EventHandler> selectedChanged = new List<EventHandler>();
        public event EventHandler SelectedChanged { add { selectedChanged.Add(value); } remove { selectedChanged.Remove(value); } }

        private bool selected = false;
        public bool Selected
        {
            get { return selected; }
            set
            {
                if (selected == value) return;

                selected = value;
                InvalidateVisual();
                foreach (EventHandler i in selectedChanged)
                    i(this, new EventArgs());
            }
        }

        private bool highlighted = false;
        public bool Highlighted
        {
            get { return highlighted; }
            set
            {
                if (highlighted == value) return;
                highlighted = value;
                InvalidateVisual();
            }
        }

        protected Circuit.Element element;
        public Circuit.Element Element { get { return element; } }

        protected ElementControl(Circuit.Element E)
        {
            element = E;
            element.Tag = this;

            Background = Brushes.Transparent;

            foreach (Circuit.Terminal i in E.Terminals)
                i.ConnectionChanged += (x, y) => InvalidateVisual();
        }

        public static ElementControl New(Circuit.Element E)
        {
            if (E is Circuit.Wire wire)
                return new WireControl(wire);
            else if (E is Circuit.Symbol symbol)
                return new SymbolControl(symbol);
            else
                throw new NotImplementedException();
        }

        protected static Point ToPoint(Circuit.Coord x) { return new Point(x.x, x.y); }

        public static double TerminalSize = 2.0;
        public static double EdgeThickness = 1.0;

        public static Brush WireBrush = Brushes.DarkBlue;
        public static Pen WirePen = new Pen(WireBrush, EdgeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }.GetAsFrozen() as Pen;
        public static Pen TerminalPen = new Pen(Brushes.Black, EdgeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }.GetAsFrozen() as Pen;

        public static Pen BlackPen = new Pen(Brushes.Black, EdgeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }.GetAsFrozen() as Pen;
        public static Pen GrayPen = new Pen(Brushes.Gray, EdgeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }.GetAsFrozen() as Pen;
        public static Pen RedPen = new Pen(Brushes.Red, EdgeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }.GetAsFrozen() as Pen;
        public static Pen GreenPen = new Pen(Brushes.Lime, EdgeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }.GetAsFrozen() as Pen;
        public static Pen BluePen = new Pen(Brushes.Blue, EdgeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }.GetAsFrozen() as Pen;
        public static Pen YellowPen = new Pen(Brushes.Yellow, EdgeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }.GetAsFrozen() as Pen;
        public static Pen CyanPen = new Pen(Brushes.Cyan, EdgeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }.GetAsFrozen() as Pen;
        public static Pen MagentaPen = new Pen(Brushes.Magenta, EdgeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }.GetAsFrozen() as Pen;
        public static Pen OrangePen = new Pen(Brushes.Orange, EdgeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }.GetAsFrozen() as Pen;

        public static Pen MapToPen(Circuit.EdgeType Edge)
        {
            switch (Edge)
            {
                case Circuit.EdgeType.Wire: return WirePen;
                case Circuit.EdgeType.Black: return BlackPen;
                case Circuit.EdgeType.Gray: return GrayPen;
                case Circuit.EdgeType.Red: return RedPen;
                case Circuit.EdgeType.Green: return GreenPen;
                case Circuit.EdgeType.Blue: return BluePen;
                case Circuit.EdgeType.Yellow: return YellowPen;
                case Circuit.EdgeType.Cyan: return CyanPen;
                case Circuit.EdgeType.Magenta: return MagentaPen;
                case Circuit.EdgeType.Orange: return OrangePen;
                default: throw new ArgumentException();
            }
        }

        public static Brush MapToBrush(Circuit.EdgeType Edge) { return MapToPen(Edge).Brush; }

        public static double MapAlignment(Circuit.Alignment Align)
        {
            switch (Align)
            {
                case Circuit.Alignment.Near: return 0.0;
                case Circuit.Alignment.Center: return 0.5;
                case Circuit.Alignment.Far: return 1.0;
                default: throw new ArgumentException();
            }
        }
    }
}
