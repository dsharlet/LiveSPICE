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
    /// Interaction logic for Schematic.xaml
    /// </summary>
    public partial class Schematic : UserControl, INotifyPropertyChanged
    {
        public const string FileExtension = ".xml";
        public const int DefaultWidth = 1600;
        public const int DefaultHeight = 1600;
        public const int AutoScrollBorder = 1;

        public int Grid = 10;

        public Schematic()
        {
            InitializeComponent();

            Background = Brushes.LightGray;
            Cursor = Cursors.Cross;
            Focusable = true;
            Width = DefaultWidth;
            Height = DefaultHeight;
            
            PreviewMouseDown += Schematic_MouseDown;
            PreviewMouseUp += Schematic_MouseUp;
            PreviewMouseMove += Schematic_MouseMove;
            MouseLeave += Schematic_MouseLeave;
            MouseEnter += Schematic_MouseEnter;

            PreviewKeyDown += Schematic_KeyDown;
            PreviewKeyUp += Schematic_KeyUp;

            edits = new EditStack();
            edits.Dirtied += OnDirtied;
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
            XElement root = new XElement("Schematic");
            doc.Add(root);

            foreach (Element i in Elements)
                root.Add(i.Serialize());

            root.SetAttributeValue("Width", ActualWidth);
            root.SetAttributeValue("Height", ActualHeight);

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
            Schematic s = new Schematic();

            XDocument doc = XDocument.Load(FileName);
            XElement root = doc.Root;
            try { s.Width = int.Parse(root.Attribute("Width").Value); }
            catch (Exception) { s.Width = DefaultWidth; }
            try { s.Height = int.Parse(root.Attribute("Height").Value); }
            catch (Exception) { s.Height = DefaultHeight; }

            foreach (XElement i in root.Elements("Element"))
                Element.Deserialize(s, i);

            s.edits = new EditStack();

            s.filename = FileName;
            return s;
        }

        // Edit.
        public void Cut() { Copy(); Remove(Selected); }
        public void Copy()
        {
            XElement copy = new XElement("Schematic");
            foreach (Element i in Selected)
                copy.Add(i.Serialize());
            Clipboard.SetData(DataFormats.StringFormat, copy.ToString());
        }
        public void Paste()
        {
            try
            {
                XElement X = XElement.Parse(Clipboard.GetData(DataFormats.StringFormat) as string);

                Edits.BeginEditGroup();
                List<Element> pasted = X.Elements("Element").Select(i => Element.Deserialize(this, i)).ToList();
                Edits.EndEditGroup();

                Select(pasted, false, true);
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
        private Vector One = new Vector(1, 1);
        public IEnumerable<Element> Elements { get { return components.Children.OfType<Element>(); } }
        public IEnumerable<Symbol> Symbols { get { return components.Children.OfType<Symbol>(); } }
        public IEnumerable<Wire> Wires { get { return components.Children.OfType<Wire>(); } }
        public IEnumerable<Element> InRect(Point x1, Point x2)
        {
            Point a = new Point(Math.Min(x1.X, x2.X) + 1, Math.Min(x1.Y, x2.Y) + 1);
            Point b = new Point(Math.Max(x1.X, x2.X) - 1, Math.Max(x1.Y, x2.Y) - 1);
            return Elements.Where(i => i.Intersects(a, b)); 
        }
        public IEnumerable<Element> InRect(Rect In) { return InRect(In.TopLeft, In.BottomRight); }
        public IEnumerable<Element> AtPoint(Point At) { return InRect(At - One, At + One); }
        public static Point LowerBound(IEnumerable<Element> Of) { return new Point(Of.Min(i => i.X), Of.Min(i => i.Y)); }
        public static Point UpperBound(IEnumerable<Element> Of) { return new Point(Of.Max(i => i.X + i.ActualWidth), Of.Max(i => i.Y + i.ActualHeight)); }
        public Point LowerBound() { return LowerBound(Elements); }
        public Point UpperBound() { return UpperBound(Elements); }

        public void Add(IEnumerable<Element> Elements)
        {
            if (!Elements.Any())
                return;
            foreach (Symbol i in Elements.Where(j => j is Symbol))
            {
                Circuit.Component C = i.Component;

                if (Symbols.FirstOrDefault(j => j.Component.Name == C.Name) != null)
                {
                    string Prefix = Regex.Match(C.Name, @"(\D*).*").Groups[1].Value;
                    C.Name = Circuit.Component.UniqueName(Symbols.Select(j => j.Component), Prefix);
                }
            }
            //foreach (Element i in Elements)
            //    i.LayoutChanged += (o, e) => UpdateCircuit(i);

            Edits.Do(new AddElements(this, Elements));
            OnSelectionChanged();
        }
        public void Remove(IEnumerable<Element> Elements)
        {
            if (!Elements.Any())
                return;
            Edits.Do(new RemoveElements(this, Elements));
            OnSelectionChanged();
        }
        public void Add(params Element[] Elements) { Add(Elements.AsEnumerable()); }
        public void Remove(params Element[] Elements) { Remove(Elements.AsEnumerable()); }
       
        public List<Point> FindWirePath(List<Point> Mouse)
        {
            Point A = Mouse.First();
            Point B = Mouse.Last();

            // Candidate wire paths.
            List<List<Point>> Candidates = new List<List<Point>>()
            {
                new List<Point>() { A, new Point(A.X, B.Y), B },
                new List<Point>() { A, new Point(B.X, A.Y), B },
            };

            // Find the path with the minimum cost:
            // - Distance from mouse path.
            // - TODO: Avoids existing symbols?
            return Candidates.ArgMin(i =>
            {
                double d = 0.0;
                foreach (Point j in Mouse)
                {
                    double dj = double.PositiveInfinity;
                    for (int k = 0; k < i.Count - 1; ++k)
                    {
                        Point a = i[k];
                        Point b = i[k + 1];

                        dj = Math.Min(dj, Distance(a, b, j));
                    }
                    d += dj;
                }
                return d;
            });
        }
        public void AddWire(Point A, Point B)
        {
            if (A == B) return;

            Debug.Assert(A.X == B.X || A.Y == B.Y);

            Edits.BeginEditGroup();

            // Find all of the wires that are parallel and overlapping this wire.
            List<Wire> overlapping = Wires.Where(i =>
                Wire.PointOnLine(A, i.A, i.B) && Wire.PointOnLine(B, i.A, i.B) && (
                    Wire.PointOnSegment(A, i.A, i.B) || Wire.PointOnSegment(B, i.A, i.B) ||
                    Wire.PointOnSegment(i.A, A, B) || Wire.PointOnSegment(i.B, A, B))).ToList();

            if (overlapping.Count() > 1)
                Remove(overlapping.Skip(1));

            Wire w;
            if (overlapping.Any())
            {
                w = overlapping.First();
            }
            else
            {
                w = new Wire();
                Add(w);
            }
            w.SetWire(
                new Point(
                    overlapping.Min(i => Math.Min(i.A.X, i.B.X), Math.Min(A.X, B.X)), 
                    overlapping.Min(i => Math.Min(i.A.Y, i.B.Y), Math.Min(A.Y, B.Y))),
                new Point(
                    overlapping.Max(i => Math.Max(i.A.X, i.B.X), Math.Max(A.X, B.X)),
                    overlapping.Max(i => Math.Max(i.A.Y, i.B.Y), Math.Max(A.Y, B.Y))));
            w.Selected = overlapping.Any(i => i.Selected);

            Edits.EndEditGroup();
        }
        public void AddWire(IList<Point> x)
        {
            Edits.BeginEditGroup();
            for (int i = 0; i < x.Count - 1; ++i)
                AddWire(x[i], x[i + 1]);
            Edits.EndEditGroup();
        }
        public void MergeWires()
        {
            Edits.BeginEditGroup();
            List<Wire> wires = Wires.ToList();
            Remove(wires);
            foreach (Wire i in wires)
                AddWire(i.A, i.B);
            Edits.EndEditGroup();
        }

        // Circuit.
        protected List<Circuit.Node> nodes = new List<Circuit.Node>();

        protected void UpdateCircuit(Element At)
        {
            // Find the nodes terminals are connected to.
            Symbol S = At as Symbol;
            if (S != null)
            {
                foreach (Circuit.Terminal i in S.Component.Terminals)
                {
                    try
                    {
                        Point x = S.MapTerminal(i);
                        Circuit.Node node = null;
                        Wire wire = Wires.FirstOrDefault(j => j.ConnectsTo(x));
                        if (wire != null)
                        {
                            if (wire.Node == null)
                                wire.Node = new Circuit.Node();
                            node = wire.Node;
                        }
                        if (i.ConnectedTo != node)
                        {
                            i.ConnectTo(node);
                            S.InvalidateVisual();
                        }
                    }
                    catch (Exception) { }
                }
            }
            Wire W = At as Wire;
            if (W != null && W.Node != null)
            {
                foreach (Circuit.Terminal i in W.Node.Connected.ToList())
                    UpdateCircuit((Symbol)i.Owner.Tag);
            }
        }

        public Circuit.Circuit Build(ILog Output)
        {
            Output.Begin();

            Circuit.Circuit circuit = new Circuit.Circuit();

            Dictionary<Wire, Circuit.Node> nodes = new Dictionary<Wire, Circuit.Node>();

            // Wires form the nodes in the circuit.
            foreach (Wire i in Wires)
            {
                try
                {
                    Circuit.Node node;
                    List<KeyValuePair<Wire, Circuit.Node>> connected = nodes.Where(j => i.ConnectsTo(j.Key.A, j.Key.B) || j.Key.ConnectsTo(i.A, i.B)).ToList();
                    if (connected.Any())
                    {
                        node = connected.First().Value;
                        foreach (KeyValuePair<Wire, Circuit.Node> j in connected)
                            nodes[j.Key] = node;
                    }
                    else
                    {
                        node = new Circuit.Node();
                        circuit.Nodes.Add(node);
                    }
                    nodes[i] = node;
                }
                catch (Exception ex)
                {
                    Output.Write(LogType.Error, ex.Message);
                }
            }

            // Find the nodes terminals are connected to.
            foreach (Symbol i in Symbols)
            {
                foreach (Circuit.Terminal j in i.Component.Terminals)
                {
                    try
                    {
                        Point x = i.MapTerminal(j);
                        Circuit.Node node = nodes.SingleOrDefault(k => k.Key.ConnectsTo(x)).Value;
                        if (node == null)
                            Output.Write(LogType.Warning, "Unconnected terminal '" + j.ToString() + "'.");
                        j.ConnectTo(node);
                    }
                    catch (Exception ex)
                    {
                        Output.Write(LogType.Error, ex.Message);
                    }
                }
                circuit.Components.Add(i.Component);
            }

            if (Output.End())
            {
                Output.Write(LogType.Info, "Build Successful.");
                Output.Write(LogType.Info, "Nodes: ");
                foreach (Circuit.Node i in circuit.Nodes)
                    Output.Write(LogType.Info, "   '" + i.Name + "' connected to " + i.Connected.Select(k => "'" + k.ToString() + "'").UnSplit(", "));
            }
            else
            {
                Output.Write(LogType.Info, "Build Failed.");
            }

            foreach (Element i in Elements)
                i.InvalidateVisual();

            return circuit;
        }
        
        // Selection.
        public IEnumerable<Element> Selected { get { return Elements.Where(i => i.Selected); } }

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

        public void Select(IEnumerable<Element> ToSelect, bool Only, bool Toggle)
        {
            bool changed = false;
            foreach (Element i in Elements)
            {
                if (ToSelect.Contains(i))
                {
                    if (Toggle || !i.Selected)
                    {
                        changed = true;
                        i.Selected = !i.Selected;
                    }
                }
                else if (Only)
                {
                    if (i.Selected)
                    {
                        changed = true;
                        i.Selected = false;
                    }
                }
            }

            if (changed)
                OnSelectionChanged();
        }

        public void Select(IEnumerable<Element> ToSelect) { Select(ToSelect, (Keyboard.Modifiers & ModifierKeys.Control) == 0, false); }
        public void Select(params Element[] ToSelect) { Select(ToSelect.AsEnumerable(), (Keyboard.Modifiers & ModifierKeys.Control) == 0, false); }

        public void ToggleSelect(IEnumerable<Element> ToSelect) { Select(ToSelect, (Keyboard.Modifiers & ModifierKeys.Control) == 0, true); }
        public void ToggleSelect(params Element[] ToSelect) { Select(ToSelect.AsEnumerable(), (Keyboard.Modifiers & ModifierKeys.Control) == 0, true); }

        public void Highlight(IEnumerable<Element> ToHighlight)
        {
            foreach (Element i in Elements) 
                i.Highlighted = ToHighlight.Contains(i);
        }
        public void Highlight(params Element[] ToHighlight) { Highlight(ToHighlight.AsEnumerable()); }

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
        
        private static double Distance(Point x1, Point x2, Point p)
        {
            // http://stackoverflow.com/questions/849211/shortest-distance-between-a-point-and-a-line-segment
            // Return minimum distance between line segment vw and point p
            double l2 = (x1 - x2).LengthSquared;  // i.e. |w-v|^2 -  avoid a sqrt
            if (l2 == 0.0)
                return (p - x1).Length;   // v == w case

            // Consider the line extending the segment, parameterized as v + t (w - v).
            // We find projection of point p onto the line. 
            // It falls where t = [(p-v) . (w-v)] / |w-v|^2
            double t = Vector.Multiply(p - x1, x2 - x1) / l2;
            if (t < 0.0) return (p - x1).Length;       // Beyond the 'v' end of the segment
            else if (t > 1.0) return (p - x2).Length;  // Beyond the 'w' end of the segment
            Point proj = x1 + t * (x2 - x1);  // Projection falls on the segment
            return (p - proj).Length;
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
