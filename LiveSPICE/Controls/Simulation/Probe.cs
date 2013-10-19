using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Circuit;

namespace LiveSPICE
{
    /// <summary>
    /// Component to mark nodes for probing.
    /// </summary>
    public class Probe : OneTerminal
    {
        protected EdgeType color;
        public EdgeType Color { get { return color; } set { color = value; } }

        public Signal Signal = null;

        private double[] buffer = null;
        public double[] Buffer { get { return buffer; } }

        private KeyValuePair<SyMath.Expression, double[]> key;
        public KeyValuePair<SyMath.Expression, double[]> AllocBuffer(int Samples) 
        {
            if (buffer == null || buffer.Length < Samples)
            {
                buffer = new double[Samples];
                key = new KeyValuePair<SyMath.Expression, double[]>(V, buffer);
            }
            return key;
        }

        public Probe() : this(EdgeType.Red) { }
        public Probe(EdgeType Color) { color = Color; }

        public override void Analyze(IList<SyMath.Equal> Mna, IList<SyMath.Expression> Unknowns)
        {
            // Probes don't affect the circuit.
            i = 0;
        }

        protected override void DrawSymbol(Circuit.SymbolLayout Sym)
        {
            base.DrawSymbol(Sym);

            Coord w = Sym.MapTerminal(Terminal);
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
                Sym.DrawText(V.ToString(), new Point(3, 6), Alignment.Far, Alignment.Near);
        }
    }
}
