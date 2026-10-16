using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Circuit;
using APoint = Avalonia.Point;
using AVector = Avalonia.Vector;

namespace LiveSPICE.Avalonia;

public sealed class SchematicCanvas : Control
{
    private const double GridSize = 10;
    private const double TerminalSize = 3;
    private static readonly Typeface TextTypeface = new Typeface("Inter");

    private Schematic? schematic;
    private APoint pan = new APoint(0, 0);
    private APoint? dragStart;
    private Coord? selectionStart;
    private Coord? selectionCurrent;
    private bool additiveSelection;
    private Element[] selectionBase = Array.Empty<Element>();
    private Coord? moveStart;
    private Coord moveDelta;
    private Element[] moveElements = Array.Empty<Element>();
    private Coord? wireStart;
    private bool wireMode;
    private readonly HashSet<Element> selected = new HashSet<Element>();

    public SchematicDocument? Document
    {
        get => document;
        set
        {
            document = value;
            Schematic = value?.Schematic;
        }
    }

    private SchematicDocument? document;

    public Component? PendingComponent { get; set; }

    public object? SelectedObject => selected.Count == 1 ? (selected.Single() is Symbol symbol ? symbol.Component : selected.Single()) : null;

    public IReadOnlyList<object> SelectedObjects => selected.Select(i => i is Symbol symbol ? symbol.Component : (object)i).ToArray();

    public event Action? SelectionChanged;

    public event Action? DocumentChanged;

    public Schematic? Schematic
    {
        get => schematic;
        set
        {
            schematic = value;
            FitToView();
            InvalidateVisual();
        }
    }

    public double Zoom
    {
        get => zoom;
        set
        {
            zoom = Math.Clamp(value, 0.1, 20);
            InvalidateVisual();
        }
    }

    private double zoom = 1;

    public SchematicCanvas()
    {
        ClipToBounds = true;
        Focusable = true;
        PointerPressed += OnPointerPressed;
        PointerReleased += OnPointerReleased;
        PointerMoved += OnPointerMoved;
        PointerWheelChanged += OnPointerWheelChanged;
        SizeChanged += (_, _) => FitToView();
    }

    public void FitToView()
    {
        if (schematic == null || Bounds.Width <= 0 || Bounds.Height <= 0)
            return;

        Coord lower = schematic.LowerBound;
        Coord upper = schematic.UpperBound;
        double width = Math.Max(upper.x - lower.x, 200);
        double height = Math.Max(upper.y - lower.y, 200);
        double fit = Math.Min((Bounds.Width - 80) / width, (Bounds.Height - 80) / height);

        zoom = Math.Clamp(fit, 0.1, 8);
        pan = new APoint(
            Bounds.Width / 2 - (lower.x + width / 2) * zoom,
            Bounds.Height / 2 - (lower.y + height / 2) * zoom);
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        context.FillRectangle(Brushes.White, Bounds);
        DrawGrid(context);

        if (schematic == null)
        {
            DrawCenteredText(context, "Open a .schx file to view a schematic");
            return;
        }

        foreach (Wire wire in schematic.Wires)
            DrawWire(context, wire);

        foreach (Symbol symbol in schematic.Symbols)
            DrawSymbol(context, symbol);

        if (selectionStart.HasValue && selectionCurrent.HasValue)
        {
            Rect rect = RectFromPoints(ToScreen(selectionStart.Value), ToScreen(selectionCurrent.Value));
            context.DrawRectangle(new SolidColorBrush(Color.FromArgb(30, 30, 120, 240)), new Pen(Brushes.DodgerBlue, 1, DashStyle.Dash), rect);
        }
    }

    private void DrawGrid(DrawingContext context)
    {
        Pen minor = new Pen(new SolidColorBrush(Color.FromRgb(225, 225, 245)), 1);
        Pen major = new Pen(new SolidColorBrush(Color.FromRgb(175, 175, 230)), 1);
        double step = GridSize * Zoom;

        if (step < 4)
            return;

        double startX = pan.X % step;
        double startY = pan.Y % step;
        for (double x = startX; x < Bounds.Width; x += step)
            context.DrawLine(Math.Abs((x - pan.X) / step % 10) < 0.001 ? major : minor, new APoint(x, 0), new APoint(x, Bounds.Height));
        for (double y = startY; y < Bounds.Height; y += step)
            context.DrawLine(Math.Abs((y - pan.Y) / step % 10) < 0.001 ? major : minor, new APoint(0, y), new APoint(Bounds.Width, y));
    }

    private void DrawWire(DrawingContext context, Wire wire)
    {
        Pen wirePen = new Pen(selected.Contains(wire) ? Brushes.DodgerBlue : Brushes.DarkBlue, Math.Max(1, Zoom));
        context.DrawLine(wirePen, ToScreen(wire.A), ToScreen(wire.B));
        DrawTerminal(context, ToScreen(wire.A), Brushes.DarkBlue);
        DrawTerminal(context, ToScreen(wire.B), Brushes.DarkBlue);
    }

    private void DrawSymbol(DrawingContext context, Symbol symbol)
    {
        SymbolLayout layout = symbol.Component.LayoutSymbol();
        Transform transform = new Transform(symbol, layout);

        foreach (SymbolLayout.Shape line in layout.Lines)
            context.DrawLine(PenFor(line.Edge), ToScreen(transform.Apply(line.x1)), ToScreen(transform.Apply(line.x2)));

        foreach (SymbolLayout.Shape rectangle in layout.Rectangles)
        {
            Rect rect = RectFromPoints(ToScreen(transform.Apply(rectangle.x1)), ToScreen(transform.Apply(rectangle.x2)));
            context.DrawRectangle(rectangle.Fill ? BrushFor(rectangle.Edge) : null, PenFor(rectangle.Edge), rect);
        }

        foreach (SymbolLayout.Shape ellipse in layout.Ellipses)
        {
            Rect rect = RectFromPoints(ToScreen(transform.Apply(ellipse.x1)), ToScreen(transform.Apply(ellipse.x2)));
            context.DrawEllipse(ellipse.Fill ? BrushFor(ellipse.Edge) : null, PenFor(ellipse.Edge), rect);
        }

        foreach (SymbolLayout.Curve curve in layout.Curves)
            DrawCurve(context, transform, curve);

        foreach (SymbolLayout.Arc arc in layout.Arcs)
            DrawArc(context, transform, arc);

        foreach (SymbolLayout.Text text in layout.Texts)
            DrawText(context, transform, text);

        foreach (Terminal terminal in layout.Terminals)
            DrawTerminal(context, ToScreen(transform.Apply(layout.MapTerminal(terminal))), terminal.ConnectedTo == null ? Brushes.Red : Brushes.DarkBlue);

        if (selected.Contains(symbol))
            context.DrawRectangle(null, new Pen(Brushes.DodgerBlue, 1), RectFromPoints(ToScreen(symbol.LowerBound), ToScreen(symbol.UpperBound)));
    }

    private void DrawCurve(DrawingContext context, Transform transform, SymbolLayout.Curve curve)
    {
        if (curve.x.Length < 2)
            return;

        Pen pen = PenFor(curve.Edge);
        APoint previous = ToScreen(transform.Apply(curve.x[0]));
        for (int i = 1; i < curve.x.Length; i++)
        {
            APoint next = ToScreen(transform.Apply(curve.x[i]));
            context.DrawLine(pen, previous, next);
            previous = next;
        }
    }

    private void DrawArc(DrawingContext context, Transform transform, SymbolLayout.Arc arc)
    {
        const int segments = 24;
        double start = arc.StartAngle;
        double end = arc.EndAngle;
        double sweep = end - start;

        if (arc.Direction == Direction.Clockwise && sweep < 0)
            sweep += 2 * Math.PI;
        else if (arc.Direction == Direction.Counterclockwise && sweep > 0)
            sweep -= 2 * Math.PI;

        Pen pen = PenFor(arc.Type);
        Circuit.Point previous = ArcPoint(arc, transform, start);
        for (int i = 1; i <= segments; i++)
        {
            double angle = start + sweep * i / segments;
            Circuit.Point next = ArcPoint(arc, transform, angle);
            context.DrawLine(pen, ToScreen(previous), ToScreen(next));
            previous = next;
        }
    }

    private static Circuit.Point ArcPoint(SymbolLayout.Arc arc, Transform transform, double angle)
    {
        return transform.Apply(new Circuit.Point(
            arc.Center.x + Math.Cos(angle) * arc.Radius,
            arc.Center.y + Math.Sin(angle) * arc.Radius));
    }

    private void DrawText(DrawingContext context, Transform transform, SymbolLayout.Text text)
    {
        double size = text.Size switch
        {
            Circuit.Size.Small => 8,
            Circuit.Size.Large => 14,
            _ => 10
        };
        FormattedText formatted = new FormattedText(text.String, System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, TextTypeface, size * Zoom, Brushes.Black);
        context.DrawText(formatted, TextOrigin(transform, text, formatted.Width, formatted.Height));
    }

    private APoint TextOrigin(Transform transform, SymbolLayout.Text text, double width, double height)
    {
        APoint point = ToScreen(transform.Apply(text.x));
        AVector p1 = TextOffset(transform, text.x, new Circuit.Point(
            text.x.x - AlignmentFactor(text.HorizontalAlign),
            text.x.y + (1 - AlignmentFactor(text.VerticalAlign))));
        AVector p2 = TextOffset(transform, text.x, new Circuit.Point(
            text.x.x - (1 - AlignmentFactor(text.HorizontalAlign)),
            text.x.y + AlignmentFactor(text.VerticalAlign)));

        p1 = new AVector(p1.X * width, p1.Y * height);
        p2 = new AVector(p2.X * width, p2.Y * height);

        return new APoint(
            Math.Min(point.X + p1.X, point.X - p2.X),
            Math.Min(point.Y + p1.Y, point.Y - p2.Y));
    }

    private static AVector TextOffset(Transform transform, Circuit.Point origin, Circuit.Point target)
    {
        Circuit.Point transformedOrigin = transform.Apply(origin);
        Circuit.Point transformedTarget = transform.Apply(target);
        return new AVector(transformedTarget.x - transformedOrigin.x, transformedTarget.y - transformedOrigin.y);
    }

    private static double AlignmentFactor(Alignment alignment)
    {
        return alignment switch
        {
            Alignment.Near => 0,
            Alignment.Center => 0.5,
            Alignment.Far => 1,
            _ => 0
        };
    }

    private void DrawTerminal(DrawingContext context, APoint point, IBrush brush)
    {
        double half = Math.Max(TerminalSize * Zoom / 2, 2);
        context.DrawRectangle(brush, null, new Rect(point.X - half, point.Y - half, half * 2, half * 2));
    }

    private void DrawCenteredText(DrawingContext context, string text)
    {
        FormattedText formatted = new FormattedText(text, System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, TextTypeface, 16, Brushes.DimGray);
        context.DrawText(formatted, new APoint((Bounds.Width - formatted.Width) / 2, (Bounds.Height - formatted.Height) / 2));
    }

    private APoint ToScreen(Circuit.Point point)
    {
        return new APoint(point.x * Zoom + pan.X, point.y * Zoom + pan.Y);
    }

    internal APoint SchematicToScreen(Circuit.Point point)
    {
        return ToScreen(point);
    }

    private Coord ToSchematic(APoint point)
    {
        return Snap(new Coord((int)Math.Round((point.X - pan.X) / Zoom), (int)Math.Round((point.Y - pan.Y) / Zoom)));
    }

    private static Coord Snap(Coord coord)
    {
        return new Coord((int)Math.Round(coord.x / GridSize) * (int)GridSize, (int)Math.Round(coord.y / GridSize) * (int)GridSize);
    }

    private static Rect RectFromPoints(APoint a, APoint b)
    {
        return new Rect(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
    }

    private Pen PenFor(EdgeType edge)
    {
        return new Pen(BrushFor(edge), Math.Max(1, Zoom));
    }

    private static IBrush BrushFor(EdgeType edge)
    {
        return edge switch
        {
            EdgeType.Wire => Brushes.DarkBlue,
            EdgeType.Gray => Brushes.Gray,
            EdgeType.Red => Brushes.Red,
            EdgeType.Green => Brushes.LimeGreen,
            EdgeType.Blue => Brushes.Blue,
            EdgeType.Yellow => Brushes.Goldenrod,
            EdgeType.Cyan => Brushes.Teal,
            EdgeType.Magenta => Brushes.Magenta,
            EdgeType.Orange => Brushes.Orange,
            _ => Brushes.Black
        };
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        Focus();
        APoint point = e.GetPosition(this);
        Coord at = ToSchematic(point);
        PointerPointProperties properties = e.GetCurrentPoint(this).Properties;

        if (schematic == null)
            return;

        if (PendingComponent != null)
        {
            Symbol symbol = new Symbol(PendingComponent.Clone()) { Position = at };
            document?.Do(new AddElementsAction(schematic, new[] { symbol }));
            SetSelection(symbol);
            PendingComponent = null;
            MarkChanged(false);
            return;
        }

        if (wireMode)
        {
            if (!wireStart.HasValue)
            {
                wireStart = at;
            }
            else if (wireStart.Value != at)
            {
                Wire wire = new Wire(wireStart.Value, at);
                document?.Do(new AddElementsAction(schematic, new[] { wire }));
                SetSelection(wire);
                wireStart = null;
                wireMode = false;
                MarkChanged(false);
            }
            return;
        }

        if (properties.IsMiddleButtonPressed)
        {
            dragStart = point;
            e.Pointer.Capture(this);
            return;
        }

        Element? hit = HitTest(at);
        if (hit != null)
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                if (!selected.Add(hit))
                    selected.Remove(hit);
                SelectionChanged?.Invoke();
                InvalidateVisual();
            }
            else if (!selected.Contains(hit))
            {
                SetSelection(hit);
            }
            moveStart = at;
            moveDelta = new Coord(0, 0);
            moveElements = selected.ToArray();
        }
        else
        {
            selectionStart = at;
            selectionCurrent = at;
            additiveSelection = e.KeyModifiers.HasFlag(KeyModifiers.Control);
            selectionBase = additiveSelection ? selected.ToArray() : Array.Empty<Element>();
            if (!additiveSelection)
                selected.Clear();
            SelectionChanged?.Invoke();
            InvalidateVisual();
        }
        e.Pointer.Capture(this);
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        dragStart = null;
        if (selectionStart.HasValue && selectionCurrent.HasValue)
        {
            SelectRect(selectionStart.Value, selectionCurrent.Value);
            selectionStart = null;
            selectionCurrent = null;
            additiveSelection = false;
            selectionBase = Array.Empty<Element>();
        }
        if (moveDelta != new Coord(0, 0) && moveElements.Length > 0)
        {
            document?.Record(new MoveElementsAction(moveElements, moveDelta));
            MarkChanged(false);
        }
        moveStart = null;
        moveDelta = new Coord(0, 0);
        moveElements = Array.Empty<Element>();
        e.Pointer.Capture(null);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        APoint current = e.GetPosition(this);
        if (selectionStart.HasValue)
        {
            selectionCurrent = ToSchematic(current);
            HighlightRect(selectionStart.Value, selectionCurrent.Value);
            InvalidateVisual();
            return;
        }

        if (moveStart.HasValue && selected.Count > 0)
        {
            Coord at = ToSchematic(current);
            Coord delta = at - moveStart.Value;
            if (delta != new Coord(0, 0))
            {
                foreach (Element element in moveElements)
                    element.Move(delta);
                moveDelta += delta;
                moveStart = at;
                InvalidateVisual();
            }
            return;
        }

        if (dragStart != null)
        {
            AVector delta = current - dragStart.Value;
            pan += delta;
            dragStart = current;
            InvalidateVisual();
        }
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        APoint focus = e.GetPosition(this);
        double oldZoom = Zoom;
        Zoom *= e.Delta.Y > 0 ? 1.1 : 1 / 1.1;
        double ratio = Zoom / oldZoom;
        pan = new APoint(focus.X - (focus.X - pan.X) * ratio, focus.Y - (focus.Y - pan.Y) * ratio);
    }

    public void BeginWireTool()
    {
        PendingComponent = null;
        wireMode = true;
        wireStart = null;
    }

    internal bool TestClick(Coord at, bool control = false)
    {
        Element? hit = HitTest(at);
        if (hit == null)
        {
            if (!control)
                ClearSelection();
            return false;
        }

        if (control)
        {
            if (!selected.Add(hit))
                selected.Remove(hit);
            SelectionChanged?.Invoke();
            InvalidateVisual();
        }
        else
        {
            SetSelection(hit);
        }
        return true;
    }

    internal void TestDragSelected(Coord from, Coord to)
    {
        if (selected.Count == 0)
            TestClick(from);
        if (selected.Count == 0)
            return;

        Element[] moved = selected.ToArray();
        Coord delta = to - from;
        if (delta == new Coord(0, 0))
            return;

        foreach (Element element in moved)
            element.Move(delta);
        document?.Record(new MoveElementsAction(moved, delta));
        MarkChanged(false);
    }

    internal void TestDragSelect(Coord a, Coord b, bool control = false)
    {
        selectionBase = control ? selected.ToArray() : Array.Empty<Element>();
        HighlightRect(a, b);
        selectionBase = Array.Empty<Element>();
        InvalidateVisual();
    }

    internal bool TestWireClick(Coord at)
    {
        if (schematic == null)
            return false;

        if (!wireStart.HasValue)
        {
            BeginWireTool();
            wireStart = at;
            return false;
        }

        if (wireStart.Value == at)
            return false;

        Wire wire = new Wire(wireStart.Value, at);
        document?.Do(new AddElementsAction(schematic, new[] { wire }));
        SetSelection(wire);
        wireStart = null;
        wireMode = false;
        MarkChanged(false);
        return true;
    }

    internal static Coord TestTransformPoint(Symbol symbol, Circuit.Point point)
    {
        return (Coord)Circuit.Point.Round(new Transform(symbol, symbol.Component.LayoutSymbol()).Apply(point));
    }

    public void DeleteSelection()
    {
        if (schematic == null || selected.Count == 0)
            return;

        Element[] remove = selected.ToArray();
        document?.Do(new RemoveElementsAction(schematic, remove));
        ClearSelection();
        MarkChanged(false);
    }

    public string? CopySelectionXml()
    {
        if (selected.Count == 0)
            return null;

        XElement root = new XElement("Schematic");
        foreach (Element element in selected)
            root.Add(element.Serialize());
        return root.ToString();
    }

    public bool PasteSelectionXml(string xml)
    {
        if (schematic == null)
            return false;

        try
        {
            List<Element> elements = XElement.Parse(xml).Elements("Element").Select(Element.Deserialize).ToList();
            if (elements.Count == 0)
                return false;

            Coord offset = new Coord(20, 20);
            foreach (Element element in elements)
                element.Move(offset);

            document?.Do(new AddElementsAction(schematic, elements));
            selected.Clear();
            foreach (Element element in elements)
                selected.Add(element);
            MarkChanged(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void SelectAll()
    {
        if (schematic == null)
            return;

        selected.Clear();
        foreach (Element element in schematic.Elements)
            selected.Add(element);
        SelectionChanged?.Invoke();
        InvalidateVisual();
    }

    public void RotateSelection(int delta)
    {
        if (selected.Count == 0)
            return;

        Circuit.Point center = SelectionCenter();
        document?.Do(new RotateElementsAction(selected, delta, center));
        MarkChanged(false);
    }

    public void FlipSelection()
    {
        if (selected.Count == 0)
            return;

        double y = SelectionCenter().y;
        document?.Do(new FlipElementsAction(selected, y));
        MarkChanged(false);
    }

    private Circuit.Point SelectionCenter()
    {
        Coord lower = new Coord(selected.Min(i => i.LowerBound.x), selected.Min(i => i.LowerBound.y));
        Coord upper = new Coord(selected.Max(i => i.UpperBound.x), selected.Max(i => i.UpperBound.y));
        return (lower + upper) / 2;
    }

    private Element? HitTest(Coord at)
    {
        if (schematic == null)
            return null;

        return schematic.Elements.Reverse().FirstOrDefault(i => i.Intersects(at - 4, at + 4));
    }

    private void HighlightRect(Coord a, Coord b)
    {
        if (schematic == null)
            return;

        selected.Clear();
        foreach (Element element in selectionBase)
            selected.Add(element);
        foreach (Element element in ElementsInRect(a, b))
            selected.Add(element);
        SelectionChanged?.Invoke();
    }

    private void SelectRect(Coord a, Coord b)
    {
        HighlightRect(a, b);
        InvalidateVisual();
    }

    private IEnumerable<Element> ElementsInRect(Coord a, Coord b)
    {
        if (schematic == null)
            return Array.Empty<Element>();

        Coord lower = new Coord(Math.Min(a.x, b.x), Math.Min(a.y, b.y));
        Coord upper = new Coord(Math.Max(a.x, b.x), Math.Max(a.y, b.y));
        return schematic.Elements.Where(i => i.Intersects(lower, upper)).ToArray();
    }

    private void SetSelection(Element element)
    {
        selected.Clear();
        selected.Add(element);
        SelectionChanged?.Invoke();
        InvalidateVisual();
    }

    private void ClearSelection()
    {
        selected.Clear();
        SelectionChanged?.Invoke();
        InvalidateVisual();
    }

    private void MarkChanged(bool markDirty = true)
    {
        if (markDirty)
            document?.MarkDirty();
        DocumentChanged?.Invoke();
        SelectionChanged?.Invoke();
        InvalidateVisual();
    }

    private readonly struct Transform
    {
        private readonly Symbol symbol;
        private readonly SymbolLayout layout;

        public Transform(Symbol symbol, SymbolLayout layout)
        {
            this.symbol = symbol;
            this.layout = layout;
        }

        public Circuit.Point Apply(Circuit.Point local)
        {
            double x = local.x;
            double y = local.y;
            if (!symbol.Flip)
                y = -y;

            int rotation = ((symbol.Rotation % 4) + 4) % 4;
            (x, y) = rotation switch
            {
                1 => (y, -x),
                2 => (-x, -y),
                3 => (-y, x),
                _ => (x, y)
            };

            return new Circuit.Point(x + symbol.Position.x, y + symbol.Position.y);
        }
    }
}