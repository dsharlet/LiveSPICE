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
        protected decimal Is = 1e-12m; // A
        protected decimal VT = 25.85e-3m; // V
        protected decimal n = 1.0m;

        public ShockleyDiodeModel(decimal Is, decimal VT, decimal n)
        {
            this.Is = Is;
            this.VT = VT;
            this.n = n;
        }

        public ShockleyDiodeModel()
            : this(1e-12m, 25.85e-3m, 1.0m)
        { }

        public override Expression Evaluate(Expression V)
        {
            return Is * (Call.Exp(V / (n * VT)) - 1);
        }
    };

    [CategoryAttribute("Standard")]
    [DisplayName("Diode")]
    public class Diode : PassiveTwoTerminal
    {
        protected DiodeModel model = new ShockleyDiodeModel();

        public Diode() { Name = "D1"; }

        public override Expression i(Expression V) 
        { 
            return model.Evaluate(V); 
        }

        protected override void DrawSymbol(SymbolLayout Sym)
        {
            Sym.AddWire(Anode, new Coord(0, 10));
            Sym.AddWire(Cathode, new Coord(0, -10));

            Sym.AddLoop(ShapeType.Black,
                new Coord(-10, 10),
                new Coord(10, 10),
                new Coord(0, -10));
            Sym.AddLine(ShapeType.Black, new Coord(-10, -10), new Coord(10, -10));

            if (model.IsLed())
            {
                Sym.DrawArrow(ShapeType.Black, new Coord(-12, 5), new Coord(-20, -3), 0.2);
                Sym.DrawArrow(ShapeType.Black, new Coord(-8, -2), new Coord(-16, -10), 0.2);
            }

            Sym.DrawText(Name, new Coord(10, 0), Alignment.Near, Alignment.Center);
        }
    }
}
