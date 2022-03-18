using System.ComponentModel;

namespace Circuit
{
    [Category("Generic")]
    [DisplayName("Buffer")]
    [Description("Ideal voltage follower.")]
    public class Buffer : TwoTerminal
    {
        public static void Analyze(Analysis Mna, string Name, Node Input, Node Output)
        {
            // Unknown output current.
            Mna.AddTerminal(Output, Mna.AddUnknown("i" + Name));
            // Follow voltage.
            Mna.AddEquation(Input.V, Output.V);
        }
        public static void Analyze(Analysis Mna, Node Input, Node Output) { Analyze(Mna, Mna.AnonymousName(), Input, Output); }

        public override void Analyze(Analysis Mna) { Analyze(Mna, Name, Anode, Cathode); }

        protected internal override void LayoutSymbol(SymbolLayout Sym)
        {
            base.LayoutSymbol(Sym);

            Sym.AddWire(Anode, new Coord(0, 10));
            Sym.AddWire(Cathode, new Coord(0, -10));

            Sym.AddLoop(EdgeType.Black,
                new Coord(-10, 10),
                new Coord(10, 10),
                new Coord(0, -10));

            Sym.DrawText(() => Name, new Coord(10, 0), Alignment.Near, Alignment.Center);
        }
    }
}
