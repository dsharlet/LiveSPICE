using ComputerAlgebra;
using System;
using System.ComponentModel;

namespace Circuit
{
    /// <summary>
    /// Resistor is a linear component with V = R*i.
    /// </summary>
    [Category("Generic")]
    [DisplayName("Resistor")]
    [DefaultProperty("Resistance")]
    [Description("Standard resistor.")]
    public class Resistor : TwoTerminal
    {
        protected Quantity resistance = new Quantity(100, Units.Ohm);
        [Serialize, Description("Resistance of this resistor.")]
        public Quantity Resistance { get { return resistance; } set { if (resistance.Set(value)) NotifyChanged(nameof(Resistance)); } }

        public Resistor() { Name = "R1"; }

        public static Expression Analyze(Analysis Mna, string Name, Node Anode, Node Cathode, Expression R)
        {
            // i = V/R
            if (R.EqualsZero())
            {
                return Conductor.Analyze(Mna, Name, Anode, Cathode);
            }
            else
            {
                Expression i = (Anode.V - Cathode.V) / R;
                Mna.AddPassiveComponent(Anode, Cathode, i);
                return i;
            }
        }
        public static Expression Analyze(Analysis Mna, Node Anode, Node Cathode, Expression R) { return Analyze(Mna, "", Anode, Cathode, R); }

        public override void Analyze(Analysis Mna) { Analyze(Mna, Name, Anode, Cathode, Resistance); }

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

        protected internal override void LayoutSymbol(SymbolLayout Sym)
        {
            base.LayoutSymbol(Sym);

            Sym.AddWire(Anode, new Coord(0, 16));
            Sym.AddWire(Cathode, new Coord(0, -16));
            Sym.InBounds(new Coord(-10, 0), new Coord(10, 0));

            Draw(Sym, 0, -16, 16, 7);

            Sym.DrawText(() => Name, new Coord(6, 0), Alignment.Near, Alignment.Center);
            Sym.DrawText(() => resistance.ToString(), new Coord(-6, 0), Alignment.Far, Alignment.Center);
        }
    }
}
