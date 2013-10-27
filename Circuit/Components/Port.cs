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
        private Terminal external;
        /// <summary>
        /// The external terminal of this port.
        /// </summary>
        [Browsable(false)]
        public Terminal External { get { return external; } }

        // Use the name of the external terminal as the name of this port.
        public override string Name { get { return external.Name; } set { external.Name = value; } }

        public Port() { external = new Terminal(this, "X1"); }

        public override void Analyze(ModifiedNodalAnalysis Mna) 
        {
            Mna.AddEquation(V, external.V);
            Mna.AddEquation(i, -external.i);
        }

        protected override void DrawSymbol(SymbolLayout Sym)
        {
            Sym.AddRectangle(EdgeType.Black, new Coord(-5, -5), new Coord(5, 5));

            Sym.DrawText(Name, new Coord(0, 7), Alignment.Center, Alignment.Near);
        }
    }
}
