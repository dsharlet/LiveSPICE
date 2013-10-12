using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;
using System.ComponentModel;

namespace Circuit
{
    [CategoryAttribute("IO")]
    [DisplayName("Input")]
    public class Input : VoltageSource
    {
        public Input() 
        {
            Voltage = Call.New(ExprFunction.New("Vin", t), t);
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

            Sym.DrawText(Voltage.ToString(), new Point(5, 0), Alignment.Near, Alignment.Center);
            Sym.DrawText(Name.ToString(), new Point(-5, 0), Alignment.Far, Alignment.Center);
        }
    }
}

