using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;
using System.ComponentModel;

namespace Circuit
{
    public abstract class Port : OneTerminal
    {
        private Terminal external;
        /// <summary>
        /// The external terminal of this port.
        /// </summary>
        [Browsable(false)]
        public Terminal External { get { return external; } }

        private int number = -1;
        [Serialize]
        [Description("If this terminal is being laid out on an IC, the index of this port. Zero will lay out the port in any unused index.")]
        public int Number { get { return number; } set { number = value; NotifyChanged("Number"); } }

        // Use the name of the external terminal as the name of this port.
        public override string Name { get { return external.Name; } set { external.Name = value; } }

        public Port() { external = new Terminal(this, "X1"); }

        public override void Analyze(Analysis Mna) 
        {
            // Port acts like a perfect conductor.
            Conductor.Analyze(Mna, Terminal, External);
        }
    }

    [Category("Standard")]
    [DisplayName("Input Port")]
    [DefaultProperty("Name")]
    [Description("Represents an input terminal when the schematic is used as a subcircuit.")]
    public class InputPort : Port
    {
        public override void LayoutSymbol(SymbolLayout Sym)
        {
            base.LayoutSymbol(Sym);

            Sym.AddLoop(EdgeType.Black,
                new Coord(-10, 5), new Coord(-5, 5),
                new Coord(0, 0),
                new Coord(-5, -5), new Coord(-10, -5));
            Sym.DrawText(() => Name, new Coord(-5, 7), Alignment.Center, Alignment.Near);
        }
    }

    [Category("Standard")]
    [DisplayName("Output Port")]
    [DefaultProperty("Name")]
    [Description("Represents an output terminal when the schematic is used as a subcircuit.")]
    public class OutputPort : Port
    {
        public override void LayoutSymbol(SymbolLayout Sym)
        {
            base.LayoutSymbol(Sym);

            Sym.AddLoop(EdgeType.Black,
                new Coord(0, 5), new Coord(5, 5),
                new Coord(10, 0),
                new Coord(5, -5), new Coord(0, -5));
            Sym.DrawText(() => Name, new Coord(5, 7), Alignment.Center, Alignment.Near);
        }
    }
}
