using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComputerAlgebra;
using System.ComponentModel;

namespace Circuit
{
    /// <summary>
    /// Buffer that delays signal to the previous timestep.
    /// </summary>
    [Category("Standard")]
    [DisplayName("Delay Buffer")]
    [Description("Ideal voltage follower with a single sample of delay.")]
    public class DelayBuffer : TwoTerminal
    {
        public override void Analyze(Analysis Mna)
        {
            // Unknown output current.
            Mna.AddTerminal(Cathode, Mna.AddUnknown("i" + Name));
            // V-[t] = V+[t0], i.e. the voltage at the previous timestep.
            Mna.AddEquation(Anode.V.Evaluate(t, t0), Cathode.V);
        }

        public override void LayoutSymbol(SymbolLayout Sym)
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
