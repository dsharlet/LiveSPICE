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

            // Add Pot controls to all the IControl symbols.
            foreach (Circuit.Symbol i in Schematic.Symbols)
            {
                SymbolControl tag = (SymbolControl)i.Tag;
                Circuit.IControl control = i.Component as Circuit.IControl;
                if (control != null)
                {
                    PotControl pot = new PotControl() { Width = 90, Height = 90, Opacity = 0.25 };
                    overlays.Children.Add(pot);
                    Canvas.SetLeft(pot, Canvas.GetLeft(tag) - pot.Width / 2 + i.Width / 2);
                    Canvas.SetTop(pot, Canvas.GetTop(tag) - pot.Height / 2 + i.Height / 2);

                    pot.Value = control.Value;
                    pot.ValueChanged += x => { control.Value = x; RaiseControlValueChanged(control); };

                    pot.MouseEnter += (o, e) => pot.Opacity = 0.95;
                    pot.MouseLeave += (o, e) => pot.Opacity = 0.25;
                }
            }
        }

        private List<Action<Circuit.IControl>> controlValueChanged = new List<Action<Circuit.IControl>>();
        protected void RaiseControlValueChanged(Circuit.IControl Control) { foreach (Action<Circuit.IControl> i in controlValueChanged) i(Control); }
        public event Action<Circuit.IControl> ControlValueChanged { add { controlValueChanged.Add(value); } remove { controlValueChanged.Remove(value); } }

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
