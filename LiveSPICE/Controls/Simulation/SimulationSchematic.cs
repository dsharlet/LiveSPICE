using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using SchematicControls;

namespace LiveSPICE
{
    /// <summary>
    /// Control for interacting with a Circuit.Schematic.
    /// </summary>
    class SimulationSchematic : SchematicControl
    {
        public SimulationSchematic(Circuit.Schematic Schematic) : base(Schematic)
        {
            InitializeComponent();

            CommandBindings.Add(new CommandBinding(ApplicationCommands.Delete, Delete_Executed, Delete_CanExecute));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.SelectAll, SelectAll_Executed, SelectAll_CanExecute));

            Focusable = true;
            Cursor = Cursors.Cross;

            Tool = new ProbeTool(this);

            int pad = Grid * 2;
            int align = Grid * 10;

            Circuit.Coord lb = Schematic.LowerBound;
            Circuit.Coord ub = Schematic.UpperBound;
            lb = Floor(lb - pad, align);
            ub = Ceiling(ub + pad, align);

            Width = ub.x - lb.x;
            Height = ub.y - lb.y;
            Origin = -lb;
        }

        private void Delete_CanExecute(object sender, CanExecuteRoutedEventArgs e) { e.CanExecute = !ProbesOf(Selected).Empty(); }
        private void SelectAll_CanExecute(object sender, CanExecuteRoutedEventArgs e) { e.CanExecute = !ProbesOf(Elements).Empty(); }
        private void Delete_Executed(object sender, ExecutedRoutedEventArgs e) { Schematic.Remove(ProbesOf(Selected).ToList()); }
        private void SelectAll_Executed(object sender, ExecutedRoutedEventArgs e) { Select(ProbesOf(Elements)); }

        public IEnumerable<Probe> Probes { get { return Symbols.Select(i => i.Component).OfType<Probe>(); } }

        public static IEnumerable<Circuit.Symbol> ProbesOf(IEnumerable<Circuit.Element> Of)
        {
            return Of.OfType<Circuit.Symbol>().Where(i => i.Component is Probe);
        }
    }
}
