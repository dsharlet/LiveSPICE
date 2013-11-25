using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComputerAlgebra;
using System.ComponentModel;

namespace Circuit
{
    /// <summary>
    /// Implements a linear model operational amplifier (op-amp). This model will not saturate.
    /// </summary>
    [Category("Op-Amps")]
    [DisplayName("Op-Amp")]
    [Description("Op-amp with model for input resistance Rin, output resistance Rout, and open-loop gain Aol. If the power supply terminals are connected, the op-amp will saturate.")]
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
        public Quantity Rin { get { return rin; } set { if (rin.Set(value)) NotifyChanged("Rin"); } }

        protected Quantity rout = new Quantity(1e2m, Units.Ohm);
        [Description("Output resistance.")]
        [Serialize]
        public Quantity Rout { get { return rout; } set { if (rout.Set(value)) NotifyChanged("Rout"); } }

        protected Quantity gain = new Quantity(1e5m, Units.None);
        [Description("Open-loop gain.")]
        [Serialize]
        public Quantity Aol { get { return gain; } set { if (gain.Set(value)) NotifyChanged("Aol"); } }
        
        private static readonly Constant Pi = Constant.New(Math.PI);
        private static readonly Constant Epsilon = Constant.New(1e-6);

        private static Expression Clamp(Expression x, Expression a, Expression b)
        {
            return Call.Max(Call.Min(x, b), a);
        }

        //private static Expression Saturate(Expression x) { return Call.Abs(x) * x / (1 + x * x); }

        private static Expression Saturate(Expression x) { return Call.ArcTan(Pi * x) / Pi; }
        private static Expression InvSaturate(Expression x) { return Call.Tan(x * Pi) / Pi; }
                        
        public override void Analyze(Analysis Mna)
        {
            // The input terminals are connected by a resistor Rin.
            Resistor.Analyze(Mna, Positive, Negative, Rin);

            // Compute Vout using Vout = A*Vin
            Expression Vout = (Expression)Aol * (Positive.V - Negative.V);
            if (vpp.IsConnected && vnn.IsConnected)
            {
                // Saturate the output if the power supply terminals are connected.
                Expression Vmax = vpp.V - vnn.V;
                Expression Vmid = (vpp.V + vnn.V) / 2;

                Vout = Vmax * Saturate(Vout / Vmax) + Vmid;
                Mna.AddTerminal(Out, (Vout - Out.V) / (Expression)Rout);

                //Mna.AddEquation(Saturate(Vout / Vmax), (Out.V - Vmid) / Vmax);
                //Mna.AddTerminal(Out, Mna.AddNewUnknown("i" + Name));

                //Mna.AddEquation(Vout / Vmax, InvSaturate((Out.V - Vmid) / Vmax));
                //Mna.AddTerminal(Out, Mna.AddNewUnknown("i" + Name));
            }
            else
            {
                Mna.AddTerminal(Out, (Vout - Out.V) / (Expression)Rout);
            }
        }

        public static void LayoutSymbol(SymbolLayout Sym, Terminal p, Terminal n, Terminal o, Terminal vp, Terminal vn, Func<string> Name, Func<string> Part)
        {
            Sym.AddTerminal(p, new Coord(-20, -10));
            Sym.DrawPositive(EdgeType.Black, new Coord(-15, -10));

            Sym.AddTerminal(n, new Coord(-20, 10));
            Sym.DrawNegative(EdgeType.Black, new Coord(-15, 10));

            Sym.AddTerminal(o, new Coord(20, 0));

            Sym.AddTerminal(vn, new Coord(0, -20), new Coord(0, -10));
            Sym.DrawNegative(EdgeType.Black, new Coord(5, -13));

            Sym.AddTerminal(vp, new Coord(0, 20), new Coord(0, 10));
            Sym.DrawPositive(EdgeType.Black, new Coord(5, 13));

            Sym.AddLoop(EdgeType.Black,
                new Coord(-20, 20),
                new Coord(-20, -20),
                new Coord(20, 0));

            if (Part != null)
                Sym.DrawText(Part, new Coord(12, 4), Alignment.Near, Alignment.Near);
            Sym.DrawText(Name, new Coord(12, -4), Alignment.Near, Alignment.Far);
        }

        public override void LayoutSymbol(SymbolLayout Sym) { LayoutSymbol(Sym, Positive, Negative, Out, vpp, vnn, () => Name, () => PartNumber); }
    }
}
