using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;
using System.ComponentModel;

namespace Circuit
{
    public abstract class DiodeModel
    {
        public abstract Expression Evaluate(Expression V);

        public virtual bool IsLed() { return false; }
    }

    /// <summary>
    /// Shockley diode model: http://en.wikipedia.org/wiki/Diode_modelling#Shockley_diode_model
    /// </summary>
    public class ShockleyDiodeModel : DiodeModel
    {
        // Shockley diode model parameters.
        protected decimal IS = 6.734e-15m; // A
        protected decimal VT = 25.85e-3m; // V
        protected decimal n = 1.0m;

        public ShockleyDiodeModel(decimal IS, decimal VT, decimal n)
        {
            this.IS = IS;
            this.VT = VT;
            this.n = n;
        }

        public ShockleyDiodeModel()
            : this(1e-12m, 25.85e-3m, 1.0m)
        { }

        public override Expression Evaluate(Expression V)
        {
            return IS * (Call.Exp(V / (n * VT)) - 1);
        }
    };

    [CategoryAttribute("Standard")]
    [DisplayName("Diode")]
    public class Diode : TwoTerminal
    {
        protected DiodeModel model = new ShockleyDiodeModel();

        public Diode() { Name = "D1"; }

        public override void Analyze(ICollection<Equal> Mna, ICollection<Expression> Unknowns)
        {
            // Make a new unknown for Va - Vc to reduce the number of non-linear variables.

            // Vac = Va - Vc
            Expression Vac = DependentVariable("V" + Name, t);
            Mna.Add(Equal.New(Vac, V));
            Unknowns.Add(Vac);

            Expression i = model.Evaluate(Vac);
            Anode.i = i;
            Cathode.i = -i;
        }

        protected override void DrawSymbol(SymbolLayout Sym)
        {
            Sym.AddWire(Anode, new Coord(0, 10));
            Sym.AddWire(Cathode, new Coord(0, -10));

            Sym.AddLoop(EdgeType.Black,
                new Coord(-10, 10),
                new Coord(10, 10),
                new Coord(0, -10));
            Sym.AddLine(EdgeType.Black, new Coord(-10, -10), new Coord(10, -10));

            if (model.IsLed())
            {
                Sym.DrawArrow(EdgeType.Black, new Coord(-12, 5), new Coord(-20, -3), 0.2);
                Sym.DrawArrow(EdgeType.Black, new Coord(-8, -2), new Coord(-16, -10), 0.2);
            }

            Sym.DrawText(Name, new Coord(10, 0), Alignment.Near, Alignment.Center);
        }
    }
}
