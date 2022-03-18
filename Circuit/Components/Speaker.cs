using ComputerAlgebra;
using System.ComponentModel;

namespace Circuit
{
    /// <summary>
    /// Ideal speakers just model input impedance.
    /// </summary>
    [Category("Generic")]
    [DisplayName("Speaker")]
    [DefaultProperty("Impedance")]
    [Description("Ideal speaker.")]
    public class Speaker : TwoTerminal
    {
        private Quantity v0dBFS = new Quantity(1, Units.V);
        [Serialize, Description("Voltage of the full signal level at this component.")]
        public Quantity V0dBFS {  get { return v0dBFS; } set { v0dBFS = value; NotifyChanged(nameof(V0dBFS)); } }

        private Quantity impedance = new Quantity(Real.Infinity, Units.Ohm);
        [Serialize, Description("Impedance of this speaker.")]
        public Quantity Impedance { get { return impedance; } set { impedance = value; NotifyChanged(nameof(Impedance)); } }

        public Speaker() { Name = "S1"; }

        /// <summary>
        /// Expression describing the normalized output signal of this component.
        /// </summary>
        [Browsable(false)]
        public Expression Out { get { return V / V0dBFS; } }

        public override void Analyze(Analysis Mna)
        {
            Resistor.Analyze(Mna, Name, Anode, Cathode, Impedance);
        }

        protected internal override void LayoutSymbol(SymbolLayout Sym)
        {
            base.LayoutSymbol(Sym);

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
