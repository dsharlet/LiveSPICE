using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;
using System.ComponentModel;

namespace Circuit
{
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public abstract class DiodeModel
    {
        public abstract Expression Evaluate(Expression V);

        public virtual bool IsLed() { return false; }
    }

    /// <summary>
    /// Shockley diode model: http://en.wikipedia.org/wiki/Diode_modelling#Shockley_diode_model
    /// </summary>
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class ShockleyDiodeModel : DiodeModel
    {
        // Shockley diode model parameters.
        protected double _is; // A
        protected double _n;
        protected bool led = false;

        public double IS { get { return _is; } set { _is = value; } }
        public double n { get { return _n; } set { _n = value; } }
        public bool Led { get { return led; } set { led = value; } }

        public ShockleyDiodeModel(double IS, double n)
        {
            _is = IS;
            _n = n;
        }
        
        public override bool IsLed() { return Led; }
        
        public override Expression Evaluate(Expression V)
        {
            return IS * (Call.Exp(V / (n * Component.VT)) - 1);
        }

        public static readonly ShockleyDiodeModel _1N270 = new ShockleyDiodeModel(1e-6, 1.0);
        public static readonly ShockleyDiodeModel _1N4001 = new ShockleyDiodeModel(6.734e-15, 1.0);
    };

    [CategoryAttribute("Standard")]
    [DisplayName("Diode")]
    public class Diode : TwoTerminal
    {
        protected DiodeModel model = ShockleyDiodeModel._1N270;
        public DiodeModel Model { get { return model; } set { model = value; NotifyChanged("Model"); } }

        public Diode() { Name = "D1"; }

        public override void Analyze(ModifiedNodalAnalysis Mna)
        {
            // Vac = Va - Vc
            Expression Vac = Mna.AddNewUnknownEqualTo("V" + Name, V);
            // Evaluate the model.
            i = Mna.AddNewUnknownEqualTo("i" + Name, model.Evaluate(Vac));
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
