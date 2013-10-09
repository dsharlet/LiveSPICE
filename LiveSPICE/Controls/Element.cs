using System;
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
    public class Element : Control
    {
        static Element()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Element), new FrameworkPropertyMetadata(typeof(Element)));
        }

        protected static double EdgeThickness = 1.0;
        protected static Pen HighlightPen = new Pen(Brushes.Gray, 0.5f) { DashStyle = DashStyles.Dash };
        protected static Pen SelectedPen = new Pen(Brushes.Blue, 0.5f) { DashStyle = DashStyles.Dash };
        protected static GuidelineSet Guidelines = new GuidelineSet(new double[] { EdgeThickness / 2 }, new double[] { EdgeThickness / 2 });

        protected Circuit.Element element;

        private Pen pen = null;
        public Pen Pen { get { return pen; } set { pen = value; InvalidateVisual(); } }

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

        public Element(Circuit.Element E)
        {
            element = E;
            element.Tag = this;
            element.LayoutChanged += OnLayoutChanged;

            UseLayoutRounding = true;
        }
        
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
                
        protected static Pen WirePen = new Pen(Brushes.Black, EdgeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        protected static Pen TerminalPen = new Pen(Brushes.Black, EdgeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        protected static Pen BlackPen = new Pen(Brushes.Black, EdgeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        protected static Pen GrayPen = new Pen(Brushes.Gray, EdgeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        protected static Pen RedPen = new Pen(Brushes.Red, EdgeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };

        protected Pen MapToPen(Circuit.ShapeType Edge)
        {
            if (pen != null)
                return pen;
            switch (Edge)
            {
                case Circuit.ShapeType.Wire: return WirePen;
                case Circuit.ShapeType.Black: return BlackPen;
                case Circuit.ShapeType.Gray: return GrayPen;
                case Circuit.ShapeType.Red: return RedPen;
                default: throw new ArgumentException();
            }
        }

        protected static double MapAlignment(Circuit.Alignment Align)
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
