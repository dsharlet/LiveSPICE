using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;
using System.ComponentModel;

namespace Circuit
{ 
    [Category("Standard")]
    [DisplayName("Variable Resistor")]
    [DefaultProperty("Resistance")]
    [Description("Variable resistor.")]
    public class VariableResistor : TwoTerminal
    {
        protected Quantity resistance = new Quantity(100, Units.Ohm);
        [Description("Resistance of this variable resistor.")]
        [Serialize]
        public Quantity Resistance { get { return resistance; } set { if (resistance.Set(value)) NotifyChanged("Resistance"); } }

        protected Expression wipe = 0.5m;
        [Serialize]
        [Description("Position of the wiper on this variable resistor, between 0 and 1.")]
        public Expression Wipe { get { return wipe; } set { wipe = value; NotifyChanged("Wipe"); } }

        public VariableResistor() { Name = "R1"; }

        public override void Analyze(ModifiedNodalAnalysis Mna)
        {
            Resistor.Analyze(Mna, Anode, Cathode, Resistance.Value * Wipe);
        }

        public override void LayoutSymbol(SymbolLayout Sym)
        {
            base.LayoutSymbol(Sym);

            Sym.AddWire(Anode, new Coord(0, 16));
            Sym.AddWire(Cathode, new Coord(0, -16));
            Sym.InBounds(new Coord(-10, 0), new Coord(10, 0));

            Sym.DrawArrow(EdgeType.Black, new Coord(-6, -15), new Coord(6, 15), 0.1);

            Resistor.Draw(Sym, 0, -16, 16, 7);

            Sym.DrawText(() => resistance.ToString(), new Coord(-7, 0), Alignment.Far, Alignment.Center);
            Sym.DrawText(() => wipe.ToString(), new Coord(9, 3), Alignment.Near, Alignment.Near);
            Sym.DrawText(() => Name, new Coord(9, -3), Alignment.Near, Alignment.Far);
        }
    }
}
