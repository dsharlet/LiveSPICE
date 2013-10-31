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
    [DefaultProperty("Capacitance")]
    [Description("Standard capacitor component")]
    public class Capacitor : TwoTerminal
    {
        private Quantity capacitance = new Quantity(100e-6m, Units.F);
        [Description("Capacitance of this capacitor.")]
        [Serialize]
        public Quantity Capacitance { get { return capacitance; } set { if (capacitance.Set(value)) NotifyChanged("Capacitance"); } }

        public Capacitor() { Name = "C1"; }

        public static Expression Analyze(ModifiedNodalAnalysis Mna, Terminal Anode, Terminal Cathode, Expression C)
        {
            // Ensure that V is not multiple variables.
            Expression V = Mna.AddNewUnknownEqualTo(Anode.V - Cathode.V);
            // i = C*dV/dt
            Expression i = C * D(V, t);
            Mna.AddPassiveComponent(Anode, Cathode, i);
            return i;
        }

        public override void Analyze(ModifiedNodalAnalysis Mna) { Analyze(Mna, Anode, Cathode, Capacitance); }

        public override void LayoutSymbol(SymbolLayout Sym)
        {
            base.LayoutSymbol(Sym);

            Sym.AddWire(Anode, new Coord(0, 2));
            Sym.AddWire(Cathode, new Coord(0, -2));

            Sym.AddLine(EdgeType.Black, new Coord(-10, 2), new Coord(10, 2));
            Sym.AddLine(EdgeType.Black, new Coord(-10, -2), new Coord(10, -2));

            Sym.DrawText(() => Name, new Coord(12, 0), Alignment.Near, Alignment.Center);
            Sym.DrawText(() => capacitance.ToString(), new Coord(-12, 0), Alignment.Far, Alignment.Center);
        }

        public override string ToString() { return Name + " = " + capacitance.ToString(); }
    }
}
