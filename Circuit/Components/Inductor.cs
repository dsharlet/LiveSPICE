using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;
using System.ComponentModel;

namespace Circuit
{ 
    /// <summary>
    /// Inductor is a linear component with V = L*di/dt.
    /// </summary>
    [CategoryAttribute("Standard")]
    [DisplayName("Inductor")]
    public class Inductor : TwoTerminal
    {
        protected Quantity inductance = new Quantity(100e-6m, Units.H);
        [Description("Inductance of this inductor.")]
        [Serialize]
        public Quantity Inductance { get { return inductance; } set { if (inductance.Set(value)) NotifyChanged("Inductance"); } }

        public Inductor() { Name = "L1"; }

        public static Expression Analyze(ModifiedNodalAnalysis Mna, Expression V, Expression L)
        {
            // Define a new unknown for the current through the inductor.
            Expression i = Mna.AddNewUnknown();
            // V = L*di/dt
            Mna.AddEquation(V, L * D(i, t));

            return i;
        }

        public override void Analyze(ModifiedNodalAnalysis Mna) { i = Analyze(Mna, V, Inductance); }

        public static void DrawCoil(SymbolLayout Sym, double x, double y1, double y2, int Turns, bool Mirror)
        {
            double t1 = -3.141592;
            double t2 = 2 * (Turns - 1) * 3.141592;
            double coil = 1.5;

            double min = t1 + coil * Math.Cos(t1);
            double max = t2 + coil * Math.Cos(t2);
            double length = max - min;

            double sign = Mirror ? -1.0 : 1.0;

            Sym.DrawFunction(
                EdgeType.Black,
                (t) => x + sign * Math.Sin(t) * 4.0,
                (t) => (t + coil * Math.Cos(t) - min) / length * (y2 - y1) + y1,
                t1, t2, Turns * 16);
        }

        protected override void DrawSymbol(SymbolLayout Sym)
        {
            Sym.AddWire(Anode, new Coord(0, 16));
            Sym.AddWire(Cathode, new Coord(0, -16));
            Sym.InBounds(new Coord(-10, 0), new Coord(10, 0));

            DrawCoil(Sym, 0.0, -16.0, 16.0, 4, false);
            
            Sym.DrawText(Name, new Coord(6, 0), Alignment.Near, Alignment.Center);
            Sym.DrawText(inductance.ToString(), new Coord(-6, 0), Alignment.Far, Alignment.Center);
        }

        public override string ToString() { return Name + " = " + inductance.ToString(); }
    }
}
