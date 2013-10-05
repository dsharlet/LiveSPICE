using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;
using System.ComponentModel;

namespace Circuit
{
    public abstract class TransistorModel
    {
        public abstract void Evaluate(Expression Vc, Expression Vb, Expression Ve, out Expression Ic, out Expression Ib, out Expression Ie);
    }

    public class EbersMollTransistor : TransistorModel
    {
        protected decimal BF, BR, IS, VT;

        public EbersMollTransistor(decimal BF, decimal BR, decimal IS, decimal VT)
        {
            this.BF = BF;
            this.BR = BR;
            this.IS = IS;
            this.VT = VT;
        }

        public EbersMollTransistor() : this(260, 10, 10e-12m, 25.85e-3m) { }

        public override void Evaluate(Expression Vc, Expression Vb, Expression Ve, out Expression Ic, out Expression Ib, out Expression Ie)
        {
            Expression Vbe = Vb - Ve;
            Expression Vbc = Vb - Vc;

            Ic = Ib = Ie = 0;
        }
    };

    /// <summary>
    /// Base class for a triode.
    /// </summary>
    [CategoryAttribute("Transistors")]
    [DisplayName("Transistor")]
    public class Transistor : Component
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

        protected TransistorModel model = new EbersMollTransistor();

        public Transistor()
        {
            c = new Terminal(this, "C");
            e = new Terminal(this, "E");
            b = new Terminal(this, "B");
            Name = "Q1";
        }

        public override void Analyze(IList<Equal> Kcl, IList<Expression> Unknowns)
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

            Sym.AddCircle(ShapeType.Black, new Coord(0, 0), 20);
        }
    }
}
