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
        [SchematicPersistent]
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
            pa.i = Ip;
            pc.i = -Ip;
            Expression Isa = Mna.AddNewUnknown("i" + Name + "sa");
            Expression Isc = Mna.AddNewUnknown("i" + Name + "sc");
            sa.i = -Isa;
            sc.i = Isc;
            st.i = Isa - Isc;
            Mna.AddEquation(Ip * turns, Isa + Isc);

            Expression Vp = pa.V - pc.V;
            Expression Vs1 = sa.V - st.V;
            Expression Vs2 = st.V - sc.V;
            Mna.AddEquation(Vp, Vs1 * turns);
            Mna.AddEquation(Vp, Vs2 * turns);
        }

        public override void LayoutSymbol(SymbolLayout Sym)
        {
            Sym.AddTerminal(pa, new Coord(-10, 40));
            Sym.AddTerminal(pc, new Coord(-10, -40));
            Sym.AddTerminal(sa, new Coord(10, 40));
            Sym.AddTerminal(st, new Coord(10, 0));
            Sym.AddTerminal(sc, new Coord(10, -40));

            Sym.AddWire(pa, new Coord(-10, 36));
            Sym.AddWire(pc, new Coord(-10, -36));
            Sym.AddWire(sa, new Coord(10, 36));
            Sym.AddWire(sc, new Coord(10, -36));
            Sym.InBounds(new Coord(-20, 0), new Coord(20, 0));

            Inductor.DrawCoil(Sym, -10, -36.0, 36.0, 8, true);
            Sym.DrawLine(EdgeType.Black, new Coord(-2, 36), new Coord(-2, -36));
            Sym.DrawLine(EdgeType.Black, new Coord(2, 36), new Coord(2, -36));
            Inductor.DrawCoil(Sym, 10, -36.0, 0.0, 4, false);
            Inductor.DrawCoil(Sym, 10, 36.0, 0.0, 4, false);


            Sym.DrawText(Name, new Coord(16, -20), Alignment.Near, Alignment.Center);
            Sym.DrawText(Turns.ToString(), new Coord(16, 20), Alignment.Near, Alignment.Center);
        }

        public override string ToString() { return Name + " = " + Turns.ToString(); }
    }
}
