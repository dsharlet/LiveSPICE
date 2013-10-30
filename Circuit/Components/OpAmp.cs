using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;
using System.ComponentModel;

namespace Circuit
{
    /// <summary>
    /// Implements a linear model operational amplifier (op-amp). This model will not saturate.
    /// </summary>
    [CategoryAttribute("Standard")]
    [DisplayName("Op-Amp")]
    public class OpAmp : IdealOpAmp
    {
        protected Terminal vpp, vnn;

        public override IEnumerable<Terminal> Terminals
        {
            get
            {
                return base.Terminals.Append(vpp, vnn);
            }
        }

        public OpAmp() 
        {
            vpp = new Terminal(this, "Vcc+");
            vnn = new Terminal(this, "Vcc-");
        }

        protected Quantity rin = new Quantity(500e6, Units.Ohm);
        [Description("Input resistance.")]
        [Serialize]
        public Quantity InputResistance { get { return rin; } set { if (rin.Set(value)) NotifyChanged("InputResistance"); } }

        protected Quantity rout = new Quantity(1e2m, Units.Ohm);
        [Description("Output resistance.")]
        [Serialize]
        public Quantity OutputResistance { get { return rout; } set { if (rout.Set(value)) NotifyChanged("OutputResistance"); } }

        protected decimal gain = 1e6m;
        [Description("Gain.")]
        [Serialize]
        public decimal Gain { get { return gain; } set { gain = value; NotifyChanged("Gain"); } }

        private static readonly Constant Pi = Constant.New(Math.PI);

        public override void Analyze(ModifiedNodalAnalysis Mna)
        {
            // The input terminals are connected by a resistor Rin.
            Resistor.Analyze(Mna, Positive, Negative, InputResistance);

            Expression Vmax = vpp.V - vnn.V;

            Expression Vout = Gain * (Positive.V - Negative.V);
            // Vo = (G*Vin - Out.V) / Rout
            Mna.AddEquation((Vmax / Pi) * Call.ArcTan(Pi / Vmax * Vout) + (vpp.V + vnn.V) / 2, Out.V);

            Mna.AddTerminal(Out, Mna.AddNewUnknown("i" + Name));
        }

        public override void LayoutSymbol(SymbolLayout Sym)
        {
            Sym.AddTerminal(Positive, new Coord(-20, -10));
            Sym.AddWire(Positive, new Coord(-20, -10));
            Sym.DrawPositive(EdgeType.Black, new Coord(-15, -10));

            Sym.AddTerminal(Negative, new Coord(-20, 10));
            Sym.AddWire(Negative, new Coord(-20, 10));
            Sym.DrawNegative(EdgeType.Black, new Coord(-15, 10));

            Sym.AddTerminal(Out, new Coord(20, 0));
            Sym.AddWire(Out, new Coord(20, 0));

            Sym.AddTerminal(vnn, new Coord(0, -20));
            Sym.AddWire(vnn, new Coord(0, -10));
            Sym.DrawNegative(EdgeType.Black, new Coord(5, -13));

            Sym.AddTerminal(vpp, new Coord(0, 20));
            Sym.AddWire(vpp, new Coord(0, 10));
            Sym.DrawPositive(EdgeType.Black, new Coord(5, 13));

            Sym.AddLoop(EdgeType.Black,
                new Coord(-20, 20),
                new Coord(-20, -20),
                new Coord(20, 0));

            Sym.DrawText(Name, new Coord(12, -4), Alignment.Near, Alignment.Far);
        }
    }
}
