using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;

namespace Circuit
{
    [CategoryAttribute("IO")]
    [DisplayName("Output")]
    public class Output : TwoTerminal
    {
        public Output() { Name = "O1"; }

        public override void Analyze(IList<Equal> Mna, IList<Expression> Unknowns)
        {
            Anode.i = 0;
            Cathode.i = 0;
        }

        protected override void DrawSymbol(SymbolLayout Sym)
        {
            int y = 15;
            Sym.AddWire(Anode, new Coord(0, y));
            Sym.AddWire(Cathode, new Coord(0, -y));

            Sym.InBounds(new Coord(-10, 0), new Coord(10, 0));

            Sym.AddLine(EdgeType.Black, new Coord(-5, y), new Coord(5, y));
            Sym.DrawPositive(EdgeType.Black, new Coord(0, y - 3));
            Sym.AddLine(EdgeType.Black, new Coord(-5, -y), new Coord(5, -y));
            Sym.DrawNegative(EdgeType.Black, new Coord(0, -y + 3));

            Sym.DrawText(Name.ToString(), new Point(0, 0), Alignment.Center, Alignment.Center);
        }
    }
}

