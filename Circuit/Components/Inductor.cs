using ComputerAlgebra;
using System;
using System.ComponentModel;

namespace Circuit
{
    /// <summary>
    /// Inductor is a linear component with V = L*di/dt.
    /// </summary>
    [Category("Generic")]
    [DisplayName("Inductor")]
    [DefaultProperty("Inductance")]
    [Description("Standard inductor.")]
    public class Inductor : TwoTerminal
    {
        protected Quantity inductance = new Quantity(100e-6m, Units.H);
        [Serialize, Description("Inductance of this inductor.")]
        public Quantity Inductance { get { return inductance; } set { if (inductance.Set(value)) NotifyChanged(nameof(Inductance)); } }

        public Inductor() { Name = "L1"; }

        public static Expression Analyze(Analysis Mna, string Name, Node Anode, Node Cathode, Expression L)
        {
            // Define a new unknown for the current through the inductor.
            Expression i = Mna.AddUnknown("i" + Name);
            Mna.AddPassiveComponent(Anode, Cathode, i);
            // V = L*di/dt
            Mna.AddEquation(Anode.V - Cathode.V, L * D(i, t));

            return i;
        }
        public static Expression Analyze(Analysis Mna, Node Anode, Node Cathode, Expression L) { return Analyze(Mna, Mna.AnonymousName(), Anode, Cathode, L); }

        public override void Analyze(Analysis Mna) { Analyze(Mna, Name, Anode, Cathode, Inductance); }

        public static void Draw(SymbolLayout Sym, double x, double y1, double y2, int Turns, double Scale)
        {
            double t1 = -Math.PI;
            double t2 = 2 * (Turns - 1) * Math.PI;
            double coil = 1.5;

            double min = t1 + coil * Math.Cos(t1);
            double max = t2 + coil * Math.Cos(t2);
            double length = max - min;

            Sym.DrawFunction(
                EdgeType.Black,
                (t) => x - Scale * Math.Sin(t),
                (t) => (t + coil * Math.Cos(t) - min) / length * (y2 - y1) + y1,
                t1, t2, Turns * 16);
        }
        public static void Draw(SymbolLayout Sym, double x, double y1, double y2, int Turns) { Draw(Sym, x, y1, y2, Turns, (y2 - y1) / (Turns * 2)); }

        protected internal override void LayoutSymbol(SymbolLayout Sym)
        {
            base.LayoutSymbol(Sym);

            Sym.AddWire(Anode, new Coord(0, 16));
            Sym.AddWire(Cathode, new Coord(0, -16));
            Sym.InBounds(new Coord(-10, 0), new Coord(10, 0));

            Draw(Sym, 0.0, -16.0, 16.0, 4);

            Sym.DrawText(() => Name, new Coord(6, 0), Alignment.Near, Alignment.Center);
            Sym.DrawText(() => inductance.ToString(), new Coord(-6, 0), Alignment.Far, Alignment.Center);
        }
    }
}
