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

namespace LiveSPICE
{
    /// <summary>
    /// Element for a symbol.
    /// </summary>
    public class Symbol : Element, Circuit.ISymbolDrawing
    {
        protected static double TerminalSize = 5.0;
        
        static Symbol() { DefaultStyleKeyProperty.OverrideMetadata(typeof(Symbol), new FrameworkPropertyMetadata(typeof(Symbol))); }
        
        protected bool showText = true;
        public bool ShowText { get { return showText; } set { showText = value; InvalidateVisual(); } }

        protected bool showTerminals = true;
        public bool ShowTerminals { get { return showTerminals; } set { showTerminals = value; InvalidateVisual(); } }

        protected Matrix transform;
        protected double scale = 1.0;
        protected Point origin;

        protected Circuit.Symbol symbol;
        public Circuit.Component Component { get { return symbol.Component; } }

        public Symbol(Circuit.Symbol S) : base(S)
        {
            symbol = S;
            //FontFamily = new FontFamily("Courier New");
            //FontSize = 10;
        }

        public Symbol(Type T) : this(new Circuit.Symbol((Circuit.Component)Activator.CreateInstance(T))) { }

        public Circuit.Symbol GetSymbol() { return symbol; }

        protected override Size MeasureOverride(Size constraint)
        {
            Point b1 = ConvertToPoint(symbol.LowerBound);
            Point b2 = ConvertToPoint(symbol.UpperBound);
            double width = Math.Abs(b2.X - b1.X);
            double height = Math.Abs(b2.Y - b1.Y);

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

            Size size = base.ArrangeOverride(arrangeBounds);
            scale = Math.Min(Math.Min(size.Width / width, size.Height / height), 1.0);
            origin = new Point((b1.X + b2.X) / 2, (b1.Y + b2.Y) / 2);

            return size;
        }

        protected Point MapToPoint(Circuit.Point x) { return transform.Transform(new Point(x.x, x.y)); }

        protected DrawingContext dc;
        protected override void OnRender(DrawingContext drawingContext)
        {
            dc = drawingContext;
            dc.PushGuidelineSet(Guidelines);
            
            transform.SetIdentity();
            transform.Scale(scale, -scale);
            transform.Translate(-origin.X, origin.Y);
            if (((Circuit.Symbol)element).Flip)
                transform.Scale(1, -1);
            transform.Rotate(((Circuit.Symbol)element).Rotation * -90);
            transform.Translate(ActualWidth / 2, ActualHeight / 2);
            
            Circuit.SymbolLayout layout = new Circuit.SymbolLayout(this);
            Circuit.Component component = ((Circuit.Symbol)element).Component;

            component.LayoutSymbol(layout);

            double dx = TerminalSize / 2;
            foreach (Circuit.Terminal i in component.Terminals)
            {
                Circuit.Point x = symbol.MapTerminal(i);
                Point x1 = MapToPoint(new Circuit.Point(x.x - dx, x.y - dx));
                Point x2 = MapToPoint(new Circuit.Point(x.x + dx, x.y + dx));
                dc.DrawRectangle(null, MapToPen(i.ConnectedTo == null ? Circuit.ShapeType.Red : Circuit.ShapeType.Black), new Rect(x1, x2));
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

        void Circuit.ISymbolDrawing.DrawRectangle(Circuit.ShapeType Type, Circuit.Point x1, Circuit.Point x2)
        {
            if (dc == null) return;

            dc.DrawRectangle(null, MapToPen(Type), new Rect(MapToPoint(x1), MapToPoint(x2)));
        }

        void Circuit.ISymbolDrawing.DrawLine(Circuit.ShapeType Type, Circuit.Point x1, Circuit.Point x2)
        {
            if (dc == null) return;

            dc.DrawLine(MapToPen(Type), MapToPoint(x1), MapToPoint(x2));
        }

        void Circuit.ISymbolDrawing.DrawLines(Circuit.ShapeType Type, IEnumerable<Circuit.Point> x)
        {
            if (dc == null) return;

            IEnumerator<Circuit.Point> e = x.GetEnumerator();
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

        void Circuit.ISymbolDrawing.DrawEllipse(Circuit.ShapeType Type, Circuit.Point x1, Circuit.Point x2)
        {
            if (dc == null) return;

            Point p1 = MapToPoint(x1);
            Point p2 = MapToPoint(x2);

            dc.DrawEllipse(null, MapToPen(Type), new Point((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2), (p2.X - p1.X) / 2, (p2.Y - p1.Y) / 2);
        }

        void Circuit.ISymbolDrawing.DrawText(string S, Circuit.Point x, Circuit.Alignment Horizontal, Circuit.Alignment Vertical)
        {
            if (dc == null || !ShowText) return;

            FormattedText text = new FormattedText(
                S, 
                CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, 
                new Typeface(FontFamily, FontStyle, FontWeight, FontStretch), FontSize * scale, 
                Brushes.Black);

            Point p = MapToPoint(x);
            Vector p1 = MapToPoint(new Circuit.Point(x.x - MapAlignment(Horizontal), x.y + (1 - MapAlignment(Vertical)))) - p;
            Vector p2 = MapToPoint(new Circuit.Point(x.x - (1 - MapAlignment(Horizontal)), x.y + MapAlignment(Vertical))) - p;

            p1.X *= text.Width; p2.X *= text.Width;
            p1.Y *= text.Height; p2.Y *= text.Height;
            
            dc.DrawText(text, new Point(Math.Min(p.X + p1.X, p.X - p2.X), Math.Min(p.Y + p1.Y, p.Y - p2.Y)));
        }
        
        static Point ConvertToPoint(Circuit.Coord x)
        {
            return new Point(x.x, x.y);
        }
    }
}
