using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComputerAlgebra;

namespace Circuit
{
    [Category("Standard")]
    [DisplayName("Named Voltage")]
    public class VoltageDefinition : TwoTerminal
    {
        public VoltageDefinition() { Name = "V1"; }

        public override void Analyze(Analysis Mna) 
        {
            Mna.AddDefinition(Name, Anode.V - Cathode.V); 
        }

        public override void LayoutSymbol(SymbolLayout Sym)
        {
            base.LayoutSymbol(Sym);

            Sym.InBounds(new Coord(-10, -20), new Coord(10, 20));

            Sym.DrawPositive(EdgeType.Black, new Coord(0, 15));
            Sym.DrawNegative(EdgeType.Black, new Coord(0, -15));

            Sym.DrawText(() => Name.ToString(), new Point(0, 0), Alignment.Center, Alignment.Center);
        }
    }

    [Category("Standard")]
    [DisplayName("Named Current")]
    public class CurrentDefinition : TwoTerminal
    {
        public CurrentDefinition() { Name = "I1"; }

        public override void Analyze(Analysis Mna) 
        { 
            Mna.AddDefinition(Name, Conductor.Analyze(Mna, Anode, Cathode));
        }

        public override void LayoutSymbol(SymbolLayout Sym)
        {
            base.LayoutSymbol(Sym);

            Sym.AddWire(Anode, new Coord(0, 7));
            Sym.AddWire(Cathode, new Coord(0, -7));

            Sym.DrawArrow(EdgeType.Black, new Coord(0, -7), new Coord(0, 7), 0.2f);

            Sym.DrawText(() => Name, new Point(5, 0), Alignment.Near, Alignment.Center); 
        }
    }
}

