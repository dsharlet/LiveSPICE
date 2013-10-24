using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;
using System.ComponentModel;

namespace Circuit
{
    /// <summary>
    /// Buffer that delays signal to the previous timestep.
    /// </summary>
    [CategoryAttribute("Standard")]
    [DisplayName("Delay Buffer")]
    public class DelayBuffer : TwoTerminal
    {
        public override void Analyze(ModifiedNodalAnalysis Mna)
        {
            // Infinite input impedance.
            Anode.i = Constant.Zero;

            // Unknown output current.
            Cathode.i = Mna.AddNewUnknown("i" + Name);

            // -V[t] = +V[t0], i.e. the voltage at the previous timestep.
            Mna.AddEquation(Anode.V.Evaluate(t, t0), Cathode.V);
        }

        protected override void DrawSymbol(SymbolLayout Sym)
        {
            Sym.AddWire(Anode, new Coord(0, 10));
            Sym.AddWire(Cathode, new Coord(0, -10));

            Sym.AddLoop(EdgeType.Black,
                new Coord(-10, 10),
                new Coord(10, 10),
                new Coord(0, -10));

            Sym.DrawText(Name, new Coord(10, 0), Alignment.Near, Alignment.Center);
        }
    }
}
