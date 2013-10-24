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
    public class SchematicEditor : SchematicControl
    {
        public const string FileExtension = ".xml";

        public SchematicEditor() : this(new Circuit.Schematic()) { }
        public SchematicEditor(string FileName) : this(Circuit.Schematic.Load(FileName))
        {
            SetFileName(FileName);
        }

        public SchematicEditor(Circuit.Schematic Schematic) : base(Schematic)
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
            Cursor = Cursors.Cross;

            edits = new EditStack();
            edits.Dirtied += OnDirtied;

            Tool = new SelectionTool(this);

            Width = 1600;
            Height = 1600;
        }
        
        private EditStack edits;
        public EditStack Edits { get { return edits; } }
        private void OnDirtied(object sender, EventArgs e) { }

        // File.
        private string filename = null;
        private void SetFileName(string FileName) { filename = FileName; NotifyChanged(FileName); }
        public string FileName { get { return filename == null ? "Untitled" : System.IO.Path.GetFileNameWithoutExtension(filename); } }
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
            Schematic.Save(FileName);
            SetFileName(FileName);
            Edits.Dirty = false;
            App.Current.Used(filename);
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
            try
            {
                SchematicEditor editor = new SchematicEditor(FileName);
                App.Current.Used(FileName);
                return editor;
            }
            catch (System.Exception)
            {
                App.Current.RemoveFromMru(FileName);
                throw;
            }
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
                Select(copied, true, false);
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
            Edits.Do(new AddElements(Schematic, Elements));
        }
        public void Remove(IEnumerable<Circuit.Element> Elements)
        {
            if (!Elements.Any())
                return;
            Edits.Do(new RemoveElements(Schematic, Elements));
        }
        public void Add(params Circuit.Element[] Elements) { Add(Elements.AsEnumerable()); }
        public void Remove(params Circuit.Element[] Elements) { Remove(Elements.AsEnumerable()); }

        public List<Circuit.Coord> FindWirePath(List<Circuit.Coord> Mouse)
        {
            Circuit.Coord A = Mouse.First();
            Circuit.Coord B = Mouse.Last();

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
                foreach (Circuit.Coord j in Mouse)
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
            bool selected = overlapping.Any(i => ((WireControl)i.Tag).Selected);

            Circuit.Coord a = new Circuit.Coord(
                overlapping.Min(i => Math.Min(i.A.x, i.B.x), Math.Min(A.x, B.x)),
                overlapping.Min(i => Math.Min(i.A.y, i.B.y), Math.Min(A.y, B.y)));
            Circuit.Coord b = new Circuit.Coord(
                overlapping.Max(i => Math.Max(i.A.x, i.B.x), Math.Max(A.x, B.x)),
                overlapping.Max(i => Math.Max(i.A.y, i.B.y), Math.Max(A.y, B.y)));

            List<Circuit.Coord> terminals = new List<Circuit.Coord>() { a, b };
            foreach (Circuit.Element i in InRect(a - 1, b + 1))
            {
                // Find all of the terminals between a and b.
                foreach (Circuit.Terminal j in i.Terminals)
                {
                    if (Circuit.Wire.PointOnSegment(i.MapTerminal(j), a, b))
                    {
                        // If i is not a wire, or it is a wire that is not coincident with a, b, add the terminal to the list.
                        if (!(i is Circuit.Wire) ||
                            i.Terminals.Any(k => !Circuit.Wire.PointOnLine(i.MapTerminal(k), a, b)))
                            terminals.Add(i.MapTerminal(j));
                    }
                }

                // If i is a Wire that crosses a, b, add a terminal at the intersection.
                if (i is Circuit.Wire)
                {
                    Circuit.Wire w = (Circuit.Wire)i;

                    Circuit.Coord ia = w.MapTerminal(w.Anode);
                    Circuit.Coord ib = w.MapTerminal(w.Cathode);

                    // If one of A, B is intersecting this wire, we shouldn't merge wires across this point.
                    if (Circuit.Wire.PointOnLine(A, ia, ib) && !Circuit.Wire.PointOnLine(B, ia, ib))
                        terminals.Add(A);
                    else if (Circuit.Wire.PointOnLine(B, ia, ib) && !Circuit.Wire.PointOnLine(A, ia, ib))
                        terminals.Add(B);                        
                }
            }
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
