using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Util;

namespace SchematicControls
{
    /// <summary>
    /// Element for a symbol.
    /// </summary>
    public class SymbolControl : ElementControl
    {
        private static readonly Pen TextOutline = new Pen(new SolidColorBrush(Color.FromArgb(32, 0, 0, 0)), 0.2);

        static SymbolControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SymbolControl), new FrameworkPropertyMetadata(typeof(SymbolControl)));
        }

        private bool showText = true;
        public bool ShowText { get { return showText; } set { showText = value; InvalidateVisual(); } }

        protected Circuit.SymbolLayout layout;

        public SymbolControl(Circuit.Symbol S) : base(S)
        {
            layout = Component.LayoutSymbol();

            S.Component.PropertyChanged += (o, e) => RefreshLayout();

            MouseMove += OnMouseMove;
        }
        public SymbolControl(Circuit.Component C) : this(new Circuit.Symbol(C)) { }

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

            TextBlock text = new TextBlock();

            Circuit.Component component = Symbol.Component;

            text.Inlines.Add(new Bold(new Run(component.ToString())));

            foreach (PropertyInfo i in component.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(j =>
                j.CustomAttribute<Circuit.Serialize>() != null &&
                (j.CustomAttribute<BrowsableAttribute>() == null || j.CustomAttribute<BrowsableAttribute>().Browsable)))
            {
                object value = i.GetValue(component, null);
                DefaultValueAttribute def = i.CustomAttribute<DefaultValueAttribute>();
                if (def == null || !Equals(def.Value, value))
                {
                    System.ComponentModel.TypeConverter tc = System.ComponentModel.TypeDescriptor.GetConverter(i.PropertyType);
                    text.Inlines.Add(new Run("\n" + i.Name + " = "));
                    text.Inlines.Add(new Bold(new Run(tc.ConvertToString(value))));
                }
            }

            ToolTip = new ToolTip() { Content = text };
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            Circuit.Symbol sym = Symbol;
            Point b1 = ToPoint(sym.LowerBound - sym.Position);
            Point b2 = ToPoint(sym.UpperBound - sym.Position);
            return new Size(Math.Abs(b2.X - b1.X), Math.Abs(b2.Y - b1.Y));
        }

        protected Matrix Transform
        {
            get
            {
                var offset = (layout.LowerBound + layout.UpperBound) / 2;

                Matrix transform = new Matrix();
                transform.Translate(-offset.x, -offset.y);
                transform.Scale(1.0, Symbol.Flip ? 1.0 : -1.0);
                transform.Rotate(Symbol.Rotation * -90);
                transform.Translate(Symbol.Width / 2, Symbol.Height / 2);
                return transform;
            }
        }

        protected void RefreshLayout()
        {
            layout = Component.LayoutSymbol();
            InvalidateVisual();
        }

        protected DrawingContext dc;
        protected override void OnRender(DrawingContext dc)
        {
            Matrix transform = Transform;

            DrawLayout(
                layout, dc, transform, Pen, ShowText ? FontFamily : null, FontWeight, FontSize,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            Rect bounds = new Rect(T(transform, layout.LowerBound), T(transform, layout.UpperBound));
            if (Selected)
                dc.DrawRectangle(null, SelectedPen, bounds);
            else if (Highlighted)
                dc.DrawRectangle(null, HighlightPen, bounds);
        }

        private static Point T(Matrix Tx, Circuit.Point x) { return Tx.Transform(new Point(x.x, x.y)); }

        public static void DrawLayout(
            Circuit.SymbolLayout Layout, DrawingContext Context, Matrix Tx, Pen Pen, FontFamily FontFamily,
            FontWeight FontWeight, double FontSize, double PixelsPerDip)
        {
            foreach (Circuit.SymbolLayout.Shape i in Layout.Lines)
                Context.DrawLine(
                    Pen ?? MapToPen(i.Edge),
                    T(Tx, i.x1),
                    T(Tx, i.x2));
            foreach (Circuit.SymbolLayout.Shape i in Layout.Rectangles)
                Context.DrawRectangle(
                    (i.Fill && Pen == null) ? MapToBrush(i.Edge) : null,
                    Pen ?? MapToPen(i.Edge),
                    new Rect(T(Tx, i.x1), T(Tx, i.x2)));
            foreach (Circuit.SymbolLayout.Shape i in Layout.Ellipses)
            {
                Brush brush = (i.Fill && Pen == null) ? MapToBrush(i.Edge) : null;
                Pen pen = Pen ?? MapToPen(i.Edge);
                Point p1 = T(Tx, i.x1);
                Point p2 = T(Tx, i.x2);

                Context.DrawEllipse(
                    brush, pen,
                    new Point((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2), (p2.X - p1.X) / 2, (p2.Y - p1.Y) / 2);
            }
            foreach (Circuit.SymbolLayout.Curve i in Layout.Curves)
            {
                IEnumerator<Circuit.Point> e = i.x.AsEnumerable().GetEnumerator();
                if (!e.MoveNext())
                    return;

                Pen pen = Pen ?? MapToPen(i.Edge);
                Point x1 = T(Tx, e.Current);
                while (e.MoveNext())
                {
                    Point x2 = T(Tx, e.Current);
                    Context.DrawLine(pen, x1, x2);
                    x1 = x2;
                }
            }
            foreach (var arc in Layout.Arcs)
            {
                var sweepDir = arc.Direction == Circuit.Direction.Clockwise ^ Tx.Determinant > 0d ? SweepDirection.Clockwise : SweepDirection.Counterclockwise;
                bool isLargeArc = Math.Abs(arc.StartAngle - arc.EndAngle) > Math.PI;

                var start = T(Tx, arc.Center + (new Circuit.Point(Math.Cos(arc.StartAngle), Math.Sin(arc.StartAngle)) * arc.Radius));
                var end = T(Tx, arc.Center + (new Circuit.Point(Math.Cos(arc.EndAngle), Math.Sin(arc.EndAngle)) * arc.Radius));

                var arcGeometry = new StreamGeometry();
                using (var ctx = arcGeometry.Open())
                {
                    ctx.BeginFigure(start, false, false);
                    ctx.ArcTo(end, new Size(Math.Abs(arc.Radius * Tx.M11), Math.Abs(arc.Radius * Tx.M22)), 0, isLargeArc, sweepDir, true, true);
                }
                arcGeometry.Freeze();
                Context.DrawGeometry(null, Pen ?? MapToPen(arc.Type), arcGeometry);
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
                        Brushes.Black, PixelsPerDip);

                    Point p = T(Tx, i.x);
                    Vector p1 = T(Tx, new Circuit.Point(i.x.x - MapAlignment(i.HorizontalAlign), i.x.y + (1 - MapAlignment(i.VerticalAlign)))) - p;
                    Vector p2 = T(Tx, new Circuit.Point(i.x.x - (1 - MapAlignment(i.HorizontalAlign)), i.x.y + MapAlignment(i.VerticalAlign))) - p;

                    p1.X *= text.Width; p2.X *= text.Width;
                    p1.Y *= text.Height; p2.Y *= text.Height;

                    Rect rc = new Rect(
                        Math.Min(p.X + p1.X, p.X - p2.X),
                        Math.Min(p.Y + p1.Y, p.Y - p2.Y),
                        text.Width,
                        text.Height);
                    if (TextOutline != null)
                        Context.DrawRectangle(null, TextOutline, rc);

                    Context.DrawText(text, rc.TopLeft);
                }
            }

            foreach (Circuit.Terminal i in Layout.Terminals)
            {
                Point x = T(Tx, Layout.MapTerminal(i));
                Vector dx = new Vector(TerminalSize / 2, TerminalSize / 2);
                Pen pen = MapToPen(i.ConnectedTo is null ? Circuit.EdgeType.Red : Circuit.EdgeType.Wire);
                Context.DrawRectangle(pen.Brush, pen, new Rect(x - dx, x + dx));
            }
        }

        public static void DrawLayout(
            Circuit.SymbolLayout Layout,
            DrawingContext Context, Matrix Tx,
            FontFamily FontFamily, FontWeight FontWeight, double FontSize, double PixelsPerDip)
        {
            DrawLayout(Layout, Context, Tx, null, FontFamily, FontWeight, FontSize, PixelsPerDip);
        }

        public static void DrawLayout(
            Circuit.SymbolLayout Layout,
            DrawingContext Context, Matrix Tx,
            FontFamily FontFamily, double PixelsPerDip)
        {
            DrawLayout(Layout, Context, Tx, null, FontFamily, FontWeights.Normal, 10.0, PixelsPerDip);
        }

        public static void DrawLayout(
            Circuit.SymbolLayout Layout,
            DrawingContext Context, Matrix Tx, double PixelsPerDip)
        {
            DrawLayout(Layout, Context, Tx, new FontFamily("Courier New"), PixelsPerDip);
        }
    }
}
