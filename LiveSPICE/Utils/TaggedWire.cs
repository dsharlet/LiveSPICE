using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;

namespace Circuit
{
    /// <summary>
    /// Named wire.
    /// </summary>
    [DisplayName("Named Wire")]
    [Category("Standard")]
    public class TaggedWire : OneTerminal
    {
        public TaggedWire() { }

        public override void Analyze(IList<Equal> Mna, IList<Expression> Unknowns) { }

        protected override void DrawSymbol(SymbolLayout Sym)
        {
            Sym.AddLoop(EdgeType.Black,
                new Coord(-10, -20),
                new Coord(0, -30),
                new Coord(10, -20),
                new Coord(10, -10),
                new Coord(0, 0),
                new Coord(-10, -10));

            Sym.DrawText(Name, new Coord(10, 10), Alignment.Near, Alignment.Center);
        }
    }
}
