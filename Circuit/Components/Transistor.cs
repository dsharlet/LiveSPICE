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
        public abstract void Evaluate(Expression VC, Expression VB, Expression VE, out Expression iC, out Expression iB, out Expression iE);
    }

    public class EbersMollTransistor : BJTModel
    {
        protected decimal IS = 6.734e-15m; // A
        protected decimal VT = 25.85e-3m; // V

        protected decimal BF = 200m;
        protected decimal BR = 0.1m;

        public EbersMollTransistor(decimal BF, decimal BR, decimal IS, decimal VT)
        {
            this.BF = BF;
            this.BR = BR;
            this.IS = IS;
            this.VT = VT;
        }

        public EbersMollTransistor() : this(260, 10, 10e-12m, 25.85e-3m) { }

        public override void Evaluate(Expression VC, Expression VB, Expression VE, out Expression iC, out Expression iB, out Expression iE)
        {
            Expression eVBC = Call.Exp((VB - VC) / VT);
            Expression eVBE = Call.Exp((VB - VE) / VT);

            iC = IS * (eVBE - eVBC)         - (IS / BR) * (eVBC - 1);
            iB = (IS / BF) * (eVBE - 1)     + (IS / BR) * (eVBC - 1);
            iE = IS * (eVBE - eVBC)         + (IS / BF) * (eVBE - 1);
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

        public override void Analyze(IList<Equal> Mna, IList<Expression> Unknowns)
        {
            Expression Ic, Ib, Ie;
            model.Evaluate(c.V, b.V, e.V, out Ic, out Ib, out Ie);
            c.i = Ic;
            b.i = Ib;
            e.i = Ie;
        }

        public override void LayoutSymbol(SymbolLayout Sym)
        {
            Sym.AddTerminal(b, new Coord(-30, 0));
            Sym.AddTerminal(e, new Coord(10, 30));
            Sym.AddTerminal(c, new Coord(10, -30));

            Sym.AddWire(b, new Coord(-20, 0));
            Sym.AddWire(e, new Coord(10, 17));
            Sym.AddWire(c, new Coord(10, -17));

            Sym.AddCircle(EdgeType.Black, new Coord(0, 0), 20);
        }
    }
}
