using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;
using System.ComponentModel;

namespace Circuit
{
    [Category("Standard")]
    [DisplayName("Buffer")]
    [Description("Ideal voltage follower.")]
    public class Buffer : TwoTerminal
    {
        public override void Analyze(ModifiedNodalAnalysis Mna)
        {
            // Unknown output current.
            Mna.AddTerminal(Cathode, Mna.AddNewUnknown("i" + Name));
            // Follow voltage.
            Mna.AddEquation(Anode.V, Cathode.V);
        }

        public override void LayoutSymbol(SymbolLayout Sym)
        {
            base.LayoutSymbol(Sym);

            Sym.AddWire(Anode, new Coord(0, 10));
            Sym.AddWire(Cathode, new Coord(0, -10));

            Sym.AddLoop(EdgeType.Black,
                new Coord(-10, 10),
                new Coord(10, 10),
                new Coord(0, -10));

            Sym.DrawText(() => Name, new Coord(10, 0), Alignment.Near, Alignment.Center);
        }
    }
}
