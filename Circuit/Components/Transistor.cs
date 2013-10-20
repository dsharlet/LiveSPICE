using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;
using System.ComponentModel;

namespace Circuit
{
    public abstract class BJTModel
    {
        public abstract void Evaluate(Expression Vbc, Expression Vbe, out Expression ic, out Expression ib, out Expression ie);
    }

    public class EbersMollTransistor : BJTModel
    {
        protected decimal IS = 6.734e-15m; // A
        protected decimal VT = 25.85e-3m; // V

        protected decimal BF = 200m;
        protected decimal BR = 10m;

        public EbersMollTransistor(decimal BF, decimal BR, decimal IS, decimal VT)
        {
            this.BF = BF;
            this.BR = BR;
            this.IS = IS;
            this.VT = VT;
        }

        public EbersMollTransistor() : this(260, 10, 10e-12m, 25.85e-3m) { }

        public override void Evaluate(Expression Vbc, Expression Vbe, out Expression ic, out Expression ib, out Expression ie)
        {
            Expression eVbc = Call.Exp(Vbc / VT);
            Expression eVbe = Call.Exp(Vbe / VT);

            ic = IS * (eVbe - eVbc)         - (IS / BR) * (eVbc - 1);
            ib = (IS / BF) * (eVbe - 1)     + (IS / BR) * (eVbc - 1);
            ie = IS * (eVbe - eVbc)         + (IS / BF) * (eVbe - 1);
        }
    };

    /// <summary>
    /// Transistors.
    /// </summary>
    [CategoryAttribute("Transistors")]
    [DisplayName("BJT")]
    public class BJT : Component
    {
        protected Terminal c, e, b;
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

        protected BJTModel model = new EbersMollTransistor();

        public BJT()
        {
            c = new Terminal(this, "C");
            e = new Terminal(this, "E");
            b = new Terminal(this, "B");
            Name = "Q1";
        }

        public override void Analyze(ICollection<Equal> Mna, ICollection<Expression> Unknowns)
        {
            Expression Vbc = DependentVariable(Name + "bc", t);
            Expression Vbe = DependentVariable(Name + "be", t);
            Mna.Add(Equal.New(Vbc, b.V - e.V));
            Mna.Add(Equal.New(Vbe, b.V - e.V));
            Unknowns.Add(Vbc);
            Unknowns.Add(Vbe);

            Expression ic, ib, ie;
            model.Evaluate(Vbc, Vbe, out ic, out ib, out ie);
            c.i = ic;
            b.i = ib;
            e.i = -(ic + ib);
        }

        public override void LayoutSymbol(SymbolLayout Sym)
        {
            Sym.AddTerminal(b, new Coord(-30, 0));
            Sym.AddTerminal(e, new Coord(10, 30));
            Sym.AddTerminal(c, new Coord(10, -30));

            int bx = -5;
            Sym.AddWire(b, new Coord(bx, 0));
            Sym.AddWire(e, new Coord(10, 17));
            Sym.AddWire(c, new Coord(10, -17));

            Sym.DrawLine(EdgeType.Black, new Coord(bx, 12), new Coord(bx, -12));
            Sym.DrawLine(EdgeType.Black, new Coord(10, 17), new Coord(bx, 8));
            Sym.DrawArrow(EdgeType.Black, new Coord(bx, -8), new Coord(10, -17), 0.2, 0.3);

            Sym.DrawText(Name, new Point(0, -20), Alignment.Far, Alignment.Far);
            Sym.AddCircle(EdgeType.Black, new Coord(0, 0), 20);
        }
    }
}
