using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComputerAlgebra;

namespace Circuit
{
    [Category("Standard")]
    [DisplayName("Output")]
    public class Output : TwoTerminal
    {
        public Output() { Name = "O1"; }

        public override void Analyze(Analysis Mna) { }

        public override void LayoutSymbol(SymbolLayout Sym)
        {
            base.LayoutSymbol(Sym);

            int w = 10;
            Sym.AddLine(EdgeType.Black, new Coord(-w, 20), new Coord(w, 20));
            Sym.DrawPositive(EdgeType.Black, new Coord(0, 15));
            Sym.AddLine(EdgeType.Black, new Coord(-w, -20), new Coord(w, -20));
            Sym.DrawNegative(EdgeType.Black, new Coord(0, -15));

            Sym.DrawText(() => Name.ToString(), new Point(0, 0), Alignment.Center, Alignment.Center);
        }
    }
}

