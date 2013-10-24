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
    public class NamedWire : OneTerminal
    {
        public NamedWire() { }

        private string wire = "Name";
        [SchematicPersistent]
        public string WireName { get { return wire; } set { wire = value; NotifyChanged("Wire");  } }

        public override void Analyze(ModifiedNodalAnalysis Mna) 
        {
            Terminal.i = 0;
        }

        protected override void DrawSymbol(SymbolLayout Sym)
        {
            Sym.InBounds(new Coord(-10, 0), new Coord(10, 0));

            Sym.AddWire(Terminal, new Coord(0, -15));
            Sym.AddWire(new Coord(0, -19), new Coord(0, -22));
            Sym.AddWire(new Coord(0, -27), new Coord(0, -30));

            Sym.DrawText(WireName, new Coord(2, -15), Alignment.Near, Alignment.Center);
        }
    }
}
