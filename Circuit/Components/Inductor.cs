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
        [SchematicPersistent]
        public Quantity Inductance { get { return inductance; } set { if (inductance.Set(value)) NotifyChanged("Inductance"); } }

        public Inductor() { Name = "L1"; }

        // Inductor is modeled as a voltage source where V = L*di/dt.
        public override void Analyze(IList<Equal> Mna, IList<Expression> Unknowns)
        {
            // Define a new unknown for the current through this inductor.
            Expression i = Call.New(ExprFunction.New("i" + Name, t), t);
            Anode.i = i;
            Cathode.i = -i;
            Unknowns.Add(i);

            // V = i*di/dt
            Expression di_dt = D(i, t);
            Mna.Add(Equal.New(Anode.V - Cathode.V, inductance.Value * di_dt));
            Unknowns.Add(di_dt);
        }

        protected override void DrawSymbol(SymbolLayout Sym)
        {
            Sym.AddWire(Anode, new Coord(0, 16));
            Sym.AddWire(Cathode, new Coord(0, -16));
            Sym.InBounds(new Coord(-10, 0), new Coord(10, 0));

            float t1 = -3.0f * 3.141592f;
            float t2 = 4.0f * 3.141592f;
            float coil = 1.5f;

            float y1 = t1 + coil * (float)Math.Cos(t1);
            float y2 = t2 + coil * (float)Math.Cos(t2);

            Sym.DrawFunction(
                ShapeType.Black,
                (t) => (float)Math.Sin(t) * 4.0f, 
                (t) => ((t + coil * (float)Math.Cos(t)) - y1) / (y2 - y1) * 32.0f - 16.0f, 
                t1, t2, 64);

            Sym.DrawText(Name, new Coord(6, 0), Alignment.Near, Alignment.Center);
            Sym.DrawText(inductance.ToString(), new Coord(-6, 0), Alignment.Far, Alignment.Center);
        }

        public override string ToString() { return Name + " = " + inductance.ToString(); }
    }
}
