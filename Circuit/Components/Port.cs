using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;
using System.ComponentModel;

namespace Circuit
{
    /// <summary>
    /// Circuit port component.
    /// </summary>
    [CategoryAttribute("IO")]
    [DisplayName("Port")]
    [Description("Represents a terminal when the schematic is used as a subcircuit.")]
    public class Port : OneTerminal
    {
        public override void Analyze(ModifiedNodalAnalysis Mna) { }

        protected override void DrawSymbol(SymbolLayout Sym)
        {
            Sym.AddRectangle(EdgeType.Black, new Coord(-5, -5), new Coord(5, 5));

            Sym.DrawText(Name, new Coord(0, 7), Alignment.Center, Alignment.Near);
        }
    }
}
