using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;
using System.ComponentModel;

namespace Circuit
{
    /// <summary>
    /// Implements a model operational amplifier (op-amp). This model will not saturate.
    /// </summary>
    [CategoryAttribute("Standard")]
    [DisplayName("Op-Amp")]
    public class OpAmp : Component
    {
        protected Terminal p, n, o;
        public override IEnumerable<Terminal> Terminals 
        { 
            get 
            {
                yield return p;
                yield return n;
                yield return o;
            } 
        }
        [Browsable(false)]
        public Terminal Positive { get { return p; } }
        [Browsable(false)]
        public Terminal Negative { get { return n; } }
        [Browsable(false)]
        public Terminal Out { get { return o; } }

        protected Quantity rin = new Quantity(1e6m, Units.Ohm);
        [Description("Input resistance.")]
        [SchematicPersistent]
        public Quantity Rin { get { return rin; } set { if (rin.Set(value)) NotifyChanged("Rin"); } }

        protected Quantity rout = new Quantity(1e2m, Units.Ohm);
        [Description("Input resistance.")]
        [SchematicPersistent]
        public Quantity Rout { get { return rout; } set { if (rout.Set(value)) NotifyChanged("Rout"); } }

        protected decimal g = 1e6m;
        [Description("Gain.")]
        [SchematicPersistent]
        public decimal G { get { return g; } set { g = value; NotifyChanged("G"); } }

        public OpAmp()
        {
            p = new Terminal(this, "+");
            n = new Terminal(this, "-");
            o = new Terminal(this, "Out");
        }

        public override void Analyze(IList<Equal> Kcl, IList<Expression> Unknowns)
        {
            // Equations for the input.
            Expression iin = Call.New(ExprFunction.New("in" + Name, t), t);
            Unknowns.Add(iin);
            Positive.i = iin;
            Negative.i = -iin;
            Expression Vin = Positive.V - Negative.V;
            Kcl.Add(Equal.New(Vin / (Expression)Rin, iin));

            // For output.
            Expression iout = Call.New(ExprFunction.New("out" + Name, t), t);
            Unknowns.Add(iout);
            Out.i = iout;

            Kcl.Add(Equal.New((G * Vin - Out.V) / (Expression)Rout, iout));
        }

        public override void LayoutSymbol(SymbolLayout Sym)
        {
            Sym.AddTerminal(Positive, new Coord(-30, 10));
            Sym.AddTerminal(Negative, new Coord(-30, -10));
            Sym.AddTerminal(Out, new Coord(30, 0));

            Sym.AddWire(Positive, new Coord(-20, 10));
            Sym.AddWire(Negative, new Coord(-20, -10));
            Sym.AddWire(Out, new Coord(20, 0));

            Sym.DrawPositive(ShapeType.Black, new Coord(-17, 10));
            Sym.DrawNegative(ShapeType.Black, new Coord(-17, -10));

            Sym.AddLoop(ShapeType.Black,
                new Coord(-20, 20),
                new Coord(-20, -20),
                new Coord(20, 0));

            Sym.DrawText(Name, new CoordD(0, -10), Alignment.Near, Alignment.Far);
        }
    }
}
