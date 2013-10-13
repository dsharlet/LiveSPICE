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
    public class SchematicEditor : Schematic
    {
        public const string FileExtension = ".xml";
        public const int AutoScrollBorder = 1;

        public SchematicEditor() : this(new Circuit.Schematic(Log.Instance)) { }
        public SchematicEditor(string FileName) : this(Circuit.Schematic.Load(FileName, Log.Instance))
        {
            SetFileName(FileName);
        }

        public SchematicEditor(Circuit.Schematic S) : base(S)
        {
            InitializeComponent();

            CommandBindings.Add(new CommandBinding(ApplicationCommands.Save, Save_Executed));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.SaveAs, SaveAs_Executed));

            CommandBindings.Add(new CommandBinding(ApplicationCommands.Delete, Delete_Executed, Delete_CanExecute));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Cut, Cut_Executed, Cut_CanExecute));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, Copy_Executed, Copy_CanExecute));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, Paste_Executed, Paste_CanExecute));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.SelectAll, SelectAll_Executed, SelectAll_CanExecute));

            CommandBindings.Add(new CommandBinding(ApplicationCommands.Undo, Undo_Executed, Undo_CanExecute));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Redo, Redo_Executed, Redo_CanExecute));

            Focusable = true;

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
        private SchematicTool tool;
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
        private string filename = null;
        private void SetFileName(string FileName) { filename = FileName; NotifyChanged(FileName); }
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
            schematic.Save(FileName);
            SetFileName(FileName);
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
                MessageBox.Show(Application.Current.MainWindow, ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(Application.Current.MainWindow, ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static SchematicEditor Open(string FileName)
        {
            return new SchematicEditor(FileName);
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
        public void Add(IEnumerable<Circuit.Element> Elements)
        {
            if (!Elements.Any())
                return;

            Edits.Do(new AddElements(schematic, Elements));
            OnSelectionChanged();
        }
        public void Remove(IEnumerable<Circuit.Element> Elements)
        {
            if (!Elements.Any())
                return;
            Edits.Do(new RemoveElements(schematic, Elements));
            OnSelectionChanged();
        }
        public void Add(params Circuit.Element[] Elements) { Add(Elements.AsEnumerable()); }
        public void Remove(params Circuit.Element[] Elements) { Remove(Elements.AsEnumerable()); }

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
            for (int i = 0; i < terminals.Count - 1; ++i)
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
    }
}
