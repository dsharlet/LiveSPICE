using System.ComponentModel;

namespace Circuit
{
    /// <summary>
    /// Ground component, V = 0.
    /// </summary>
    [Category("Generic")]
    [DisplayName("Ground")]
    public class Ground : OneTerminal
    {
        public Ground() { Name = "GND1"; }

        public static void Analyze(Analysis Mna, Node G)
        {
            // Nodes connected to ground have V = 0.
            Mna.AddEquation(G.V, 0);
            // Ground doesn't care about current.
            Mna.AddTerminal(G, null);
        }

        public override void Analyze(Analysis Mna) { Analyze(Mna, Terminal); }

        protected internal override void LayoutSymbol(SymbolLayout Sym)
        {
            base.LayoutSymbol(Sym);

            Sym.AddLoop(EdgeType.Black,
                new Coord(-10, 0),
                new Coord(10, 0),
                new Coord(0, -10));
        }
    }
}
