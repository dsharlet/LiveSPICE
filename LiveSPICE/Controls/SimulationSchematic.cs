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

            Focusable = true;
            Cursor = Cursors.Cross;

            Tool = new ProbeTool(this);
        }

        public IEnumerable<Probe> Probes { get { return Symbols.Select(i => i.Component).OfType<Probe>(); } }
    }
}
