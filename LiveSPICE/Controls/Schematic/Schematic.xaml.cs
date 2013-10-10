using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using Microsoft.Win32;
using System.Xml.Linq;
using SyMath;

namespace LiveSPICE
{
    /// <summary>
    /// Control for interacting with a Circuit.Schematic.
    /// </summary>
    public partial class Schematic : UserControl, INotifyPropertyChanged
    {
        public const string FileExtension = ".xml";
        public const int AutoScrollBorder = 1;

        protected Circuit.Schematic schematic;

        public int Grid = 10;

        public Schematic() : this(new Circuit.Schematic()) { }

        public Schematic(Circuit.Schematic S)
        {
            InitializeComponent();

            schematic = S;

            Background = Brushes.LightGray;
            Cursor = Cursors.Cross;
            Focusable = true;
            Width = schematic.Width;
            Height = schematic.Height;
            
            PreviewMouseDown += Schematic_MouseDown;
            PreviewMouseUp += Schematic_MouseUp;
            PreviewMouseMove += Schematic_MouseMove;
            MouseLeave += Schematic_MouseLeave;
            MouseEnter += Schematic_MouseEnter;

            PreviewKeyDown += Schematic_KeyDown;
            PreviewKeyUp += Schematic_KeyUp;

            edits = new EditStack();
            edits.Dirtied += OnDirtied;

            foreach (Circuit.Element i in S.Elements)
            {
                Element e = Element.New(i);
                components.Children.Add(e);
                Canvas.SetLeft(e, i.LowerBound.x);
                Canvas.SetTop(e, i.LowerBound.y);
            }
        }

        // Schematic tools.
        SchematicTool tool;
        public SchematicTool Tool 
        { 
            get 
            {
                if (tool == null)
                {
                    tool = new SelectionTool(this);
                    tool.Begin();
                }
                return tool;
            }
            set
            {
                if (tool != null)
                    tool.End();
                tool = value != null ? value : new SelectionTool(this);
                tool.Begin();
                if (mouse.HasValue)
                {
                    tool.MouseEnter(mouse.Value);
                    tool.MouseMove(mouse.Value);
                }
            }
        }

        private EditStack edits;
        public EditStack Edits { get { return edits; } }
        private void OnDirtied(object sender, EventArgs e) { }

        // File.
        private string filename;
        public string FileName { get { return filename == null ? "Untitled" : filename; } }
        public bool Save()
        {
            if (filename == null)
                return SaveAs();
            else
                return Save(filename);
        }
        public bool SaveAs()
        {
            SaveFileDialog dlg = new SaveFileDialog()
            {
                FileName = FileName,
                Filter = "Circuit Schematics|*" + FileExtension,
                DefaultExt = FileExtension
            };
            if (dlg.ShowDialog(Application.Current.MainWindow) ?? false)
                return Save(dlg.FileName);
            else
                return false;
        }
        public bool CanClose()
        {
            if (Edits != null && Edits.Dirty)
            {
                switch (MessageBox.Show(Application.Current.MainWindow, "Save changes to schematic '" + FileName + "'?", Application.Current.MainWindow.Title, MessageBoxButton.YesNoCancel))
                {
                    case MessageBoxResult.Yes: return Save();
                    case MessageBoxResult.No: return true;
                    case MessageBoxResult.Cancel: return false;
                }
            }
            return true;
        }

        private bool Save(string FileName)
        {
            XDocument doc = new XDocument();
            doc.Add(schematic.Serialize());
            doc.Save(FileName);

            filename = FileName;
            NotifyChanged("FileName");
            Edits.Dirty = false;
            return true;
        }
                        
        private void Save_Executed(object sender, ExecutedRoutedEventArgs e) 
        {
            try
            {
                Save(); 
            }
            catch (Exception ex)
            {
                MessageBox.Show(Application.Current.MainWindow, ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void SaveAs_Executed(object sender, ExecutedRoutedEventArgs e) 
        { 
            try
            {
                SaveAs();
            }
            catch (Exception ex)
            {
                MessageBox.Show(Application.Current.MainWindow, ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static Schematic Open(string FileName)
        {
            XDocument doc = XDocument.Load(FileName);
            return new Schematic(Circuit.Schematic.Deserialize(doc.Root));
        }

        // Edit.
        public void Cut() { Copy(); Remove(Selected); }
        public void Copy()
        {
            XElement copy = new XElement("Schematic");
            foreach (Circuit.Element i in Selected)
                copy.Add(i.Serialize());
            Clipboard.SetData(DataFormats.StringFormat, copy.ToString());
        }
        public void Paste()
        {
            try
            {
                XElement X = XElement.Parse(Clipboard.GetData(DataFormats.StringFormat) as string);

                List<Circuit.Element> copied = X.Elements("Element").Select(i => Circuit.Element.Deserialize(i)).ToList();
                Add(copied);
                Select(copied, false, true);
            }
            catch (System.Exception) { }
        }
        public void Delete() { Remove(Selected); }
        
        private void Delete_CanExecute(object sender, CanExecuteRoutedEventArgs e) { e.CanExecute = !Selected.Empty(); }
        private void SelectAll_CanExecute(object sender, CanExecuteRoutedEventArgs e) { e.CanExecute = !Elements.Empty(); }
        private void Cut_CanExecute(object sender, CanExecuteRoutedEventArgs e) { e.CanExecute = !Selected.Empty(); }
        private void Copy_CanExecute(object sender, CanExecuteRoutedEventArgs e) { e.CanExecute = !Selected.Empty(); }
        private void Paste_CanExecute(object sender, CanExecuteRoutedEventArgs e) { e.CanExecute = Clipboard.ContainsData(DataFormats.StringFormat); }
        private void Undo_CanExecute(object sender, CanExecuteRoutedEventArgs e) { e.CanExecute = Edits.CanUndo(); }
        private void Redo_CanExecute(object sender, CanExecuteRoutedEventArgs e) { e.CanExecute = Edits.CanRedo(); }

        private void Delete_Executed(object sender, ExecutedRoutedEventArgs e) { Remove(Selected); }
        private void SelectAll_Executed(object sender, ExecutedRoutedEventArgs e) { Select(Elements); }
        private void Cut_Executed(object sender, ExecutedRoutedEventArgs e) { Cut(); }
        private void Copy_Executed(object sender, ExecutedRoutedEventArgs e) { Copy(); }
        private void Paste_Executed(object sender, ExecutedRoutedEventArgs e) { Paste(); }
        private void Undo_Executed(object sender, ExecutedRoutedEventArgs e) { Edits.Undo(); }
        private void Redo_Executed(object sender, ExecutedRoutedEventArgs e) { Edits.Redo(); }
        
        // Elements.
        public IEnumerable<Circuit.Element> Elements { get { return schematic.Elements; } }
        public IEnumerable<Circuit.Symbol> Symbols { get { return schematic.Elements.OfType<Circuit.Symbol>(); } }
        public IEnumerable<Circuit.Wire> Wires { get { return schematic.Elements.OfType<Circuit.Wire>(); } }

        public IEnumerable<Circuit.Element> InRect(Circuit.Coord x1, Circuit.Coord x2)
        {
            Circuit.Coord a = new Circuit.Coord(Math.Min(x1.x, x2.x), Math.Min(x1.y, x2.y));
            Circuit.Coord b = new Circuit.Coord(Math.Max(x1.x, x2.x), Math.Max(x1.y, x2.y));
            return Elements.Where(i => i.Intersects(a, b));
        }
        public IEnumerable<Circuit.Element> AtPoint(Circuit.Coord At) { return InRect(At - 1, At + 1); }
        public IEnumerable<Circuit.Element> InRect(Point x1, Point x2) { return InRect(ToCoord(x1), ToCoord(x2)); }
        public IEnumerable<Circuit.Element> AtPoint(Point At) { return AtPoint(ToCoord(At)); }

        private static Circuit.Coord ToCoord(Point x) { return new Circuit.Coord((int)Math.Round(x.X), (int)Math.Round(x.Y)); }

        public static Point LowerBound(IEnumerable<Circuit.Element> Of) { return new Point(Of.Min(i => i.LowerBound.x), Of.Min(i => i.LowerBound.y)); }
        public static Point UpperBound(IEnumerable<Circuit.Element> Of) { return new Point(Of.Max(i => i.UpperBound.x), Of.Max(i => i.UpperBound.y)); }
        public Point LowerBound() { return LowerBound(Elements); }
        public Point UpperBound() { return UpperBound(Elements); }

        public void DoAdd(IEnumerable<Circuit.Element> Elements)
        {
            foreach (Circuit.Element i in Elements)
            {
                Element e = Element.New(i);
                schematic.Elements.Add(i);
                components.Children.Add(e);
                Canvas.SetLeft(e, i.LowerBound.x);
                Canvas.SetTop(e, i.LowerBound.y);

                i.LayoutChanged += OnLayoutChanged;
            }
        }

        public void DoRemove(IEnumerable<Circuit.Element> Elements)
        {
            foreach (Circuit.Element i in Elements)
            {
                i.LayoutChanged -= OnLayoutChanged;

                schematic.Elements.Remove(i);
                components.Children.Remove((Element)i.Tag);
            }
        }

        public void Add(IEnumerable<Circuit.Element> Elements)
        {
            if (!Elements.Any())
                return;
            
            Edits.Do(new AddElements(this, Elements));
            OnSelectionChanged();
        }
        public void Remove(IEnumerable<Circuit.Element> Elements)
        {
            if (!Elements.Any())
                return;
            Edits.Do(new RemoveElements(this, Elements));
            OnSelectionChanged();
        }
        public void Add(params Circuit.Element[] Elements) { Add(Elements.AsEnumerable()); }
        public void Remove(params Circuit.Element[] Elements) { Remove(Elements.AsEnumerable()); }

        private static Circuit.Coord Round(Point x)
        {
            return new Circuit.Coord((int)Math.Round(x.X), (int)Math.Round(x.Y));
        }

        public List<Circuit.Coord> FindWirePath(List<Point> Mouse)
        {
            Circuit.Coord A = Round(Mouse.First());
            Circuit.Coord B = Round(Mouse.Last());

            // Candidate wire paths.
            List<List<Circuit.Coord>> Candidates = new List<List<Circuit.Coord>>()
            {
                new List<Circuit.Coord>() { A, new Circuit.Coord(A.x, B.y), B },
                new List<Circuit.Coord>() { A, new Circuit.Coord(B.x, A.y), B },
            };

            // Find the path with the minimum cost:
            // - Distance from mouse path.
            // - TODO: Avoids existing symbols?
            return Candidates.ArgMin(i =>
            {
                double d = 0.0;
                foreach (Circuit.Coord j in Mouse.Select(x => Round(x)))
                {
                    double dj = double.PositiveInfinity;
                    for (int k = 0; k < i.Count - 1; ++k)
                    {
                        Circuit.Coord a = i[k];
                        Circuit.Coord b = i[k + 1];

                        dj = Math.Min(dj, Distance(a, b, j));
                    }
                    d += dj;
                }
                return d;
            });
        }

        private void BreakWiresAtTerminal(Circuit.Coord x)
        {
            List<Circuit.Wire> wires = Wires.Where(i => 
                x != i.A && x != i.B && 
                Circuit.Wire.PointOnSegment(x, i.A, i.B)).ToList();
            // Find the wires at x and split them.
            foreach (Circuit.Wire i in wires)
            {
                Remove(i);
                AddWire(i.A, x);
                AddWire(x, i.B);
            }
        }

        private IEnumerable<Circuit.Wire> CoincidentWires(Circuit.Coord A, Circuit.Coord B)
        {
            return Wires.Where(i =>
                   Circuit.Wire.PointOnLine(A, i.A, i.B) && Circuit.Wire.PointOnLine(B, i.A, i.B) && (
                       Circuit.Wire.PointOnSegment(A, i.A, i.B) || Circuit.Wire.PointOnSegment(B, i.A, i.B) ||
                       Circuit.Wire.PointOnSegment(i.A, A, B) || Circuit.Wire.PointOnSegment(i.B, A, B)));
        }

        public void AddWire(Circuit.Coord A, Circuit.Coord B)
        {
            if (A == B) return;

            Debug.Assert(A.x == B.x || A.y == B.y);

            Edits.BeginEditGroup();
            
            // Find all of the wires that are parallel and overlapping this wire.
            List<Circuit.Wire> overlapping = CoincidentWires(A, B).ToList();
            bool selected = overlapping.Any(i => ((Wire)i.Tag).Selected);
            
            Circuit.Coord a = new Circuit.Coord(
                overlapping.Min(i => Math.Min(i.A.x, i.B.x), Math.Min(A.x, B.x)),
                overlapping.Min(i => Math.Min(i.A.y, i.B.y), Math.Min(A.y, B.y)));
            Circuit.Coord b = new Circuit.Coord(
                overlapping.Max(i => Math.Max(i.A.x, i.B.x), Math.Max(A.x, B.x)),
                overlapping.Max(i => Math.Max(i.A.y, i.B.y), Math.Max(A.y, B.y)));

            // Find all of the terminals between a and b.
            List<Circuit.Coord> terminals = new List<Circuit.Coord>() { a, b };
            //foreach (Circuit.Element i in InRect(a, b))
            //{
            //    foreach (Circuit.Terminal j in i.Terminals)
            //    {
            //        if (Circuit.Wire.PointOnLine(i.MapTerminal(j), a, b))
            //        {
            //            // If i is not a wire, or it is a wire that is not coincident with a, b, add the terminal to the list.
            //            if (!(i is Circuit.Wire) || 
            //                i.Terminals.Any(k => !Circuit.Wire.PointOnLine(i.MapTerminal(k), a, b)))
            //                terminals.Add(i.MapTerminal(j));
            //        }
            //    }
            //}
            terminals.Sort((t1, t2) => t1.x == t2.x ? t1.y.CompareTo(t2.y) : t1.x.CompareTo(t2.x));
            
            // Remove the original wires, and add new ones between each terminal between a and b.
            Remove(overlapping);
            for (int i = 0; i < terminals.Count - 1; ++i )
                Add(new Circuit.Wire(terminals[i], terminals[i + 1]));
            
            Edits.EndEditGroup();
        }
        public void AddWire(IList<Circuit.Coord> x)
        {
            Edits.BeginEditGroup();
            for (int i = 0; i < x.Count - 1; ++i)
                AddWire(x[i], x[i + 1]);
            Edits.EndEditGroup();
        }

        protected void OnLayoutChanged(object sender, EventArgs e)
        {
            Circuit.Element E = (Circuit.Element)sender;

            
        }

        // Circuit.
        protected List<Circuit.Node> nodes = new List<Circuit.Node>();
        
        public Circuit.Circuit Build(ILog Output)
        {
            return schematic.Circuit;
        }
        
        // Selection.
        public IEnumerable<Circuit.Element> Selected { get { return Elements.Where(i => ((Element)i.Tag).Selected); } }

        private List<EventHandler> selectionChanged = new List<EventHandler>();
        public event EventHandler SelectionChanged
        {
            add { selectionChanged.Add(value); }
            remove { selectionChanged.Remove(value); }
        }
        public void OnSelectionChanged()
        {
            foreach (EventHandler i in selectionChanged)
                i(this, new EventArgs());
        }

        public void Select(IEnumerable<Circuit.Element> ToSelect, bool Only, bool Toggle)
        {
            bool changed = false;
            foreach (Circuit.Element i in Elements)
            {
                if (ToSelect.Contains(i))
                {
                    if (Toggle || !((Element)i.Tag).Selected)
                    {
                        changed = true;
                        ((Element)i.Tag).Selected = !((Element)i.Tag).Selected;
                    }
                }
                else if (Only)
                {
                    if (((Element)i.Tag).Selected)
                    {
                        changed = true;
                        ((Element)i.Tag).Selected = false;
                    }
                }
            }

            if (changed)
                OnSelectionChanged();
        }

        public void Select(IEnumerable<Circuit.Element> ToSelect) { Select(ToSelect, (Keyboard.Modifiers & ModifierKeys.Control) == 0, false); }
        public void Select(params Circuit.Element[] ToSelect) { Select(ToSelect.AsEnumerable(), (Keyboard.Modifiers & ModifierKeys.Control) == 0, false); }

        public void ToggleSelect(IEnumerable<Circuit.Element> ToSelect) { Select(ToSelect, (Keyboard.Modifiers & ModifierKeys.Control) == 0, true); }
        public void ToggleSelect(params Circuit.Element[] ToSelect) { Select(ToSelect.AsEnumerable(), (Keyboard.Modifiers & ModifierKeys.Control) == 0, true); }

        public void Highlight(IEnumerable<Circuit.Element> ToHighlight)
        {
            foreach (Circuit.Element i in Elements) 
                ((Element)i.Tag).Highlighted = ToHighlight.Contains(i);
        }
        public void Highlight(params Circuit.Element[] ToHighlight) { Highlight(ToHighlight.AsEnumerable()); }

        public Circuit.Point SnapToGrid(Circuit.Point x) { return new Circuit.Point(Math.Round(x.x / Grid) * Grid, Math.Round(x.y / Grid) * Grid); }
        public Point SnapToGrid(Point x) { return new Point(Math.Round(x.X / Grid) * Grid, Math.Round(x.Y / Grid) * Grid); }
        public Vector SnapToGrid(Vector x) { return new Vector(Math.Round(x.X / Grid) * Grid, Math.Round(x.Y / Grid) * Grid); }
                
        // Keyboard events.
        void Schematic_KeyDown(object sender, KeyEventArgs e) { e.Handled = Tool.KeyDown(e.Key); }
        void Schematic_KeyUp(object sender, KeyEventArgs e) { e.Handled = Tool.KeyUp(e.Key); }

        void Schematic_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Focus();
            Point at = SnapToGrid(e.GetPosition(this));
            if (e.ChangedButton == MouseButton.Left)
            {
                CaptureMouse();
                Tool.MouseDown(at);
            }
            else
            {
                ReleaseMouseCapture();
                Tool.Cancel();
            }
            e.Handled = true;
        }
        void Schematic_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Point at = SnapToGrid(e.GetPosition(this));
            if (e.ChangedButton == MouseButton.Left)
            {
                ReleaseMouseCapture();
                Tool.MouseUp(at);
            }
            e.Handled = true;
        }
        Point? mouse = null;
        void Schematic_MouseMove(object sender, MouseEventArgs e)
        {
            Point at = e.GetPosition(this);
            if (IsMouseCaptured)
                BringIntoView(new Rect(at - new Vector(AutoScrollBorder, AutoScrollBorder), at + new Vector(AutoScrollBorder, AutoScrollBorder)));
            at = SnapToGrid(at);
            if (!mouse.HasValue || mouse.Value != at)
            {
                mouse = at;
                Tool.MouseMove(at);
                e.Handled = true;
            }
        }
        
        void Schematic_MouseEnter(object sender, MouseEventArgs e)
        {
            Point at = SnapToGrid(e.GetPosition(this));
            mouse = at;
            Tool.MouseEnter(at);
            e.Handled = true;
        }
        void Schematic_MouseLeave(object sender, MouseEventArgs e)
        {
            Point at = SnapToGrid(e.GetPosition(this));
            mouse = null;
            Tool.MouseLeave(at);
            e.Handled = true;
        }

        private static double Distance(Circuit.Coord x1, Circuit.Coord x2) 
        {
            Circuit.Coord dx = x2 - x1;
            return Math.Sqrt(dx * dx); 
        }

        private static double Distance(Circuit.Coord x1, Circuit.Coord x2, Circuit.Coord p)
        {
            // TODO: This is wrong.
            return Math.Min(
                Math.Min(Distance(p, x1), Distance(p, x2)),
                x1.y == x2.y ? Math.Abs(p.y - x1.y) : Math.Abs(p.x - x1.x));
        }

        // INotifyPropertyChanged interface.
        protected void NotifyChanged(string p)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(p));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
