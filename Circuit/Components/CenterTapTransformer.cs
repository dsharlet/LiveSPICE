using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;
using System.ComponentModel;

namespace Circuit
{ 
    /// <summary>
    /// Ideal transformer.
    /// </summary>
    [CategoryAttribute("Transformers")]
    [DisplayName("Center Tap Transformer")]
    public class CenterTapTransformer : Component
    {
        private Terminal pa, pc, sa, st, sc;

        public override IEnumerable<Terminal> Terminals
        {
            get 
            {
                yield return pa;
                yield return pc;
                yield return sa;
                yield return st;
                yield return sc;
            }
        }

        protected Ratio turns = new Ratio(1, 1);
        [Description("Turns ratio.")]
        [Serialize]
        public Ratio Turns { get { return turns; } set { turns = value; NotifyChanged("Turns"); } }

        public CenterTapTransformer()
        {
            pa = new Terminal(this, "PA");
            pc = new Terminal(this, "PC");
            sa = new Terminal(this, "SA");
            st = new Terminal(this, "ST");
            sc = new Terminal(this, "SC");
            Name = "TX1"; 
        }

        public override void Analyze(ModifiedNodalAnalysis Mna) 
        {
            Expression Ip = Mna.AddNewUnknown("i" + Name + "p");
            Mna.AddPassiveComponent(pa, pc, Ip);
            Expression Isa = Mna.AddNewUnknown("i" + Name + "sa");
            Expression Isc = Mna.AddNewUnknown("i" + Name + "sc");
            Mna.AddTerminal(sa, -Isa);
            Mna.AddTerminal(sc, Isc);
            Mna.AddTerminal(st, Isa - Isc);
            Mna.AddEquation(Ip * turns, Isa + Isc);

            Expression Vp = pa.V - pc.V;
            Expression Vs1 = sa.V - st.V;
            Expression Vs2 = st.V - sc.V;
            Mna.AddEquation(Vp, Vs1 * turns * 2);
            Mna.AddEquation(Vp, Vs2 * turns * 2);
        }

        public override void LayoutSymbol(SymbolLayout Sym)
        {
            int h = 20;

            Sym.AddTerminal(pa, new Coord(-10, h));
            Sym.AddTerminal(pc, new Coord(-10, -h));
            Sym.AddTerminal(sa, new Coord(10, h));
            Sym.AddTerminal(st, new Coord(10, 0));
            Sym.AddTerminal(sc, new Coord(10, -h));

            Sym.DrawText(Name, new Coord(-16, -h / 2), Alignment.Far, Alignment.Center);
            Sym.DrawText(Turns.ToString(), new Coord(-16, h / 2), Alignment.Far, Alignment.Center);

            h -= 4;

            Sym.AddWire(pa, new Coord(-10, h));
            Sym.AddWire(pc, new Coord(-10, -h));
            Sym.AddWire(sa, new Coord(10, h));
            Sym.AddWire(sc, new Coord(10, -h));
            Sym.InBounds(new Coord(-20, 0), new Coord(20, 0));

            Inductor.DrawCoil(Sym, -10, -h, h, 4, true);
            Sym.DrawLine(EdgeType.Black, new Coord(-2, h), new Coord(-2, -h));
            Sym.DrawLine(EdgeType.Black, new Coord(2, h), new Coord(2, -h));
            Inductor.DrawCoil(Sym, 10, -h, 0.0, 2, false);
            Inductor.DrawCoil(Sym, 10, h, 0.0, 2, false);
        }

        public override string ToString() { return Name + " = " + Turns.ToString(); }
    }
}
