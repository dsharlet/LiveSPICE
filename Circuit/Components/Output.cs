using ComputerAlgebra;
using System.ComponentModel;

namespace Circuit
{
    [Category("Generic")]
    [DisplayName("Output")]
    [Description("Output port.")]
    public class Output : TwoTerminal
    {
        private Quantity v0dBFS = new Quantity(1, Units.V);
        [Serialize, Description("Voltage of the full signal level at this component.")]
        public Quantity V0dBFS {  get { return v0dBFS; } set { v0dBFS = value; NotifyChanged(nameof(V0dBFS)); } }

        public Output() { Name = "O1"; }

        /// <summary>
        /// Expression describing the normalized output signal of this component.
        /// </summary>
        public Expression Out { get { return V / V0dBFS; } }

        public override void LayoutSymbol(SymbolLayout Sym)
        {
            base.LayoutSymbol(Sym);

            int w = 10;
            Sym.AddLine(EdgeType.Black, new Coord(-w, 20), new Coord(w, 20));
            Sym.DrawPositive(EdgeType.Black, new Coord(0, 15));
            Sym.AddLine(EdgeType.Black, new Coord(-w, -20), new Coord(w, -20));
            Sym.DrawNegative(EdgeType.Black, new Coord(0, -15));

            Sym.DrawArrow(EdgeType.Black, new Coord(0, 0), new Coord(15, 0), 0.2);

            Sym.DrawText(() => Name, new Point(-2, 0), Alignment.Far, Alignment.Center);
        }

        public override string ToString() { return Name; }
    }

    [Category("Generic")]
    [DisplayName("Speaker")]
    [DefaultProperty("Impedance")]
    [Description("Ideal speaker.")]
    public class Speaker : Output
    {
        private Quantity impedance = new Quantity(Real.Infinity, Units.Ohm);
        [Serialize, Description("Impedance of this speaker.")]
        public Quantity Impedance { get { return impedance; } set { impedance = value; NotifyChanged("Impedance"); } }

        public Speaker() { Name = "S1"; }

        public override void Analyze(Analysis Mna)
        {
            Resistor.Analyze(Mna, Name, Anode, Cathode, Impedance);
        }

        public override void LayoutSymbol(SymbolLayout Sym)
        {
            Sym.AddTerminal(Anode, new Coord(0, 20));
            Sym.AddTerminal(Cathode, new Coord(0, -20));

            Sym.AddWire(Anode, new Coord(0, 10));
            Sym.AddWire(Cathode, new Coord(0, -10));

            Sym.AddLoop(EdgeType.Black,
                new Coord(-10, 10),
                new Coord(5, 10),
                new Coord(15, 20),
                new Coord(15, -20),
                new Coord(5, -10),
                new Coord(-10, -10));

            Sym.DrawText(() => Name, new Coord(17, 0), Alignment.Near, Alignment.Center);
        }

        public override string ToString() { return Name; }
    }
}
