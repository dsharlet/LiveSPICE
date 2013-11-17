using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;
using System.ComponentModel;

namespace Circuit
{
    public enum BjtType
    {
        NPN,
        PNP,
    };

    /// <summary>
    /// Implementation of the Ebers-Moll transistor model: http://people.seas.harvard.edu/~jones/es154/lectures/lecture_3/bjt_models/ebers_moll/ebers_moll.html
    /// </summary>
    [Category("Transistors")]
    [DisplayName("BJT")]
    [Description("Bipolar junction transistor implemented with the Ebers-Moll model.")]
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
        public Terminal C { get { return c; } }
        [Browsable(false)]
        public Terminal E { get { return e; } }
        [Browsable(false)]
        public Terminal B { get { return b; } }
        
        private BjtType type = BjtType.NPN;
        [Description("BJT structure.")]
        [Serialize]
        public BjtType Type { get { return type; } set { type = value; NotifyChanged("Type"); } }

        protected Quantity _is = new Quantity(1e-12m, Units.A);
        [Description("Saturation current.")]
        [Serialize]
        public Quantity IS { get { return _is; } set { if (_is.Set(value)) NotifyChanged("IS"); } }

        private double bf = 100;
        [Description("Forward common emitter current gain.")]
        [Serialize]
        public double BF { get { return bf; } set { bf = value; NotifyChanged("BF"); } }

        private double br = 1;
        [Description("Reverse common emitter current gain.")]
        [Serialize]
        public double BR { get { return br; } set { br = value; NotifyChanged("BR"); } }

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

            Expression Vbc = Mna.AddNewUnknownEqualTo(Name + "bc", sign * (B.V - C.V));
            Expression Vbe = Mna.AddNewUnknownEqualTo(Name + "be", sign * (B.V - E.V));

            double aR = BR / (1 + BR);
            double aF = BF / (1 + BF);

            Expression iF = (Expression)IS * (Call.Exp(Vbe / VT) - 1);
            Expression iR = (Expression)IS * (Call.Exp(Vbc / VT) - 1);
            
            // TODO: Algebraically rearranging these results in dramatically different stability behavior. 
            // It would be nice to understand this.
            //Expression ie = iF - aR * iR;
            Expression ic = aF * iF - iR;
            Expression ib = (1 - aF) * iF + (1 - aR) * iR;

            ic = Mna.AddNewUnknownEqualTo("i" + Name + "c", ic);
            ib = Mna.AddNewUnknownEqualTo("i" + Name + "b", ib);
            Mna.AddTerminal(C, sign * ic);
            Mna.AddTerminal(B, sign * ib);
            Mna.AddTerminal(E, -sign * (ic + ib));
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

        public override void LayoutSymbol(SymbolLayout Sym) { LayoutSymbol(Sym, Type, C, B, E, () => Name, () => PartNumber); }
    }
}
