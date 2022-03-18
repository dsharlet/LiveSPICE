using ComputerAlgebra;
using System.ComponentModel;

namespace Circuit
{
    /// <summary>
    /// Ideal voltage source.
    /// </summary>
    [Category("Generic")]
    [DisplayName("Input")]
    [DefaultProperty("Voltage")]
    [Description("Ideal voltage source representing an input port.")]
    public class Input : TwoTerminal
    {
        private Quantity v0dBFS = new Quantity(1, Units.V);
        [Serialize, Description("Voltage of the full signal level at this component.")]
        public Quantity V0dBFS { get { return v0dBFS; } set { v0dBFS = value; NotifyChanged(nameof(V0dBFS)); } }

        public Input() { Name = "V1"; }

        public override void Analyze(Analysis Mna)
        {
            Expression VIn = In * V0dBFS;
            // Assume the initial condition of the input is 0.
            Arrow init = Arrow.New(VIn.Evaluate(t, 0), 0);
            VoltageSource.Analyze(Mna, Anode, Cathode, VIn, init);
        }

        /// <summary>
        /// Expression describing the normalized input signal of this component.
        /// </summary>
        [Browsable(false)]
        public Expression In { get { return DependentVariable(Name, t); } }

        protected internal override void LayoutSymbol(SymbolLayout Sym)
        {
            base.LayoutSymbol(Sym);

            int w = 10;
            Sym.AddLine(EdgeType.Black, new Coord(-w, 20), new Coord(w, 20));
            Sym.DrawPositive(EdgeType.Black, new Coord(0, 15));
            Sym.AddLine(EdgeType.Black, new Coord(-w, -20), new Coord(w, -20));
            Sym.DrawNegative(EdgeType.Black, new Coord(0, -15));

            Sym.DrawText(() => Name, new Point(0, 0), Alignment.Center, Alignment.Center);
        }
    }
}

