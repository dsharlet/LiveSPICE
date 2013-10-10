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
    public class Symbol : Element
    {       
        static Symbol() { DefaultStyleKeyProperty.OverrideMetadata(typeof(Symbol), new FrameworkPropertyMetadata(typeof(Symbol))); }
        
        protected bool showText = true;
        public bool ShowText { get { return showText; } set { showText = value; InvalidateVisual(); } }
        
        protected Matrix transform;
        protected Point origin;
        protected double scale = 1.0;

        public Vector Size { get { return new Vector(GetSymbol().Size.x, GetSymbol().Size.y); } }

        protected Circuit.SymbolLayout layout = new Circuit.SymbolLayout();
        public Circuit.Component Component { get { return GetSymbol().Component; } }

        public Symbol(Circuit.Symbol S) : base(S)
        {
            Component.LayoutSymbol(layout);
        }

        public Symbol(Type T) : this(new Circuit.Symbol((Circuit.Component)Activator.CreateInstance(T))) { }

        public Circuit.Symbol GetSymbol() { return (Circuit.Symbol)element; }

        protected override Size MeasureOverride(Size constraint)
        {
            Point b1 = ToPoint(GetSymbol().LowerBound - GetSymbol().Position);
            Point b2 = ToPoint(GetSymbol().UpperBound - GetSymbol().Position);
            double width = Math.Abs(b2.X - b1.X);
            double height = Math.Abs(b2.Y - b1.Y);

            return new Size(
                Math.Min(width, constraint.Width),
                Math.Min(height, constraint.Height));
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            Point b1 = ToPoint(GetSymbol().LowerBound - GetSymbol().Position);
            Point b2 = ToPoint(GetSymbol().UpperBound - GetSymbol().Position);
            double width = Math.Abs(b2.X - b1.X);
            double height = Math.Abs(b2.Y - b1.Y);

            Size size = base.ArrangeOverride(arrangeBounds);
            scale = Math.Min(Math.Min(size.Width / width, size.Height / height), 1.0);
            origin = new Point((b1.X + b2.X) / 2, (b1.Y + b2.Y) / 2);

            return size;
        }

        protected Point LayoutToLocal(Circuit.Point x) { return transform.Transform(new Point(x.x, x.y)); }
        protected Point SymbolToLocal(Circuit.Point x) 
        { 
            return new Point(
                x.x * scale - origin.X + ActualWidth / 2,
                x.y * scale - origin.Y + ActualHeight / 2);
        }

        protected DrawingContext dc;
        protected override void OnRender(DrawingContext drawingContext)
        {
            dc = drawingContext;
            dc.PushGuidelineSet(Symbol.Guidelines);

            transform.SetIdentity();
            transform.Scale(scale, -scale);
            transform.Translate(-origin.X, origin.Y);
            if (((Circuit.Symbol)element).Flip)
                transform.Scale(1, -1);
            transform.Rotate(((Circuit.Symbol)element).Rotation * -90);
            transform.Translate(ActualWidth / 2, ActualHeight / 2);
            
            DrawLayout(layout, dc, transform, Pen, ShowText ? FontFamily : null, FontWeight, FontSize);

            Circuit.Symbol sym = GetSymbol();
            Point b1 = SymbolToLocal(sym.LowerBound - sym.Position);
            Point b2 = SymbolToLocal(sym.UpperBound - sym.Position);

            Rect bounds = new Rect(b1, b2);
            if (Selected)
                dc.DrawRectangle(null, SelectedPen, bounds);
            else if (Highlighted)
                dc.DrawRectangle(null, HighlightPen, bounds);

            dc.Pop();
            dc = null;
        }
        
        private static Point ToPoint(Circuit.Coord x) { return new Point(x.x, x.y); }

        public static double TerminalSize = 3.0;
        public static double EdgeThickness = 1.0;
        public static GuidelineSet Guidelines = new GuidelineSet(new double[] { EdgeThickness / 2 }, new double[] { EdgeThickness / 2 });

        public static Brush WireBrush = Brushes.Black;
        public static Pen WirePen = new Pen(WireBrush, EdgeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        public static Pen TerminalPen = new Pen(Brushes.Black, EdgeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };

        public static Pen BlackPen = new Pen(Brushes.Black, EdgeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        public static Pen GrayPen = new Pen(Brushes.Gray, EdgeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        public static Pen RedPen = new Pen(Brushes.Red, EdgeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };

        private static Point T(Matrix Tx, Circuit.Point x)
        {
            return Tx.Transform(new Point(x.x, x.y));
        }

        public static void DrawLayout(
            Circuit.SymbolLayout Layout,
            DrawingContext Context, Matrix Tx, 
            Pen Pen, FontFamily FontFamily, FontWeight FontWeight, double FontSize)
        {
            Context.PushGuidelineSet(Guidelines);

            foreach (Circuit.SymbolLayout.Shape i in Layout.Lines)
                Context.DrawLine(Pen != null ? Pen : MapToPen(i.Edge), T(Tx, i.x1), T(Tx, i.x2));
            foreach (Circuit.SymbolLayout.Shape i in Layout.Rectangles)
                Context.DrawRectangle(null, Pen != null ? Pen : MapToPen(i.Edge), new Rect(T(Tx, i.x1), T(Tx, i.x2)));
            foreach (Circuit.SymbolLayout.Shape i in Layout.Ellipses)
            {
                Pen pen = Pen != null ? Pen : MapToPen(i.Edge);
                Point p1 = T(Tx, i.x1);
                Point p2 = T(Tx, i.x2);

                Context.DrawEllipse(null, pen, new Point((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2), (p2.X - p1.X) / 2, (p2.Y - p1.Y) / 2);
            }
            foreach (Circuit.SymbolLayout.Curve i in Layout.Curves)
            {
                IEnumerator<Circuit.Point> e = i.x.AsEnumerable().GetEnumerator();
                if (!e.MoveNext())
                    return;

                Pen pen = Pen != null ? Pen : MapToPen(i.Edge);
                Point x1 = T(Tx, e.Current);
                while (e.MoveNext())
                {
                    Point x2 = T(Tx, e.Current);
                    Context.DrawLine(pen, x1, x2);
                    x1 = x2;
                }
            }

            if (FontFamily != null)
            {
                // Not sure if this matrix has row or column vectors... want the y axis scaling here.
                double scale = Math.Sqrt(Tx.M11 * Tx.M11 + Tx.M21 * Tx.M21);

                foreach (Circuit.SymbolLayout.Text i in Layout.Texts)
                {
                    FormattedText text = new FormattedText(
                        i.String,
                        CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                        new Typeface(FontFamily, FontStyles.Normal, FontWeight, FontStretches.Normal), FontSize * scale,
                        Brushes.Black);

                    Point p = T(Tx, i.x);
                    Vector p1 = T(Tx, new Circuit.Point(i.x.x - MapAlignment(i.HorizontalAlign), i.x.y + (1 - MapAlignment(i.VerticalAlign)))) - p;
                    Vector p2 = T(Tx, new Circuit.Point(i.x.x - (1 - MapAlignment(i.HorizontalAlign)), i.x.y + MapAlignment(i.VerticalAlign))) - p;

                    p1.X *= text.Width; p2.X *= text.Width;
                    p1.Y *= text.Height; p2.Y *= text.Height;

                    Context.DrawText(text, new Point(Math.Min(p.X + p1.X, p.X - p2.X), Math.Min(p.Y + p1.Y, p.Y - p2.Y)));
                }
            }

            foreach (Circuit.Terminal i in Layout.Terminals)
                DrawTerminal(Context, T(Tx, Layout.MapTerminal(i)), i.ConnectedTo != null);

            Context.Pop();
        }

        public static void DrawLayout(
            Circuit.SymbolLayout Layout,
            DrawingContext Context, Matrix Tx,
            FontFamily FontFamily, FontWeight FontWeight, double FontSize)
        {
            DrawLayout(Layout, Context, Tx, null, FontFamily, FontWeight, FontSize);
        }

        public static void DrawLayout(
            Circuit.SymbolLayout Layout,
            DrawingContext Context, Matrix Tx,
            FontFamily FontFamily)
        {
            DrawLayout(Layout, Context, Tx, null, FontFamily, FontWeights.Normal, 10.0);
        }

        public static void DrawLayout(
            Circuit.SymbolLayout Layout,
            DrawingContext Context, Matrix Tx)
        {
            DrawLayout(Layout, Context, Tx, new FontFamily("Courier New"));
        }

        public static void DrawTerminal(DrawingContext Context, Point x, bool Connected)
        {
            Vector dx = new Vector(TerminalSize / 2, TerminalSize / 2);
            Context.DrawRectangle(null, MapToPen(Connected ? Circuit.EdgeType.Black : Circuit.EdgeType.Red), new Rect(x - dx, x + dx));
        }

        public static Pen MapToPen(Circuit.EdgeType Edge)
        {
            switch (Edge)
            {
                case Circuit.EdgeType.Wire: return WirePen;
                case Circuit.EdgeType.Black: return BlackPen;
                case Circuit.EdgeType.Gray: return GrayPen;
                case Circuit.EdgeType.Red: return RedPen;
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
