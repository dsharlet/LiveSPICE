using System.ComponentModel;

namespace Circuit
{
    [Category("Generic")]
    [DisplayName("Port")]
    [DefaultProperty("Name")]
    [Description("Represents an input terminal when the schematic is used as a subcircuit.")]
    public class Port : OneTerminal
    {
        private Terminal external;
        /// <summary>
        /// The external terminal of this port.
        /// </summary>
        [Browsable(false)]
        public Terminal External { get { return external; } }

        private int number = 0;
        [Serialize, Description("If this terminal is being laid out on an IC, this is the index of this port. Zero will lay out the port in any unused index.")]
        public int Number { get { return number; } set { number = value; NotifyChanged(nameof(Number)); } }

        // Use the name of the external terminal as the name of this port.
        [Serialize]
        public override string Name { get { return external.Name; } set { external.Name = value; } }

        public Port() { external = new Terminal(this, "X1"); }

        public override void Analyze(Analysis Mna)
        {
            // Port acts like a perfect conductor.
            Conductor.Analyze(Mna, Terminal, External);
        }

        protected internal override void LayoutSymbol(SymbolLayout Sym)
        {
            base.LayoutSymbol(Sym);

            Sym.InBounds(new Coord(-10, -10), new Coord(10, 10));

            Sym.AddRectangle(EdgeType.Black, new Coord(-5, -5), new Coord(5, 5));
            Sym.DrawText(() => Name, new Coord(0, 7), Alignment.Center, Alignment.Near);
        }
    }
}
