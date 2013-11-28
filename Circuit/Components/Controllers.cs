using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComputerAlgebra;

namespace Circuit
{
    [Category("Standard")]
    [DisplayName("Voltage Controller")]
    public class VoltageController : TwoTerminal
    {
        public VoltageController() { Name = "V1"; }

        public override void Analyze(Analysis Mna) 
        {
            Mna.AddDefinition("V[" + Name + "]", Anode.V - Cathode.V); 
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
    [DisplayName("Current Controller")]
    public class CurrentController : TwoTerminal
    {
        public CurrentController() { Name = "I1"; }

        public override void Analyze(Analysis Mna) 
        { 
            Mna.AddDefinition("i[" + Name + "]", Conductor.Analyze(Mna, Anode, Cathode));
        }

        public override void LayoutSymbol(SymbolLayout Sym)
        {
            base.LayoutSymbol(Sym);

            Sym.AddWire(Anode, new Coord(0, 7));
            Sym.AddWire(Cathode, new Coord(0, -7));

            //Sym.AddCircle(EdgeType.Black, new Coord(0, 0), r);
            Sym.DrawArrow(EdgeType.Black, new Coord(0, -7), new Coord(0, 7), 0.2f);

            Sym.DrawText(() => Name, new Point(5, 0), Alignment.Near, Alignment.Center); 
        }
    }
}

