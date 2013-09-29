using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
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
using System.Reflection;
using Circuit;

namespace LiveSPICE
{
    /// <summary>
    /// Element for a symbol.
    /// </summary>
    public class Symbol : Element, ISymbolDrawing
    {
        protected static double TerminalSize = 5.0;
        
        static Symbol() { DefaultStyleKeyProperty.OverrideMetadata(typeof(Symbol), new FrameworkPropertyMetadata(typeof(Symbol))); }
        
        protected bool showText = true;
        public bool ShowText { get { return showText; } set { showText = value; InvalidateVisual(); } }

        protected bool showTerminals = true;
        public bool ShowTerminals { get { return showTerminals; } set { showTerminals = value; InvalidateVisual(); } }

        protected Matrix transform;
        protected Point origin = new Point(0, 0);
        protected double scale = 1.0;
        protected int rotation = 0;
        protected bool flip = false;

        public int Rotation { get { return rotation; } set { rotation = value; InvalidateMeasure(); UpdateLayout(); OnLayoutChanged(); InvalidateVisual(); } }
        public bool Flip { get { return flip; } set { flip = value; OnLayoutChanged(); InvalidateVisual(); } }

        private Component component;
        protected SymbolLayout symbol;
        public Component Component 
        { 
            get { return component; }
            set
            {
                component = value;
                component.Tag = this;
                component.PropertyChanged += (o, e) => InvalidateVisual();

                symbol = new SymbolLayout(this);
                component.LayoutSymbol(symbol);
                UpdateLayout();
            }
        }

        public Symbol() 
        { 
            //FontFamily = new FontFamily("Courier New");
            //FontSize = 10;
        }
        public Symbol(Component Component) : this() { this.Component = Component; }
        public Symbol(Type T) { Component = (Component)Activator.CreateInstance(T); }

        public override XElement Serialize()
        {
            XElement X = base.Serialize();
            Type T = component.GetType();
            X.SetAttributeValue("Type", T.AssemblyQualifiedName);
            X.SetAttributeValue("Rotation", Rotation);
            X.SetAttributeValue("Flip", Flip);
            foreach (PropertyInfo i in T.GetProperties().Where(i => i.GetCustomAttribute<Circuit.SchematicPersistent>() != null))
            {
                System.ComponentModel.TypeConverter tc = System.ComponentModel.TypeDescriptor.GetConverter(i.PropertyType);
                X.SetAttributeValue(i.Name, tc.ConvertToString(i.GetValue(component)));
            }
            return X;
        }
        protected override void Deserialize(XElement X)
        {
            Type T = Type.GetType(X.Attribute("Type").Value);
            Rotation = int.Parse(X.Attribute("Rotation").Value);
            Flip = bool.Parse(X.Attribute("Flip").Value);
            Component = (Component)Activator.CreateInstance(T);
            foreach (PropertyInfo i in T.GetProperties().Where(i => i.GetCustomAttribute<Circuit.SchematicPersistent>() != null))
            {
                XAttribute attr = X.Attribute(i.Name);
                if (attr != null)
                {
                    System.ComponentModel.TypeConverter tc = System.ComponentModel.TypeDescriptor.GetConverter(i.PropertyType);
                    i.SetValue(component, tc.ConvertFromString(attr.Value));
                }
            }
        }

        public Point MapTerminal(Terminal T) { return MapToPoint(symbol.MapTerminal(T)) + (Vector)Position; }
        
        public override void RotateAround(int Delta, Point Around)
        {
            Point at = Position + Size / 2;
            at = RotateAround(at, Delta, Around);

            Rotation += Delta;
            Position = at - Size / 2;
        }

        public override void FlipOver(double y)
        {
            Point at = Position + Size / 2;
            at.Y += 2 * (y - at.Y);

            Flip = !flip;
            Position = at - Size / 2;
        }
        
        protected override Size MeasureOverride(Size constraint)
        {
            Point b1 = ConvertToPoint(symbol.LowerBound);
            Point b2 = ConvertToPoint(symbol.UpperBound);
            double width = Math.Abs(b2.X - b1.X);
            double height = Math.Abs(b2.Y - b1.Y);
            if (rotation % 2 != 0)
                Swap(ref width, ref height);

            return new Size(
                Math.Min(width, constraint.Width),
                Math.Min(height, constraint.Height));
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            Point b1 = ConvertToPoint(symbol.LowerBound);
            Point b2 = ConvertToPoint(symbol.UpperBound);
            double width = Math.Abs(b2.X - b1.X);
            double height = Math.Abs(b2.Y - b1.Y);
            if (rotation % 2 != 0)
                Swap(ref width, ref height);

            Size size = base.ArrangeOverride(arrangeBounds);
            scale = Math.Min(Math.Min(size.Width / width, size.Height / height), 1.0);
            origin = new Point((b1.X + b2.X) / 2, (b1.Y + b2.Y) / 2);

            return size;
        }

        protected Point MapToPoint(CoordD x) { return transform.Transform(new Point(x.x, x.y)); }

        protected DrawingContext dc;
        protected override void OnRender(DrawingContext drawingContext)
        {
            dc = drawingContext;
            dc.PushGuidelineSet(Guidelines);
            
            transform.SetIdentity();
            transform.Scale(scale, -scale);
            transform.Translate(-origin.X, origin.Y);
            if (flip)
                transform.Scale(1, -1);
            transform.Rotate(rotation * -90);
            transform.Translate(ActualWidth / 2, ActualHeight / 2);
            
            component.LayoutSymbol(symbol);

            double dx = TerminalSize / 2;
            foreach (Terminal i in component.Terminals)
            {
                CoordD x = symbol.MapTerminal(i);
                Point x1 = MapToPoint(new CoordD(x.x - dx, x.y - dx));
                Point x2 = MapToPoint(new CoordD(x.x + dx, x.y + dx));
                dc.DrawRectangle(null, MapToPen(i.ConnectedTo == null ? ShapeType.Red : ShapeType.Black), new Rect(x1, x2));
            }

            Point b1 = MapToPoint(symbol.LowerBound);
            Point b2 = MapToPoint(symbol.UpperBound);

            Rect bounds = new Rect(b1, b2);
            if (Selected)
                dc.DrawRectangle(null, SelectedPen, bounds);
            else if (Highlighted)
                dc.DrawRectangle(null, HighlightPen, bounds);

            dc.Pop();
            dc = null;
        }

        void ISymbolDrawing.DrawRectangle(ShapeType Type, CoordD x1, CoordD x2)
        {
            if (dc == null) return;

            dc.DrawRectangle(null, MapToPen(Type), new Rect(MapToPoint(x1), MapToPoint(x2)));
        }

        void ISymbolDrawing.DrawLine(ShapeType Type, CoordD x1, CoordD x2)
        {
            if (dc == null) return;

            dc.DrawLine(MapToPen(Type), MapToPoint(x1), MapToPoint(x2));
        }

        void ISymbolDrawing.DrawLines(ShapeType Type, IEnumerable<CoordD> x)
        {
            if (dc == null) return;

            IEnumerator<CoordD> e = x.GetEnumerator();
            if (!e.MoveNext())
                return;

            Pen pen = MapToPen(Type);
            Point x1 = MapToPoint(e.Current);
            while (e.MoveNext())
            {
                Point x2 = MapToPoint(e.Current);
                dc.DrawLine(pen, x1, x2);
                x1 = x2;
            }
        }

        void ISymbolDrawing.DrawEllipse(ShapeType Type, CoordD x1, CoordD x2)
        {
            if (dc == null) return;

            Point p1 = MapToPoint(x1);
            Point p2 = MapToPoint(x2);

            dc.DrawEllipse(null, MapToPen(Type), new Point((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2), (p2.X - p1.X) / 2, (p2.Y - p1.Y) / 2);
        }
        
        void ISymbolDrawing.DrawText(string S, CoordD x, Alignment Horizontal, Alignment Vertical)
        {
            if (dc == null || !ShowText) return;

            FormattedText text = new FormattedText(
                S, 
                CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, 
                new Typeface(FontFamily, FontStyle, FontWeight, FontStretch), FontSize * scale, 
                Brushes.Black);

            Point p = MapToPoint(x);
            Vector p1 = MapToPoint(new CoordD(x.x - MapAlignment(Horizontal), x.y + (1 - MapAlignment(Vertical)))) - p;
            Vector p2 = MapToPoint(new CoordD(x.x - (1 - MapAlignment(Horizontal)), x.y + MapAlignment(Vertical))) - p;

            p1.X *= text.Width; p2.X *= text.Width;
            p1.Y *= text.Height; p2.Y *= text.Height;
            
            dc.DrawText(text, new Point(Math.Min(p.X + p1.X, p.X - p2.X), Math.Min(p.Y + p1.Y, p.Y - p2.Y)));
        }

        static void Swap<T>(ref T lhs, ref T rhs)
        {
            T temp = lhs;
            lhs = rhs;
            rhs = temp;
        }

        static Point ConvertToPoint(CoordD x)
        {
            return new Point(x.x, x.y);
        }
    }
}
