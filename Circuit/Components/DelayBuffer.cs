using ComputerAlgebra;
using System.ComponentModel;

namespace Circuit
{
    /// <summary>
    /// Buffer that delays signal to the previous timestep.
    /// </summary>
    [Category("Generic")]
    [DisplayName("Delay Buffer")]
    [Description("Ideal voltage follower with a single sample of delay.")]
    public class DelayBuffer : TwoTerminal
    {
        public override void Analyze(Analysis Mna)
        {
            // Unknown output current.
            Mna.AddTerminal(Cathode, Mna.AddUnknown("i" + Name));
            // V-[t] = V+[t - T], i.e. the voltage at the previous timestep.
            Mna.AddEquation(Anode.V.Evaluate(t, t - T), Cathode.V);
        }

        protected internal override void LayoutSymbol(SymbolLayout Sym)
        {
            base.LayoutSymbol(Sym);

            Sym.AddWire(Anode, new Coord(0, 10));
            Sym.AddWire(Cathode, new Coord(0, -6));

            Sym.AddLoop(EdgeType.Black,
                new Coord(-10, 10),
                new Coord(10, 10),
                new Coord(2, -6),
                new Coord(-2, -6));

            Sym.DrawText(() => Name, new Coord(10, 0), Alignment.Near, Alignment.Center);
        }
    }
}
