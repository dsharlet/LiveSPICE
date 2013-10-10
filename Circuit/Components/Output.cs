using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;

namespace Circuit
{
    [CategoryAttribute("IO")]
    [DisplayName("Output")]
    public class Output : TwoTerminal
    {
        private Quantity signal = new Quantity(Call.New(ExprFunction.New("Vout", t), t), Units.V);
        [Description("Name of the output signal.")]
        [SchematicPersistent]
        public Quantity Signal { get { return signal; } set { if (signal.Set(value)) NotifyChanged("Signal"); } }

        public Output() { Name = "O1"; }

        public override void Analyze(IList<Equal> Mna, IList<Expression> Unknowns)
        {
            Anode.i = 0;
            Cathode.i = 0;
            
            Mna.Add(Equal.New(V, signal.Value));
        }

        protected override void DrawSymbol(SymbolLayout Sym)
        {
            int r1 = 10;
            int r2 = 4;

            Sym.AddWire(Anode, new Coord(0, r1));
            Sym.AddWire(Cathode, new Coord(0, -r2));

            Sym.AddCircle(EdgeType.Black, new Coord(0, 0), r1);
            Sym.AddCircle(EdgeType.Black, new Coord(0, 0), r2);
            Sym.AddCircle(EdgeType.Black, new Coord(0, -r2), 1);
        }
    }
}

