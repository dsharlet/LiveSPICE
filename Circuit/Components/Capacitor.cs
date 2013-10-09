using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;
using System.ComponentModel;

namespace Circuit
{ 
    /// <summary>
    /// Capacitor is a passive linear component with i = C*dV/dt.
    /// </summary>
    [CategoryAttribute("Standard")]
    [DisplayName("Capacitor")]
    public class Capacitor : TwoTerminal
    {
        private Quantity capacitance = new Quantity(100e-6m, Units.F);
        [Description("Capacitance of this capacitor.")]
        [SchematicPersistent]
        public Quantity Capacitance { get { return capacitance; } set { if (capacitance.Set(value)) NotifyChanged("Capacitance"); } }

        public Capacitor() { Name = "C1"; }

        public override void Analyze(IList<Equal> Mna, IList<Expression> Unknowns)
        {
            // Vac = Va - Vc
            Expression Vac = Call.New(ExprFunction.New("V" + Name, t), t);
            Mna.Add(Equal.New(Vac, V));
            Unknowns.Add(Vac);
            
            // i = C*dV/dt.
            Expression dVac_dt = D(Vac, t);
            Expression i = capacitance.Value * dVac_dt;
            Anode.i = i;
            Cathode.i = -i;
            Unknowns.Add(dVac_dt);
        }
                
        protected override void DrawSymbol(SymbolLayout Sym)
        {
            Sym.AddWire(Anode, new Coord(0, 2));
            Sym.AddWire(Cathode, new Coord(0, -2));

            Sym.AddLine(ShapeType.Black, new Coord(-10, 2), new Coord(10, 2));
            Sym.AddLine(ShapeType.Black, new Coord(-10, -2), new Coord(10, -2));

            Sym.DrawText(Name, new Coord(12, 0), Alignment.Near, Alignment.Center);
            Sym.DrawText(capacitance.ToString(), new Coord(-12, 0), Alignment.Far, Alignment.Center);
        }

        public override string ToString() { return Name + " = " + capacitance.ToString(); }
    }
}
