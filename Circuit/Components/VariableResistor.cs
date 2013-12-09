using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComputerAlgebra;
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
        [Serialize, Description("Resistance of this variable resistor.")]
        public Quantity Resistance { get { return resistance; } set { if (resistance.Set(value)) NotifyChanged("Resistance"); } }

        protected double wipe = 0.5;
        [Serialize, Description("Position of the wiper on this variable resistor, between 0 and 1.")]
        public double Wipe { get { return wipe; } set { wipe = value; NotifyChanged("Wipe"); } }

        protected SweepType sweep = SweepType.Linear;
        [Serialize, Description("Sweep mapping of the wiper.")]
        public SweepType Sweep { get { return sweep; } set { sweep = value; NotifyChanged("Sweep"); } }

        public VariableResistor() { Name = "R1"; }

        public override void Analyze(Analysis Mna)
        {
            Expression P = Mna.AddParameter(this, Name, wipe, 1e-6, 1.0 - 1e-6, Sweep);

            Resistor.Analyze(Mna, Name, Anode, Cathode, (Expression)Resistance * P);
        }

        public override void LayoutSymbol(SymbolLayout Sym)
        {
            base.LayoutSymbol(Sym);

            Sym.AddWire(Anode, new Coord(0, 16));
            Sym.AddWire(Cathode, new Coord(0, -16));
            Sym.InBounds(new Coord(-10, 0), new Coord(10, 0));

            Resistor.Draw(Sym, 0, -16, 16, 7);

            Sym.DrawArrow(EdgeType.Black, new Coord(-6, -15), new Coord(6, 15), 0.1);

            Sym.DrawText(() => Resistance.ToString(), new Coord(-7, 0), Alignment.Far, Alignment.Center);
            Sym.DrawText(() => Wipe.ToString("G3"), new Coord(9, 3), Alignment.Near, Alignment.Near);
            Sym.DrawText(() => Name, new Coord(9, -3), Alignment.Near, Alignment.Far);
        }
    }
}
