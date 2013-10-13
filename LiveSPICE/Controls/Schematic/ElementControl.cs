using System;
using System.Diagnostics;
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

namespace LiveSPICE
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

        protected static Pen HighlightPen = new Pen(Brushes.Gray, 0.5f) { DashStyle = DashStyles.Dash };
        protected static Pen SelectedPen = new Pen(Brushes.Blue, 0.5f) { DashStyle = DashStyles.Dash };
        
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
                if (highlighted) UpdateToolTip();
                InvalidateVisual();
            }
        }

        protected Circuit.Element element;

        protected ElementControl(Circuit.Element E)
        {
            Debug.Assert(element.Tag == null);

            element = E;
            element.Tag = this;
            element.LayoutChanged += OnLayoutChanged;

            OnLayoutChanged(null, null);

            UseLayoutRounding = true;
            Background = Brushes.Transparent;

            foreach (Circuit.Terminal i in E.Terminals)
                i.ConnectionChanged += (x, y) => InvalidateVisual();
        }

        protected virtual void UpdateToolTip() { }
        
        protected void OnLayoutChanged(object sender, EventArgs e)
        {
            Circuit.Coord lb = element.LowerBound;
            Circuit.Coord ub = element.UpperBound;

            Canvas.SetLeft(this, lb.x);
            Canvas.SetTop(this, lb.y);

            Width = ub.x - lb.x;
            Height = ub.y - lb.y;

            InvalidateVisual();
        }

        public static ElementControl New(Circuit.Element E)
        {
            if (E is Circuit.Wire)
                return new WireControl((Circuit.Wire)E);
            else if (E is Circuit.Symbol)
                return new SymbolControl((Circuit.Symbol)E);
            else
                throw new NotImplementedException();
        }

        protected static Point ToPoint(Circuit.Coord x) { return new Point(x.x, x.y); }

        public static double TerminalSize = 3.0;
        public static double EdgeThickness = 1.0;
        public static GuidelineSet Guidelines = new GuidelineSet(new double[] { EdgeThickness / 2 }, new double[] { EdgeThickness / 2 });

        public static Brush WireBrush = Brushes.DarkBlue;
        public static Pen WirePen = new Pen(WireBrush, EdgeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        public static Pen TerminalPen = new Pen(Brushes.Black, EdgeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };

        public static Pen BlackPen = new Pen(Brushes.Black, EdgeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        public static Pen GrayPen = new Pen(Brushes.Gray, EdgeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        public static Pen RedPen = new Pen(Brushes.Red, EdgeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        public static Pen GreenPen = new Pen(Brushes.Lime, EdgeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        public static Pen BluePen = new Pen(Brushes.Blue, EdgeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        public static Pen YellowPen = new Pen(Brushes.Yellow, EdgeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        public static Pen CyanPen = new Pen(Brushes.Cyan, EdgeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        public static Pen MagentaPen = new Pen(Brushes.Magenta, EdgeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };

        public static void DrawTerminal(DrawingContext Context, Point x, bool Connected)
        {
            Vector dx = new Vector(TerminalSize / 2, TerminalSize / 2);
            Context.DrawRectangle(null, MapToPen(Connected ? Circuit.EdgeType.Wire : Circuit.EdgeType.Red), new Rect(x - dx, x + dx));
        }

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
                default: throw new ArgumentException();
            }
        }

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
