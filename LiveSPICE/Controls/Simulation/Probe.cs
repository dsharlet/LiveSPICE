using Circuit;

namespace LiveSPICE
{
    /// <summary>
    /// Component to mark nodes for probing.
    /// </summary>
    class Probe : OneTerminal
    {
        protected EdgeType color;
        public EdgeType Color { get { return color; } set { color = value; } }

        public Signal Signal = null;

        private double[] buffer = null;
        public double[] Buffer { get { return buffer; } }

        public double[] AllocBuffer(int Samples)
        {
            if (buffer == null || buffer.Length != Samples)
                buffer = new double[Samples];
            return buffer;
        }

        private Probe() : this(EdgeType.Red) { }
        public Probe(EdgeType Color) { color = Color; }

        public override void Analyze(Analysis Mna) { }

        protected override void LayoutSymbol(SymbolLayout Sym)
        {
            Coord w = new Coord(0, 0);
            Sym.AddTerminal(Terminal, w);

            Coord dw = new Coord(1, 1);
            Coord pw = new Coord(dw.y, -dw.x);

            w += dw * 10;
            Sym.AddWire(Terminal, w);

            Sym.AddLine(color, w - pw * 4, w + pw * 4);
            Sym.AddLoop(color,
                w + pw * 2,
                w + pw * 2 + dw * 10,
                w + dw * 12,
                w - pw * 2 + dw * 10,
                w - pw * 2);

            if (ConnectedTo != null)
                Sym.DrawText(() => V.ToString(), new Point(0, 6), Alignment.Far, Alignment.Near);
        }
    }
}
