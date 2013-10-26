using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;
using System.ComponentModel;

namespace Circuit
{
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public abstract class BJTModel
    {
        public abstract void Evaluate(Expression Vbc, Expression Vbe, out Expression ic, out Expression ib, out Expression ie);
    }

    // http://people.seas.harvard.edu/~jones/es154/lectures/lecture_3/bjt_models/ebers_moll/ebers_moll.html
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class EbersMollModel : BJTModel
    {
        private double bf = 100;
        private double br = 1.0;
        private double _is = 6.734e-15; // A
        private double vt = 25.85e-3; // V

        public double BF { get { return bf; } set { bf = value; } }
        public double BR { get { return br; } set { br = value; } }
        public double IS { get { return _is; } set { _is = value; } }
        public double VT { get { return vt; } set { vt = value; } }
        
        public EbersMollModel(double BF, double BR, double IS, double VT)
        {
            bf = BF;
            br = BR;
            _is = IS;
            vt = VT;
        }

        public EbersMollModel() { }

        public override void Evaluate(Expression Vbc, Expression Vbe, out Expression ic, out Expression ib, out Expression ie)
        {
            double aR = BR / (1 + BR);
            double aF = BF / (1 + BF);

            Expression iDE = IS * (Call.Exp(Vbe / VT) - 1);
            Expression iDC = IS * (Call.Exp(Vbc / VT) - 1);

            ie = iDE - aR * iDC;
            ic = -iDC + aF * iDE;
            ib = (1 - aF) * iDE + (1 - aR) * iDC;
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

        protected BJTModel model = new EbersMollModel();
        public BJTModel Model { get { return model; } set { model = value; NotifyChanged("Model"); } }

        public BJT()
        {
            c = new Terminal(this, "C");
            e = new Terminal(this, "E");
            b = new Terminal(this, "B");
            Name = "Q1";
        }

        public override void Analyze(ModifiedNodalAnalysis Mna)
        {
            Expression Vbc = Mna.AddNewUnknownEqualTo(Name + "bc", b.V - c.V);
            Expression Vbe = Mna.AddNewUnknownEqualTo(Name + "be", b.V - e.V);

            Expression ic, ib, ie;
            model.Evaluate(Vbc, Vbe, out ic, out ib, out ie);
            c.i = Mna.AddNewUnknownEqualTo("i" + Name + "c", ic);
            b.i = Mna.AddNewUnknownEqualTo("i" + Name + "b", ib);
            e.i = -(ic + ib);
        }

        public override void LayoutSymbol(SymbolLayout Sym)
        {
            Sym.AddTerminal(c, new Coord(10, 20));
            Sym.AddTerminal(b, new Coord(-20, 0));
            Sym.AddTerminal(e, new Coord(10, -20));

            int bx = -5;
            Sym.AddWire(c, new Coord(10, 17));
            Sym.AddWire(b, new Coord(bx, 0));
            Sym.AddWire(e, new Coord(10, -17));

            Sym.DrawLine(EdgeType.Black, new Coord(bx, 12), new Coord(bx, -12));
            Sym.DrawLine(EdgeType.Black, new Coord(10, 17), new Coord(bx, 8));
            Sym.DrawArrow(EdgeType.Black, new Coord(bx, -8), new Coord(10, -17), 0.2, 0.3);

            Sym.DrawText(Name, new Point(0, -20), Alignment.Far, Alignment.Far);
            Sym.AddCircle(EdgeType.Black, new Coord(0, 0), 20);
        }
    }
}
