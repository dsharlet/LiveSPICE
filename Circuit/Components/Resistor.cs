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
    [Category("Standard")]
    [DisplayName("Resistor")]
    [DefaultProperty("Resistance")]
    [Description("Standard resistor.")] 
    public class Resistor : TwoTerminal
    {
        protected Quantity resistance = new Quantity(100, Units.Ohm);
        [Description("Resistance of this resistor.")]
        [Serialize]
        public Quantity Resistance { get { return resistance; } set { if (resistance.Set(value)) NotifyChanged("Resistance"); } }

        public Resistor() { Name = "R1"; }

        public static Expression Analyze(ModifiedNodalAnalysis Mna, Terminal Anode, Terminal Cathode, Expression R)
        {
            // i = V/R
            if (R.IsZero())
            {
                return Conductor.Analyze(Mna, Anode, Cathode);
            }
            else
            {
                Expression i = (Anode.V - Cathode.V) / R;
                Mna.AddPassiveComponent(Anode, Cathode, i);
                return i;
            }
        }

        public override void Analyze(ModifiedNodalAnalysis Mna) { Analyze(Mna, Anode, Cathode, Resistance); }

        public static void Draw(SymbolLayout Sym, double x, double y1, double y2, int N, double Scale)
        {
            double h = y2 - y1;
            
            Sym.DrawFunction(
                EdgeType.Black,
                (t) => x - Scale * (Math.Abs((t + 0.5) % 2 - 1) * 2 - 1),
                (t) => t * h / N + y1,
                0, N, N * 2);
        }
        public static void Draw(SymbolLayout Sym, double x, double y1, double y2, int N) { Draw(Sym, x, y1, y2, N, (y2 - y1) / (N + 1)); }

        public override void LayoutSymbol(SymbolLayout Sym)
        {
            base.LayoutSymbol(Sym);

            Sym.AddWire(Anode, new Coord(0, 16));
            Sym.AddWire(Cathode, new Coord(0, -16));
            Sym.InBounds(new Coord(-10, 0), new Coord(10, 0));

            Draw(Sym, 0, -16, 16, 7);
            
            if (!(resistance.Value is Constant))
                Sym.DrawArrow(EdgeType.Black, new Coord(-6, -15), new Coord(6, 15), 0.1);

            Sym.DrawText(() => Name, new Coord(6, 0), Alignment.Near, Alignment.Center);
            Sym.DrawText(() => resistance.ToString(), new Coord(-6, 0), Alignment.Far, Alignment.Center);
        }
    }
}
