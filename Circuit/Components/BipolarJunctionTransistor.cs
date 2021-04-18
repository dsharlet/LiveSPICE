using ComputerAlgebra;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Circuit
{
    public enum BjtType
    {
        NPN,
        PNP,
    };

    [Category("Transistors")]
    [DisplayName("BJT")]
    public class BipolarJunctionTransistor : Component, INotifyPropertyChanged
    {
        private Terminal c, e, b;
        public override IEnumerable<Terminal> Terminals
        {
            get
            {
                yield return c;
                yield return e;
                yield return b;
            }
        }
        [Browsable(false)]
        public Terminal Collector { get { return c; } }
        [Browsable(false)]
        public Terminal Emitter { get { return e; } }
        [Browsable(false)]
        public Terminal Base { get { return b; } }

        private BjtType type = BjtType.NPN;
        [Serialize, Description("BJT structure.")]
        public BjtType Type { get { return type; } set { type = value; NotifyChanged(nameof(Type)); } }

        protected Quantity _is = new Quantity(1e-12m, Units.A);
        [Serialize, Description("Saturation current.")]
        public Quantity IS { get { return _is; } set { if (_is.Set(value)) NotifyChanged(nameof(IS)); } }

        private Quantity bf = new Quantity(100, Units.None);
        [Serialize, Description("Forward common emitter current gain.")]
        public Quantity BF { get { return bf; } set { if (bf.Set(value)) NotifyChanged(nameof(BF)); } }

        private Quantity br = new Quantity(1, Units.None);
        [Serialize, Description("Reverse common emitter current gain.")]
        public Quantity BR { get { return br; } set { if (br.Set(value)) NotifyChanged(nameof(BR)); } }

        public BipolarJunctionTransistor()
        {
            c = new Terminal(this, "C");
            e = new Terminal(this, "E");
            b = new Terminal(this, "B");
            Name = "Q1";
        }

        public override void Analyze(Analysis Mna)
        {
            int sign;
            switch (Type)
            {
                case BjtType.NPN: sign = 1; break;
                case BjtType.PNP: sign = -1; break;
                default: throw new NotSupportedException("Unknown BJT structure.");
            }

            Expression Vbc = sign * (Base.V - Collector.V);
            Expression Vbe = sign * (Base.V - Emitter.V);
            Vbc = Mna.AddUnknownEqualTo(Name + "bc", Vbc);
            Vbe = Mna.AddUnknownEqualTo(Name + "be", Vbe);

            Expression aR = BR / (1 + (Expression)BR);
            Expression aF = BF / (1 + (Expression)BF);

            Expression iF = IS * LinExpm1(Vbe / VT);
            Expression iR = IS * LinExpm1(Vbc / VT);
            
            Expression ie = iF - aR * iR;
            Expression ic = aF * iF - iR;
            Expression ib = (1 - aF) * iF + (1 - aR) * iR;

            ic = Mna.AddUnknownEqualTo("i" + Name + "c", ic);
            ib = Mna.AddUnknownEqualTo("i" + Name + "b", ib);
            ie = Mna.AddUnknownEqualTo("i" + Name + "e", ie);
            Mna.AddTerminal(Collector, sign * ic);
            Mna.AddTerminal(Base, sign * ib);
            Mna.AddTerminal(Emitter, -sign * ie);
        }

        public static void LayoutSymbol(SymbolLayout Sym, BjtType Type, Terminal C, Terminal B, Terminal E, Func<string> Name, Func<string> Part)
        {
            int bx = -5;
            Sym.AddTerminal(B, new Coord(-20, 0), new Coord(bx, 0));
            switch (Type)
            {
                case BjtType.NPN:
                    Sym.AddTerminal(C, new Coord(10, 20), new Coord(10, 17));
                    Sym.AddTerminal(E, new Coord(10, -20), new Coord(10, -17));
                    Sym.DrawLine(EdgeType.Black, new Coord(10, 17), new Coord(bx, 8));
                    Sym.DrawArrow(EdgeType.Black, new Coord(bx, -8), new Coord(10, -17), 0.2, 0.3);
                    break;
                case BjtType.PNP:
                    Sym.AddTerminal(E, new Coord(10, 20), new Coord(10, 17));
                    Sym.AddTerminal(C, new Coord(10, -20), new Coord(10, -17));
                    Sym.DrawArrow(EdgeType.Black, new Coord(10, 17), new Coord(bx, 8), 0.2, 0.3);
                    Sym.DrawLine(EdgeType.Black, new Coord(bx, -8), new Coord(10, -17));
                    break;
                default:
                    throw new NotSupportedException("Unknown BJT type.");
            }
            Sym.DrawLine(EdgeType.Black, new Coord(bx, 12), new Coord(bx, -12));

            if (Part != null)
                Sym.DrawText(Part, new Coord(8, 20), Alignment.Far, Alignment.Near);
            Sym.DrawText(Name, new Point(8, -20), Alignment.Far, Alignment.Far);

            Sym.AddCircle(EdgeType.Black, new Coord(0, 0), 20);
        }

        protected internal override void LayoutSymbol(SymbolLayout Sym) { LayoutSymbol(Sym, Type, Collector, Base, Emitter, () => Name, () => PartNumber); }
    }
}
