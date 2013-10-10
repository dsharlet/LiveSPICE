using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;
using System.ComponentModel;

namespace Circuit
{
    /// <summary>
    /// Ground component, V = 0.
    /// </summary>
    [CategoryAttribute("Standard")]
    [DisplayName("Ground")]
    public class Ground : OneTerminal
    {
        public Ground() { Name = "GND"; }

        public override void Analyze(IList<Equal> Mna, IList<Expression> Unknowns)
        {
            // Nodes connected to ground have V = 0.
            Mna.Add(Equal.New(Terminal.V, Constant.Zero));
            Terminal.i = null;
        }

        protected override void DrawSymbol(SymbolLayout Sym)
        {
            Sym.AddLoop(EdgeType.Black,
                new Coord(-10, 0),
                new Coord(10, 0),
                new Coord(0, -10));
        }
    }
}
