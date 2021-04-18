using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Circuit
{
    /// <summary>
    /// Implements an ideal operational amplifier (op-amp). An ideal op-amp will not saturate.
    /// </summary>
    [Category("Op-Amps")]
    [DisplayName("Ideal Op-Amp")]
    [Description("Ideal op-amp.")]
    public class IdealOpAmp : Component
    {
        protected Terminal p, n, o;
        public override IEnumerable<Terminal> Terminals
        {
            get
            {
                yield return p;
                yield return n;
                yield return o;
            }
        }
        [Browsable(false)]
        public Terminal Positive { get { return p; } }
        [Browsable(false)]
        public Terminal Negative { get { return n; } }
        [Browsable(false)]
        public Terminal Out { get { return o; } }

        public IdealOpAmp()
        {
            p = new Terminal(this, "+");
            n = new Terminal(this, "-");
            o = new Terminal(this, "Out");
        }

        public override void Analyze(Analysis Mna)
        {
            // Infinite input impedance.
            Mna.AddPassiveComponent(Positive, Negative, 0);
            // Unknown output current.
            Mna.AddTerminal(Out, Mna.AddUnknown("i" + Name));
            // The voltage between the positive and negative terminals is 0.
            Mna.AddEquation(Positive.V, Negative.V);
        }

        public static void LayoutSymbol(SymbolLayout Sym, Terminal p, Terminal n, Terminal o, Func<string> Name)
        {
            Sym.AddTerminal(p, new Coord(-20, -10));
            Sym.DrawPositive(EdgeType.Black, new Coord(-15, -10));

            Sym.AddTerminal(n, new Coord(-20, 10));
            Sym.DrawNegative(EdgeType.Black, new Coord(-15, 10));

            Sym.AddTerminal(o, new Coord(20, 0));
            Sym.AddWire(o, new Coord(20, 0));

            Sym.AddLoop(EdgeType.Black,
                new Coord(-20, 20),
                new Coord(-20, -20),
                new Coord(20, 0));

            Sym.DrawText(Name, new Coord(12, -4), Alignment.Near, Alignment.Far);
        }

        protected internal override void LayoutSymbol(SymbolLayout Sym) { LayoutSymbol(Sym, Positive, Negative, Out, () => Name); }
    }
}
