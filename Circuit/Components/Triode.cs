using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComputerAlgebra;
using System.ComponentModel;

namespace Circuit
{
    /// <summary>
    /// Base class for a triode.
    /// </summary>
    public abstract class Triode : Component
    {
        private Terminal p, g, k;
        public override IEnumerable<Terminal> Terminals
        {
            get
            {
                yield return p;
                yield return g;
                yield return k;
            }
        }
        [Browsable(false)]
        public Terminal Plate { get { return p; } }
        [Browsable(false)]
        public Terminal Grid { get { return g; } }
        [Browsable(false)]
        public Terminal Cathode { get { return k; } }
        
        public Triode()
        {
            p = new Terminal(this, "P");
            g = new Terminal(this, "G");
            k = new Terminal(this, "K");
            Name = "V1";
        }

        protected abstract void Analyze(Analysis Mna, Expression Vgk, Expression Vpk, out Expression ip, out Expression ig);

        public override void Analyze(Analysis Mna)
        {
            Expression Vpk = Mna.AddNewUnknownEqualTo(Name + "pk", p.V - k.V);
            Expression Vgk = Mna.AddNewUnknownEqualTo(Name + "gk", g.V - k.V);

            Expression ip, ig;
            Analyze(Mna, Vgk, Vpk, out ip, out ig);
            ip = Mna.AddNewUnknownEqualTo("i" + Name + "p", ip);
            ig = Mna.AddNewUnknownEqualTo("i" + Name + "g", ig);
            Mna.AddTerminal(p, ip);
            Mna.AddTerminal(g, ig);
            Mna.AddTerminal(k, -(ip + ig));
        }

        public void ConnectTo(Node P, Node G, Node K)
        {
            p.ConnectTo(P);
            g.ConnectTo(G);
            k.ConnectTo(K);
        }

        public static void LayoutSymbol(SymbolLayout Sym, Terminal P, Terminal G, Terminal K, Func<string> Name, Func<string> Part)
        {
            Sym.AddTerminal(P, new Coord(0, 20), new Coord(0, 4));
            Sym.AddWire(new Coord(-10, 4), new Coord(10, 4));

            Sym.AddTerminal(G, new Coord(-20, 0), new Coord(-12, 0));
            for (int i = -8; i < 16; i += 8)
                Sym.AddWire(new Coord(i, 0), new Coord(i + 4, 0));

            Sym.AddTerminal(K, new Coord(-10, -20), new Coord(-10, -6), new Coord(-8, -4), new Coord(8, -4), new Coord(10, -6));

            Sym.AddCircle(EdgeType.Black, new Coord(0, 0), 20);

            if (Part != null)
                Sym.DrawText(Part, new Coord(-2, 20), Alignment.Far, Alignment.Near);
            Sym.DrawText(Name, new Point(-8, -20), Alignment.Near, Alignment.Far);
        }

        public override void LayoutSymbol(SymbolLayout Sym) { LayoutSymbol(Sym, p, g, k, () => Name, () => PartNumber); }
    }
}
