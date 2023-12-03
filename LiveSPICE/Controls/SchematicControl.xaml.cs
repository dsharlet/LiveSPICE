using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SchematicControls;

namespace LiveSPICE
{
    public class SelectionEventArgs : EventArgs
    {
        private List<Circuit.Component> selected;
        public IEnumerable<Circuit.Component> Selected { get { return selected; } }

        public SelectionEventArgs(IEnumerable<Circuit.Component> Selected) { selected = Selected.ToList(); }
        public SelectionEventArgs(params Circuit.Component[] Selected) { selected = Selected.ToList(); }
    }

    /// <summary>
    /// Control for interacting with a Circuit.Schematic.
    /// </summary>
    public partial class SchematicControl : UserControl, INotifyPropertyChanged
    {
        protected static readonly int Grid = 5;
        protected static readonly Vector AutoScrollBorder = new Vector(1, 1);

        private Circuit.Schematic schematic;
        /// <summary>
        /// Get the Schematic this control is displaying.
        /// </summary>
        public Circuit.Schematic Schematic { get { return schematic; } }

        protected Circuit.Coord origin = new Circuit.Coord(0, 0);
        public Circuit.Coord Origin { get { return origin; } set { origin = value; RefreshLayout(); } }

        public Canvas Overlays { get { return overlays; } }

        public SchematicControl(Circuit.Schematic Schematic)
        {
            InitializeComponent();

            schematic = Schematic;

            schematic.Elements.ItemAdded += ElementAdded;
            schematic.Elements.ItemRemoved += ElementRemoved;

            // Create controls for all the elements already in the schematic.
            foreach (Circuit.Element i in schematic.Elements)
                ElementAdded(null, new Circuit.ElementEventArgs(i));
        }

        // Schematic tools.
        private SchematicTool tool;
        public SchematicTool Tool
        {
            get { return tool; }
            set
            {
                if (tool != null)
                    tool.End();
                tool = value;
                if (tool != null)
                {
                    tool.Begin();
                    if (mouse.HasValue)
                    {
                        tool.MouseEnter(SnapToGrid(mouse.Value));
                        tool.MouseMove(SnapToGrid(mouse.Value));
                    }
                }
            }
        }

        // Elements.
        public IEnumerable<Circuit.Element> Elements { get { return schematic.Elements; } }
        public IEnumerable<Circuit.Symbol> Symbols { get { return schematic.Elements.OfType<Circuit.Symbol>(); } }
        public IEnumerable<Circuit.Wire> Wires { get { return schematic.Elements.OfType<Circuit.Wire>(); } }

        public IEnumerable<Circuit.Element> InRect(Circuit.Coord x1, Circuit.Coord x2)
        {
            Circuit.Coord a = new Circuit.Coord(Math.Min(x1.x, x2.x), Math.Min(x1.y, x2.y));
            Circuit.Coord b = new Circuit.Coord(Math.Max(x1.x, x2.x), Math.Max(x1.y, x2.y));
            return Symbols.Where(i => i.Intersects(a, b)).Cast<Circuit.Element>().Concat(
                Wires.Where(i => i.Intersects(a, b)));
        }
        public IEnumerable<Circuit.Element> AtPoint(Circuit.Coord At) { return InRect(At - 1, At + 1); }
        public Circuit.Node NodeAt(Circuit.Coord At) { return schematic.NodeAt(At); }

        public static Circuit.Coord LowerBound(IEnumerable<Circuit.Element> Of) { return new Circuit.Coord(Of.Min(i => i.LowerBound.x), Of.Min(i => i.LowerBound.y)); }
        public static Circuit.Coord UpperBound(IEnumerable<Circuit.Element> Of) { return new Circuit.Coord(Of.Max(i => i.UpperBound.x), Of.Max(i => i.UpperBound.y)); }
        public Circuit.Coord LowerBound() { return LowerBound(Elements); }
        public Circuit.Coord UpperBound() { return UpperBound(Elements); }

        protected static int Floor(double x, int p) { return (int)Math.Floor(x / p) * p; }
        protected static int Ceiling(double x, int p) { return (int)Math.Ceiling(x / p) * p; }
        protected static int Round(double x, int p) { return (int)Math.Round(x / p) * p; }
        protected static Circuit.Coord Floor(Circuit.Coord x, int p) { return new Circuit.Coord(Floor(x.x, p), Floor(x.y, p)); }
        protected static Circuit.Coord Ceiling(Circuit.Coord x, int p) { return new Circuit.Coord(Ceiling(x.x, p), Ceiling(x.y, p)); }
        protected static Circuit.Coord Round(Circuit.Coord x, int p) { return new Circuit.Coord(Round(x.x, p), Round(x.y, p)); }
        public Circuit.Coord SnapToGrid(Circuit.Coord x) { return new Circuit.Coord(Round(x.x, Grid), Round(x.y, Grid)); }
        public Circuit.Coord SnapToGrid(Point x) { return new Circuit.Coord(Round(x.X, Grid), Round(x.Y, Grid)) - origin; }

        public Point ToPoint(Circuit.Coord x) { return new Point(x.x + origin.x, x.y + origin.y); }

        private void ElementAdded(object sender, Circuit.ElementEventArgs e)
        {
            ElementControl control = ElementControl.New(e.Element);
            if (control is WireControl)
                wires.Children.Add(control);
            else if (control is SymbolControl)
                symbols.Children.Add(control);
            else
                throw new InvalidOperationException("Unknown element type");
            control.Element.LayoutChanged += ElementLayoutChanged;
            ElementLayoutChanged(control.Element, null);
        }
        private void ElementRemoved(object sender, Circuit.ElementEventArgs e)
        {
            ElementControl control = (ElementControl)e.Element.Tag;
            control.Element.LayoutChanged -= ElementLayoutChanged;
            wires.Children.Remove(control);
            symbols.Children.Remove(control);
            if (control.Selected)
                RaiseSelectionChanged();
        }
        private void ElementLayoutChanged(object sender, EventArgs e)
        {
            Circuit.Element element = (Circuit.Element)sender;
            Circuit.Coord lb = element.LowerBound;
            Circuit.Coord ub = element.UpperBound;

            ElementControl control = (ElementControl)element.Tag;

            Canvas.SetLeft(control, lb.x + origin.x);
            Canvas.SetTop(control, lb.y + origin.y);

            control.Width = ub.x - lb.x;
            control.Height = ub.y - lb.y;

            control.InvalidateVisual();
        }

        private void RefreshLayout()
        {
            foreach (Circuit.Element i in Elements)
                ElementLayoutChanged(i, null);
        }

        // Add/remove overlay element controls.
        public void AddOverlay(ElementControl Element)
        {
            Overlays.Children.Add(Element);
            Element.Element.LayoutChanged += ElementLayoutChanged;
            ElementLayoutChanged(Element.Element, null);
        }
        public void RemoveOverlay(ElementControl Element)
        {
            Overlays.Children.Remove(Element);
            Element.Element.LayoutChanged -= ElementLayoutChanged;
        }

        // Selection.
        public IEnumerable<Circuit.Element> Selected { get { return Elements.Where(i => ((ElementControl)i.Tag).Selected); } }

        public delegate void SelectionEventHandler(object sender, SelectionEventArgs e);

        private List<SelectionEventHandler> selectionChanged = new List<SelectionEventHandler>();
        public event SelectionEventHandler SelectionChanged
        {
            add { selectionChanged.Add(value); }
            remove { selectionChanged.Remove(value); }
        }
        public void RaiseSelectionChanged()
        {
            SelectionEventArgs e = new SelectionEventArgs(Selected.OfType<Circuit.Symbol>().Select(i => i.Component).DefaultIfEmpty(Schematic.Circuit));
            foreach (SelectionEventHandler i in selectionChanged)
                i(this, e);
        }

        private List<EventHandler> editSelection = new List<EventHandler>();
        public event EventHandler EditSelection
        {
            add { editSelection.Add(value); }
            remove { editSelection.Remove(value); }
        }
        public void RaiseEditSelection(EventArgs e)
        {
            foreach (EventHandler i in editSelection)
                i(this, e);
        }

        public void Select(IEnumerable<Circuit.Element> ToSelect, bool Only, bool Toggle)
        {
            bool changed = false;
            foreach (Circuit.Element i in Elements)
            {
                if (ToSelect.Contains(i))
                {
                    if (Toggle || !((ElementControl)i.Tag).Selected)
                    {
                        changed = true;
                        ((ElementControl)i.Tag).Selected = !((ElementControl)i.Tag).Selected;
                    }
                }
                else if (Only)
                {
                    if (((ElementControl)i.Tag).Selected)
                    {
                        changed = true;
                        ((ElementControl)i.Tag).Selected = false;
                    }
                }
            }

            if (changed)
                RaiseSelectionChanged();
        }

        public void Select(IEnumerable<Circuit.Element> ToSelect) { Select(ToSelect, (Keyboard.Modifiers & ModifierKeys.Control) == 0, false); }
        public void Select(params Circuit.Element[] ToSelect) { Select(ToSelect.AsEnumerable(), (Keyboard.Modifiers & ModifierKeys.Control) == 0, false); }

        public void ToggleSelect(IEnumerable<Circuit.Element> ToSelect) { Select(ToSelect, (Keyboard.Modifiers & ModifierKeys.Control) == 0, true); }
        public void ToggleSelect(params Circuit.Element[] ToSelect) { Select(ToSelect.AsEnumerable(), (Keyboard.Modifiers & ModifierKeys.Control) == 0, true); }

        public void Highlight(IEnumerable<Circuit.Element> ToHighlight)
        {
            foreach (Circuit.Element i in Elements)
                ((ElementControl)i.Tag).Highlighted = ToHighlight.Contains(i);
        }
        public void Highlight(params Circuit.Element[] ToHighlight) { Highlight(ToHighlight.AsEnumerable()); }

        // Keyboard events.
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if ((e.KeyboardDevice.Modifiers & (ModifierKeys.Control | ModifierKeys.Alt)) != 0)
                return;
            if (Tool != null)
                e.Handled = Tool.KeyDown(e);
        }
        protected override void OnKeyUp(KeyEventArgs e)
        {
            if ((e.KeyboardDevice.Modifiers & (ModifierKeys.Control | ModifierKeys.Alt)) != 0)
                return;
            if (Tool != null)
                e.Handled = Tool.KeyUp(e);
        }

        // Mouse events.
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                Focus();
                Keyboard.Focus(this);
                CaptureMouse();
                if (Tool != null)
                {
                    Circuit.Coord at = SnapToGrid(e.GetPosition(root));
                    if (e.ClickCount == 2)
                        Tool.MouseDoubleClick(at);
                    else
                        Tool.MouseDown(at);
                }
                e.Handled = true;
            }
            else if (e.ChangedButton == MouseButton.Middle)
            {
                Focus();
                Keyboard.Focus(this);
                CaptureMouse();
                e.Handled = true;
            }
            else if (e.ChangedButton == MouseButton.Right)
            {
                ReleaseMouseCapture();
                if (Tool != null)
                    Tool.Cancel();
                e.Handled = true;
            }
        }
        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            Circuit.Coord at = SnapToGrid(e.GetPosition(root));
            if (e.ChangedButton == MouseButton.Left)
            {
                if (Tool != null)
                    Tool.MouseUp(at);
                ReleaseMouseCapture();
                e.Handled = true;
            }
            else if (e.ChangedButton == MouseButton.Middle)
            {
                ReleaseMouseCapture();
                e.Handled = true;
            }
            else if (e.ChangedButton == MouseButton.Right && e.ClickCount == 1)
            {
                if (Tool != null)
                {
                    ContextMenu = Tool.BuildContextMenu(at);
                    if (ContextMenu != null)
                        ContextMenu.IsOpen = true;
                }
                e.Handled = true;
            }
        }
        private Circuit.Coord? mouse = null;
        protected override void OnMouseMove(MouseEventArgs e)
        {
            Point x = e.GetPosition(root);
            if (IsMouseCaptured && e.LeftButton == MouseButtonState.Pressed)
                BringIntoView(new Rect(x - AutoScrollBorder, x + AutoScrollBorder));
            Circuit.Coord at = SnapToGrid(x);
            if (!mouse.HasValue || mouse.Value != at)
            {
                mouse = at;
                if (Tool != null)
                    Tool.MouseMove(at);
            }
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            Circuit.Coord at = SnapToGrid(e.GetPosition(root));
            mouse = at;
            if (Tool != null)
                Tool.MouseEnter(at);
            e.Handled = true;
        }
        protected override void OnMouseLeave(MouseEventArgs e)
        {
            Circuit.Coord at = SnapToGrid(e.GetPosition(root));
            mouse = null;
            if (Tool != null)
                Tool.MouseLeave(at);
            e.Handled = true;
        }

        // INotifyPropertyChanged interface.
        protected void NotifyChanged(string p)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
