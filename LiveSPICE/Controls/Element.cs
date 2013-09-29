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
using Circuit;

namespace LiveSPICE
{
    /// <summary>
    /// Base class for schematic elements.
    /// </summary>
    public class Element : Control
    {
        protected static double EdgeThickness = 1.0;
        protected static Pen HighlightPen = new Pen(Brushes.Gray, 0.5f) { DashStyle = DashStyles.Dash };
        protected static Pen SelectedPen = new Pen(Brushes.Blue, 0.5f) { DashStyle = DashStyles.Dash };
        protected static GuidelineSet Guidelines = new GuidelineSet(new double[] { EdgeThickness / 2 }, new double[] { EdgeThickness / 2 });

        private Pen pen = null;
        public Pen Pen { get { return pen; } set { pen = value; InvalidateVisual(); } }

        public Point Position { get { return new Point(X, Y); } set { X = (int)Math.Round(value.X); Y = (int)Math.Round(value.Y); } }
        public Vector Size { get { return new Vector(ActualWidth, ActualHeight); } set { Width = value.X; Height = value.Y; InvalidateMeasure(); } }

        protected List<EventHandler> layoutChanged = new List<EventHandler>();
        public event EventHandler LayoutChanged { add { layoutChanged.Add(value); } remove { layoutChanged.Remove(value); } }
        protected void OnLayoutChanged()
        {
            EventArgs args = new EventArgs();
            foreach (EventHandler i in layoutChanged)
                i(this, args);
        }

        public int X { get { return (int)Math.Round(Canvas.GetLeft(this)); } set { Canvas.SetLeft(this, value); OnLayoutChanged(); } }
        public int Y { get { return (int)Math.Round(Canvas.GetTop(this)); } set { Canvas.SetTop(this, value); OnLayoutChanged(); } }

        static Element()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Element), new FrameworkPropertyMetadata(typeof(Element)));
        }

        public Element()
        {
            UseLayoutRounding = true;
        }

        public virtual XElement Serialize()
        {
            XElement X = new XElement("Element");
            X.SetAttributeValue("ElementType", GetType().AssemblyQualifiedName);
            X.SetAttributeValue("Position", Position);
            return X;
        }

        protected virtual void Deserialize(XElement X) { }

        public static Element Deserialize(Schematic Target, XElement X)
        {
            Type T = Type.GetType(X.Attribute("ElementType").Value);
            Element E = (Element)Activator.CreateInstance(T);
            E.Deserialize(X);
            Target.Add(E);
            E.Position = Point.Parse(X.Attribute("Position").Value);
            return E;
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

        public virtual bool Intersects(Point x1, Point x2)
        { 
            return new Rect(x1, x2).IntersectsWith(new Rect(Position, Size));
        }

        public virtual void Move(Vector dx) { Position += dx; }
        public virtual void RotateAround(int Delta, Point x) { }
        public virtual void FlipOver(double x) { }
        
        protected static Point RotateAround(Point x, int Delta, Point Around)
        {
            Vector dx = x - Around;

            double Sin = Math.Round(Math.Sin(Delta * Math.PI / 2));
            double Cos = Math.Round(Math.Cos(Delta * Math.PI / 2));

            Vector X = new Vector(Cos, Sin);
            Vector Y = new Vector(-Sin, Cos);

            return new Point(Vector.Multiply(dx, X) + Around.X, Vector.Multiply(dx, Y) + Around.Y);
        }

        protected static Pen WirePen = new Pen(Brushes.Black, EdgeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        protected static Pen TerminalPen = new Pen(Brushes.Black, EdgeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        protected static Pen BlackPen = new Pen(Brushes.Black, EdgeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        protected static Pen GrayPen = new Pen(Brushes.Gray, EdgeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        protected static Pen RedPen = new Pen(Brushes.Red, EdgeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };

        protected Pen MapToPen(ShapeType Edge)
        {
            if (pen != null)
                return pen;
            switch (Edge)
            {
                case ShapeType.Wire: return WirePen;
                case ShapeType.Black: return BlackPen;
                case ShapeType.Gray: return GrayPen;
                case ShapeType.Red: return RedPen;
                default: throw new ArgumentException();
            }
        }

        protected static double MapAlignment(Alignment Align)
        {
            switch (Align)
            {
                case Alignment.Near: return 0.0;
                case Alignment.Center: return 0.5;
                case Alignment.Far: return 1.0;
                default: throw new ArgumentException();
            }
        }
    }
}
