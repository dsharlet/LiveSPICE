using ComputerAlgebra;
using System.Collections.Generic;
using System.ComponentModel;

namespace Circuit
{
    /// <summary>
    /// Ideal transformer.
    /// </summary>
    [Category("Generic")]
    [DisplayName("Transformer")]
    [DefaultProperty("Turns")]
    [Description("Ideal transformer.")]
    public class Transformer : Component
    {
        private Terminal pa, pc, sa, sc;

        public override IEnumerable<Terminal> Terminals
        {
            get
            {
                yield return pa;
                yield return pc;
                yield return sa;
                yield return sc;
            }
        }

        protected Ratio turns = new Ratio(1, 1);
        [Serialize, Description("Primary:secondary turns ratio.")]
        public Ratio Turns { get { return turns; } set { turns = value; NotifyChanged(nameof(Turns)); } }

        public Transformer()
        {
            pa = new Terminal(this, "PA");
            pc = new Terminal(this, "PC");
            sa = new Terminal(this, "SA");
            sc = new Terminal(this, "SC");
            Name = "TX1";
        }

        public override void Analyze(Analysis Mna)
        {
            Expression Ip = Mna.AddUnknown("i" + Name + "p");
            Mna.AddPassiveComponent(pa, pc, Ip);
            Expression Is = Mna.AddUnknown("i" + Name + "s");
            Mna.AddPassiveComponent(sc, sa, Is);
            Mna.AddEquation(Ip * turns, Is);

            Expression Vp = pa.V - pc.V;
            Expression Vs = sa.V - sc.V;
            Mna.AddEquation(Vp, turns * Vs);
        }

        protected internal override void LayoutSymbol(SymbolLayout Sym)
        {
            Sym.AddTerminal(pa, new Coord(-10, 20));
            Sym.AddTerminal(pc, new Coord(-10, -20));
            Sym.AddTerminal(sa, new Coord(10, 20));
            Sym.AddTerminal(sc, new Coord(10, -20));

            Sym.AddWire(pa, new Coord(-10, 16));
            Sym.AddWire(pc, new Coord(-10, -16));
            Sym.AddWire(sa, new Coord(10, 16));
            Sym.AddWire(sc, new Coord(10, -16));
            Sym.InBounds(new Coord(-20, 0), new Coord(20, 0));

            Inductor.Draw(Sym, -10, -16.0, 16.0, 4, 4.0);
            Sym.DrawLine(EdgeType.Black, new Coord(-2, 16), new Coord(-2, -16));
            Sym.DrawLine(EdgeType.Black, new Coord(2, 16), new Coord(2, -16));
            Inductor.Draw(Sym, 10, -16.0, 16.0, 4, -4.0);

            Sym.DrawText(() => Name, new Coord(-16, 0), Alignment.Far, Alignment.Center);
            Sym.DrawText(() => Turns.ToString(), new Coord(16, 0), Alignment.Near, Alignment.Center);
        }
    }
}
