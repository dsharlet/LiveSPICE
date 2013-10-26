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
using System.ComponentModel;

namespace LiveSPICE
{
    /// <summary>
    /// Element for a symbol.
    /// </summary>
    public class SymbolControl : ElementControl
    {       
        static SymbolControl() { DefaultStyleKeyProperty.OverrideMetadata(typeof(SymbolControl), new FrameworkPropertyMetadata(typeof(SymbolControl))); }

        private bool showText = true;
        public bool ShowText { get { return showText; } set { showText = value; InvalidateVisual();  } }
    
        protected Circuit.SymbolLayout layout = new Circuit.SymbolLayout();

        public SymbolControl(Circuit.Symbol S) : base(S)
        {
            Component.LayoutSymbol(layout);

            S.Component.PropertyChanged += (o, e) => InvalidateVisual();

            MouseMove += OnMouseMove;
        }
        public SymbolControl(Type T) : this(new Circuit.Symbol((Circuit.Component)Activator.CreateInstance(T))) { }

        public Circuit.Symbol Symbol { get { return (Circuit.Symbol)element; } }
        public Circuit.Component Component { get { return Symbol.Component; } }
        public Vector Size { get { return new Vector(Symbol.Size.x, Symbol.Size.y); } }

        protected void OnMouseMove(object sender, MouseEventArgs e)
        {
            Point x = e.GetPosition(this);

            Matrix transform = Transform;

            foreach (Circuit.Terminal i in Symbol.Terminals)
            {
                Circuit.Coord tx = layout.MapTerminal(i);
                Point tp = new Point(tx.x, tx.y);
                tp = transform.Transform(tp);
                if ((tp - x).Length < 5.0)
                {
                    ToolTip = "Terminal '" + i.ToString() + "'";
                    return;
                }
            }

            ToolTip = Symbol.Component.ToString();

            //StringBuilder sb = new StringBuilder();

            //Type T = component.GetType();

            //DisplayNameAttribute name = T.GetCustomAttribute<DisplayNameAttribute>();
            //if (name != null)
            //    sb.AppendLine(name.DisplayName);
            //else
            //    sb.AppendLine(T.ToString());

            //foreach (PropertyInfo i in T.GetProperties().Where(j => j.GetCustomAttribute<Circuit.SchematicPersistent>() != null))
            //{
            //    System.ComponentModel.TypeConverter tc = System.ComponentModel.TypeDescriptor.GetConverter(i.PropertyType);
            //    sb.AppendLine(i.Name + " = " + tc.ConvertToString(i.GetValue(component)));
            //}

            //ToolTip = sb.ToString();
        }

        protected override Size MeasureOverride(Size constraint)
        {
            Circuit.Symbol sym = Symbol;
            Point b1 = ToPoint(sym.LowerBound - sym.Position);
            Point b2 = ToPoint(sym.UpperBound - sym.Position);
            return new Size(
                Math.Min(Math.Abs(b2.X - b1.X), constraint.Width),
                Math.Min(Math.Abs(b2.Y - b1.Y), constraint.Height));
        }

        protected Matrix Transform
        {
            get
            {
                layout = new Circuit.SymbolLayout();
                Symbol.Component.LayoutSymbol(layout);

                Circuit.Coord center = (layout.LowerBound + layout.UpperBound) / 2;
                double scale = Math.Min(Math.Min(ActualWidth / Symbol.Width, ActualHeight / Symbol.Height), 1.0);

                Matrix transform = new Matrix();
                transform.Translate(-center.x, -center.y);
                transform.Scale(scale, Symbol.Flip ? scale : -scale);
                transform.Rotate(Symbol.Rotation * -90);
                transform.Translate(ActualWidth / 2, ActualHeight / 2);
                return transform;
            }
        }

        protected DrawingContext dc;
        protected override void OnRender(DrawingContext drawingContext)
        {
            Matrix transform = Transform;

            dc = drawingContext;
            dc.PushGuidelineSet(SymbolControl.Guidelines);
            
            Rect bounds = new Rect(T(transform, layout.LowerBound), T(transform, layout.UpperBound));
            if (Selected)
                dc.DrawRectangle(null, SelectedPen, bounds);
            else if (Highlighted)
                dc.DrawRectangle(null, HighlightPen, bounds);

            DrawLayout(layout, dc, transform, Pen, ShowText ? FontFamily : null, FontWeight, FontSize);

            dc.Pop();
            dc = null;
        }
        
        private static Point T(Matrix Tx, Circuit.Point x) { return Tx.Transform(new Point(x.x, x.y)); }

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
                    double size;
                    switch (i.Size)
                    {
                        case Circuit.Size.Small: size = 0.5; break;
                        case Circuit.Size.Large: size = 1.5; break;
                        default: size = 1.0; break;
                    }
                    FormattedText text = new FormattedText(
                        i.String,
                        CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                        new Typeface(FontFamily, FontStyles.Normal, FontWeight, FontStretches.Normal), FontSize * scale * size,
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
    }
}
