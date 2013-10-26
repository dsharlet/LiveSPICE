using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;
using System.ComponentModel;

namespace Circuit
{ 
    /// <summary>
    /// Resistor is a linear component with V = R*i.
    /// </summary>
    [CategoryAttribute("Standard")]
    [DisplayName("Resistor")]
    public class Resistor : TwoTerminal
    {
        protected Quantity resistance = new Quantity(100, Units.Ohm);
        [Description("Resistance of this resistor.")]
        [SchematicPersistent]
        public Quantity Resistance { get { return resistance; } set { if (resistance.Set(value)) NotifyChanged("Resistance"); } }

        public Resistor() { Name = "R1"; }

        public static Expression Analyze(ModifiedNodalAnalysis Mna, Expression V, Expression R)
        {
            // i = V/R
            if (R.Equals(Real.Infinity))
                return 0;
            else if (R.IsZero())
                return Conductor.Analyze(Mna, V);

            return V / R;
        }

        public override void Analyze(ModifiedNodalAnalysis Mna) { i = Analyze(Mna, V, Resistance); }

        
        protected override void DrawSymbol(SymbolLayout Sym)
        {
            Sym.AddWire(Anode, new Coord(0, 16));
            Sym.AddWire(Cathode, new Coord(0, -16));
            Sym.InBounds(new Coord(-10, 0), new Coord(10, 0));

            int N = 7;
            Sym.DrawFunction(
                EdgeType.Black,
                (t) => Math.Abs((t + 0.5) % 2 - 1) * 8 - 4, 
                (t) => t * 32 / N - 16,
                0, N, N * 2);

            if (!(resistance.Value is Constant))
                Sym.DrawArrow(EdgeType.Black, new Coord(-6, -15), new Coord(6, 15), 0.1);

            Sym.DrawText(Name, new Coord(6, 0), Alignment.Near, Alignment.Center);
            Sym.DrawText(resistance.ToString(), new Coord(-6, 0), Alignment.Far, Alignment.Center);
        }

        public override string ToString() { return Name + " = " + resistance.ToString(); }
    }
}
