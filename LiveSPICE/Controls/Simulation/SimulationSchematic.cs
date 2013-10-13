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
    public class SimulationSchematic : SchematicControl
    {
        public SimulationSchematic(Circuit.Schematic Schematic) : base(Schematic)
        {
            InitializeComponent();

            CommandBindings.Add(new CommandBinding(ApplicationCommands.Delete, Delete_Executed, Delete_CanExecute));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.SelectAll, SelectAll_Executed, SelectAll_CanExecute));

            Focusable = true;
            Cursor = Cursors.Cross;

            Tool = new ProbeSelectionTool(this);
        }

        private void Delete_CanExecute(object sender, CanExecuteRoutedEventArgs e) { e.CanExecute = !ProbesOf(Selected).Empty(); }
        private void SelectAll_CanExecute(object sender, CanExecuteRoutedEventArgs e) { e.CanExecute = !ProbesOf(Elements).Empty(); }
        private void Delete_Executed(object sender, ExecutedRoutedEventArgs e) { Schematic.Remove(ProbesOf(Selected).ToList()); }
        private void SelectAll_Executed(object sender, ExecutedRoutedEventArgs e) { Select(ProbesOf(Elements)); }

        public IEnumerable<Probe> Probes { get { return Symbols.Select(i => i.Component).OfType<Probe>(); } }

        public static IEnumerable<Circuit.Element> ProbesOf(IEnumerable<Circuit.Element> Of)
        {
            return Of.OfType<Circuit.Symbol>().Where(i => i.Component is Probe);
        }
    }
}
