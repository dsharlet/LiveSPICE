using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;

namespace Circuit
{
    [CategoryAttribute("IO")]
    [DisplayName("Input")]
    public class Input : TwoTerminal
    {
        public Input() { Name = "Input"; }
        
        public override void Analyze(IList<Equal> Mna, IList<Expression> Unknowns)
        {
            // A voltage source supplies any current necessary to maintain the voltage.
            Anode.i = null;
            Cathode.i = null;

            Mna.Add(Equal.New(V, Call.New(ExprFunction.New(Name, t), t)));
        }

        protected override void DrawSymbol(SymbolLayout Sym)
        {
            int r1 = 10;
            int r2 = 4;

            Sym.AddWire(Anode, new Coord(0, r2));
            Sym.AddWire(Cathode, new Coord(0, -r1));

            Sym.AddCircle(EdgeType.Black, new Coord(0, 0), r1);
            Sym.AddCircle(EdgeType.Black, new Coord(0, 0), r2);
            Sym.AddCircle(EdgeType.Black, new Coord(0, -r1), 1);
        }
    }

    [CategoryAttribute("IO")]
    [DisplayName("Output")]
    public class Output : PassiveTwoTerminal
    {
        public Output() { Name = "Output"; }

        public override Expression i(Expression V)
        {
            return 0;
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

