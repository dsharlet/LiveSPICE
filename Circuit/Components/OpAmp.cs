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
        protected Terminal vcc, vee;
                
        public override IEnumerable<Terminal> Terminals { get { return base.Terminals.Append(vcc, vee); } }

        public OpAmp() 
        {
            vcc = new Terminal(this, "Vcc+");
            vee = new Terminal(this, "Vcc-");
        }

        protected Quantity rin = new Quantity(500e6, Units.Ohm);
        [Serialize, Description("Input resistance.")]
        public Quantity Rin { get { return rin; } set { if (rin.Set(value)) NotifyChanged("Rin"); } }

        protected Quantity rout = new Quantity(75m, Units.Ohm);
        [Serialize, Description("Output resistance.")]
        public Quantity Rout { get { return rout; } set { if (rout.Set(value)) NotifyChanged("Rout"); } }

        protected Quantity gain = new Quantity(1e5m, Units.None);
        [Serialize, Description("Open-loop gain.")]
        public Quantity Aol { get { return gain; } set { if (gain.Set(value)) NotifyChanged("Aol"); } }
        
        public override void Analyze(Analysis Mna)
        {
            // The input terminals are connected by a resistor Rin.
            Resistor.Analyze(Mna, Negative, Positive, Rin);

            // Compute Vout using Vout = Aol*V[Rin]
            Expression Vout = (Expression)Aol * (Negative.V - Positive.V);
            if (vcc.IsConnected && vee.IsConnected)
            {
                Node ncc = new Node() { Name = "ncc" };
                Node nee = new Node() { Name = "nee" };
                Node lim = new Node() { Name = "lim" };

                Mna.PushContext();

                // Implement voltage limiter.
                Mna.DeclNodes(ncc, nee, lim);

                VoltageSource.Analyze(Mna, vcc, ncc, 2);
                Diode.Analyze(Mna, lim, ncc, 8e-16, 1, VT);

                VoltageSource.Analyze(Mna, vee, nee, -2);
                Diode.Analyze(Mna, nee, lim, 8e-16, 1, VT);

                Mna.AddTerminal(lim, (Vout - lim.V) / (Expression)Rout);

                // Output.
                Conductor.Analyze(Mna, lim, Out);

                Mna.PopContext();
            }
            else
            {
                Mna.AddTerminal(Out, (Vout - Out.V) / (Expression)Rout);
            }
        }

        public static void LayoutSymbol(SymbolLayout Sym, Terminal p, Terminal n, Terminal o, Terminal vp, Terminal vn, Func<string> Name, Func<string> Part)
        {
            IdealOpAmp.LayoutSymbol(Sym, p, n, o, Name);

            Sym.AddTerminal(vn, new Coord(0, -20), new Coord(0, -10));
            Sym.DrawNegative(EdgeType.Black, new Coord(5, -13));

            Sym.AddTerminal(vp, new Coord(0, 20), new Coord(0, 10));
            Sym.DrawPositive(EdgeType.Black, new Coord(5, 13));

            if (Part != null)
                Sym.DrawText(Part, new Coord(12, 4), Alignment.Near, Alignment.Near);
        }

        public override void LayoutSymbol(SymbolLayout Sym) { LayoutSymbol(Sym, Positive, Negative, Out, vcc, vee, () => Name, () => PartNumber); }
    }
}
