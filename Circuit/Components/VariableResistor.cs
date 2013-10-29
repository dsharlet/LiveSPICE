using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;
using System.ComponentModel;

namespace Circuit
{ 
    [CategoryAttribute("Controls")]
    public class VariableResistor : Resistor
    {
        protected Expression wipe = 0.5m;
        [Serialize]
        public Expression Wipe { get { return wipe; } set { wipe = value; NotifyChanged("Wipe"); } }

        public VariableResistor() { }

        public override void Analyze(ModifiedNodalAnalysis Mna)
        {
            Resistor.Analyze(Mna, Anode, Cathode, Resistance.Value * Wipe);
        }
        
        protected override void DrawSymbol(SymbolLayout Sym)
        {
            Sym.AddWire(Anode, new Coord(0, 16));
            Sym.AddWire(Cathode, new Coord(0, -16));
            Sym.InBounds(new Coord(-10, 0), new Coord(10, 0));

            Sym.DrawArrow(EdgeType.Black, new Coord(-6, -15), new Coord(6, 15), 0.1);

            int N = 7;
            Sym.DrawFunction(
                EdgeType.Black,
                (t) => Math.Abs((t + 0.5) % 2 - 1) * 8 - 4,
                (t) => t * 32 / N - 16,
                0, N, N * 2);

            Sym.DrawText(resistance.ToString(), new Coord(-7, 0), Alignment.Far, Alignment.Center);
            Sym.DrawText(wipe.ToString(), new Coord(9, 3), Alignment.Near, Alignment.Near);
            Sym.DrawText(Name, new Coord(9, -3), Alignment.Near, Alignment.Far);
        }

        public override string ToString() { return Name + " = " + resistance.ToString(); }
    }
}
